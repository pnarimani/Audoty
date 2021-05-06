using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
#if USE_UNITASK
using Cysharp.Threading.Tasks;
#else
// Enables the use of coroutines
using System.Collections;
#endif
#if USE_EDITOR_COROUTINES && UNITY_EDITOR
using Unity.EditorCoroutines.Editor;

#endif

namespace Audoty
{
    [CreateAssetMenu(fileName = "Audio Player", menuName = "Audio Player", order = 215)]
    public class AudioPlayer : ScriptableObject
    {
        [SerializeField] private List<AudioClip> _clips;

        [SerializeField, BoxGroup("Parameters")]
        private bool _loop;

        [SerializeField, BoxGroup("Parameters")]
        [Tooltip("When live link is enabled, changes to the parameter will apply to existing/live audio sources.")]
        private bool _liveLinkLoop = true;

        [SerializeField, BoxGroup("Parameters")]
        private bool _saveLoop;

        [SerializeField]
        [Space]
        [BoxGroup("Parameters")]
        [Tooltip("When true, only one instance of this AudioPlayer will be played")]
        private bool _singleton;

        [SerializeField, BoxGroup("Parameters")]
        private bool _saveSingelton;

        [Space] [SerializeField, Range(0, 1), BoxGroup("Parameters")]
        private float _volume = 1;

        [SerializeField, BoxGroup("Parameters")]
        [Tooltip("When live link is enabled, changes to the parameter will apply to existing/live audio sources.")]
        private bool _liveLinkVolume = true;

        [SerializeField, BoxGroup("Parameters")]
        private bool _saveVolume;

        [Space] [SerializeField, BoxGroup("Parameters")]
        private float _minDistance = 1;

        [SerializeField, BoxGroup("Parameters")]
        private float _maxDistance = 500;

        [SerializeField, BoxGroup("Parameters")]
        [Tooltip("When live link is enabled, changes to the parameter will apply to existing/live audio sources.")]
        private bool _liveLinkDistances = true;

        [SerializeField, BoxGroup("Parameters")]
        private bool _saveDistances;

        [Space] [SerializeField, MinMaxSlider(-3, 3), BoxGroup("Parameters")]
        private Vector2 _pitch = Vector2.one;

        [SerializeField, BoxGroup("Parameters")]
        [Tooltip("When live link is enabled, changes to the parameter will apply to existing/live audio sources.")]
        private bool _liveLinkPitch = true;

        [SerializeField, BoxGroup("Parameters")]
        private bool _savePitch;

        [SerializeField, HideInInspector] private int _randomizedSaveKey;
        
        private static readonly Queue<AudioSource> Pool = new Queue<AudioSource>();
        private readonly Dictionary<int, AudioSource> _playingSources = new Dictionary<int, AudioSource>();

#if !USE_UNITASK
        private CoroutineRunner _coroutineRunner;
#endif
        private int _nextId;
        private Handle? _singletonHandle;

#if UNITY_EDITOR
        // Keep record of keys to make sure there is no conflict
        private static readonly Dictionary<int, AudioPlayer> SaveKeys = new Dictionary<int, AudioPlayer>();
        private Handle? _lastPlayedAudio;
#endif

        public List<AudioClip> Clips
        {
            get => _clips;
            set => _clips = value;
        }

        public bool Loop
        {
            get => _loop;
            set
            {
                if (_loop == value)
                    return;

                _loop = value;
                SaveParameters();
                ReconfigurePlayingAudioSources();
            }
        }

        public bool Singleton
        {
            get => _singleton;
            set
            {
                _singleton = value;
                SaveParameters();
            }
        }

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                SaveParameters();
                ReconfigurePlayingAudioSources();
            }
        }

        public float MinDistance
        {
            get => _minDistance;
            set
            {
                _minDistance = value;
                SaveParameters();
                ReconfigurePlayingAudioSources();
            }
        }

        public float MaxDistance
        {
            get => _maxDistance;
            set
            {
                _maxDistance = value;
                SaveParameters();
                ReconfigurePlayingAudioSources();
            }
        }

        public Vector2 Pitch
        {
            get => _pitch;
            set
            {
                if (_pitch == value)
                    return;
                _pitch = value;
                SaveParameters();
                ReconfigurePlayingAudioSources();
            }
        }

#if UNITY_EDITOR
        private bool ShowStopButton => _lastPlayedAudio != null && _lastPlayedAudio.Value.IsPlaying();
#endif

        private string PersistentPrefix => _randomizedSaveKey + "_";

        private bool PersistentLoop
        {
            get => PlayerPrefs.GetInt(PersistentPrefix + "loop", _loop ? 1 : 0) == 1;
            set => PlayerPrefs.SetInt(PersistentPrefix + "loop", value ? 1 : 0);
        }

        private bool PersistentSingleton
        {
            get => PlayerPrefs.GetInt(PersistentPrefix + "singleton", _singleton ? 1 : 0) == 1;
            set => PlayerPrefs.SetInt(PersistentPrefix + "singleton", value ? 1 : 0);
        }

        private float PersistentVolume
        {
            get => PlayerPrefs.GetFloat(PersistentPrefix + "volume", _volume);
            set => PlayerPrefs.SetFloat(PersistentPrefix + "volume", value);
        }

        private float PersistentMinDistance
        {
            get => PlayerPrefs.GetFloat(PersistentPrefix + "minDistance", _minDistance);
            set => PlayerPrefs.SetFloat(PersistentPrefix + "minDistance", value);
        }

        private float PersistentMaxDistance
        {
            get => PlayerPrefs.GetFloat(PersistentPrefix + "maxDistance", _maxDistance);
            set => PlayerPrefs.SetFloat(PersistentPrefix + "maxDistance", value);
        }

        private Vector2 PersistentPitch
        {
            get
            {
                float x = PlayerPrefs.GetFloat(PersistentPrefix + "pitchX", _pitch.x);
                float y = PlayerPrefs.GetFloat(PersistentPrefix + "pitchY", _pitch.y);
                return new Vector2(x, y);
            }
            set
            {
                PlayerPrefs.SetFloat(PersistentPrefix + "pitchX", value.x);
                PlayerPrefs.SetFloat(PersistentPrefix + "pitchY", value.y);
            }
        }

        private void OnEnable()
        {
            LoadParameters();
        }


        /// <summary>
        /// Plays a random clip Fire & Forget style
        /// </summary>
        [Button("Play Random", ButtonSizes.Large)]
#if !USE_UNITASK && !USE_EDITOR_COROUTINES
        [InfoBox("In order to return Audio Sources to pool in Edit Mode, you need to install UniTask or Editor Coroutines package.\n(There will be no problems in play mode)", InfoMessageType.Error, VisibleIf
 = "@UnityEngine.Application.isPlaying == false")]
#endif
        [HideIf("ShowStopButton")]
        public void PlayForget()
        {
            Play();
        }

        /// <summary>
        /// Plays a specific clip Fire & Forget style
        /// </summary>
        /// <param name="index"></param>
        [Button("Play Specific", ButtonSizes.Large, ButtonStyle.Box, Expanded = true)]
        [HideIf("ShowStopButton")]
        public void PlayForget(int index)
        {
            Play(index);
        }

        public void PlayForget(string clipName)
        {
            Play(clipName);
        }

        /// <summary>
        /// Stops singleton instance if it's playing
        /// </summary>
        public void StopSingleton()
        {
            if (_singleton && _singletonHandle != null && _singletonHandle.Value.IsPlaying())
                _singletonHandle.Value.Stop();
        }

        /// <summary>
        /// Plays a random clip, optionally at a position, and returns a handle which can be used to stop the clip.
        /// If the position is provided, audio will be 3D, otherwise, audio will be played 2D 
        /// </summary>
        /// <param name="position">Position to play the clip at.</param>
        /// <returns></returns>
        public Handle Play(Vector3? position = null)
        {
            if (_clips.Count == 0)
                throw new NoClipsFoundException(this);

            return Play(Random.Range(0, _clips.Count), position);
        }

        /// <summary>
        /// Finds and plays a clip by clip name, optionally at a position, and returns a handle which can be used to stop the clip.
        /// If the position is provided, audio will be 3D, otherwise, audio will be played 2D 
        /// </summary>
        /// <param name="clipName">The name of the clip to play</param>
        /// <param name="position">Position to play the clip at.</param>
        /// <returns></returns>
        public Handle Play(string clipName, Vector3? position = null)
        {
            int index = _clips.FindIndex(x => x.name == clipName);
            if (index == -1)
                throw new ClipNotFoundException(this, clipName);

            return Play(index, position);
        }

        /// <summary>
        /// Plays a given clip, optionally at a position, and returns a handle which can be used to stop the clip.
        /// If the position is provided, audio will be 3D, otherwise, audio will be played 2D 
        /// </summary>
        /// <param name="index">The index of the clip to play</param>
        /// <param name="position">Position to play the clip at.</param>
        /// <returns></returns>
        public Handle Play(int index, Vector3? position = null)
        {
#if UNITY_EDITOR
            CheckSaveKeyConflict();
#endif
            
            // If this instance is singleton, and there's an instance of audio that is playing, return that instance of audio
            if (_singleton && _singletonHandle != null && _singletonHandle.Value.IsPlaying())
                return _singletonHandle.Value;

            AudioSource audioSource = Spawn(position);

            AudioClip clip = _clips[index];

            if (clip == null)
                throw new ClipNullException(this, index);

            if (position != null)
                audioSource.transform.position = position.Value;

            audioSource.clip = clip;
            audioSource.Play();

            int id = _nextId;
            _nextId++;

            // Since looping audios will be playing forever, we are not going to despawn them
            if (!_loop)
            {
                // Select despawn strategy based on what packages are present
#if USE_UNITASK
                DespawnAfter(id, clip.length).Forget();
#else
#if USE_EDITOR_COROUTINES && UNITY_EDITOR
                if (Application.isPlaying)
                    RunCoroutine(DespawnAfter(id, clip.length));
                else
                    EditorCoroutineUtility.StartCoroutine(DespawnAfter(id, clip.length), this);
#else
                RunCoroutine(DespawnAfter(id, clip.length));
#endif

#endif
            }

            _playingSources.Add(id, audioSource);

            var handle = new Handle(this, id);

            if (_singleton)
                _singletonHandle = handle;

#if UNITY_EDITOR
            _lastPlayedAudio = handle;
#endif

            return handle;
        }

#if UNITY_EDITOR
        [Button("Stop", ButtonSizes.Large), ShowIf("ShowStopButton")]
        private void StopLastPlayingClip()
        {
            if (_lastPlayedAudio != null)
                _lastPlayedAudio.Value.Stop();
        }
#endif

        private bool Stop(int id)
        {
            if (_playingSources.TryGetValue(id, out AudioSource source))
            {
                if (source != null)
                {
                    source.Stop();
                    Pool.Enqueue(source);
                }

                _playingSources.Remove(id);
                return true;
            }

            return false;
        }

        private AudioSource Spawn(Vector3? position)
        {
            AudioSource source;
            if (Pool.Count > 0)
            {
                source = Pool.Dequeue();
            }
            else
            {
                var go = new GameObject("Audio Source", typeof(AudioSource))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                if (Application.isPlaying)
                    DontDestroyOnLoad(go);

                source = go.GetComponent<AudioSource>();
            }

            ConfigureParameters(source, false);

            source.spatialBlend = position != null ? 1 : 0;
            source.transform.position = position ?? Vector3.zero;

            return source;
        }

        private void ConfigureParameters(AudioSource source, bool live)
        {
            if (!live || _liveLinkPitch)
                source.pitch = Random.Range(_pitch.x, _pitch.y);
            
            if (!live || _liveLinkVolume)
                source.volume = _volume;
            
            if (!live || _liveLinkDistances)
            {
                source.minDistance = _minDistance;
                source.maxDistance = _maxDistance;
            }

            if (!live || _liveLinkLoop)
                source.loop = _loop;
        }

#if USE_UNITASK
        private async UniTask DespawnAfter(int id, float time)
        {
            await UniTask.Delay((int) (time * 1000));
            Stop(id);
        }
#else
        private void RunCoroutine(IEnumerator coroutine)
        {
            if (_coroutineRunner == null)
            {
                _coroutineRunner = new GameObject("Coroutine Runner", typeof(CoroutineRunner))
                {
                    hideFlags = HideFlags.HideAndDontSave
                }.GetComponent<CoroutineRunner>();
            }

            _coroutineRunner.StartCoroutine(coroutine);
        }

        private IEnumerator DespawnAfter(int id, float time)
        {
#if USE_EDITOR_COROUTINES && UNITY_EDITOR
            if (!Application.isPlaying)
            {
                yield return new EditorWaitForSeconds(time);
            }
            else
            {
                yield return new WaitForSeconds(time);
            }
#else
            yield return new WaitForSeconds(time);
#endif
            Stop(id);
        }
#endif

#if UNITY_EDITOR
        private void OnValidate()
        {
            while (_randomizedSaveKey == 0)
                _randomizedSaveKey = Random.Range(int.MinValue + 1, int.MaxValue - 1);
            
            CheckSaveKeyConflict();
            
            ReconfigurePlayingAudioSources();
        }

        private void CheckSaveKeyConflict()
        {
            // Remove all entries which their audio player has been destroyed
            int[] keysToRemove = SaveKeys
                .Where(x => x.Value == null)
                .Select(x => x.Key)
                .ToArray();
            foreach (int k in keysToRemove)
            {
                SaveKeys.Remove(k);
            }
            
            const int iterations = 1000;
            for (int i = 0; i < iterations; i++)
            {
                if (_randomizedSaveKey == 0) 
                    OnValidate();

                if (SaveKeys.TryGetValue(_randomizedSaveKey, out AudioPlayer existing))
                {
                    if (existing == this) 
                        return;

                    string existingName = existing != null ? existing.name : "Unknown";
                    
                    Debug.LogError($"Found conflicting save keys between Audio Player `{existingName}` and `{name}`. Resolving the conflict by changing save key of {name}");
                    _randomizedSaveKey = Random.Range(int.MinValue + 1, int.MaxValue - 1);
                    continue;
                }

                SaveKeys.Add(_randomizedSaveKey, this);

                break;
            }
        }
#endif

        private void LoadParameters()
        {
#if UNITY_EDITOR
            CheckSaveKeyConflict();
#endif
            // We check isEditor this way to make it easier to develop non-editor code. 
            // If you put the rest of the code in #else intellisense will not work.
            if (Application.isEditor)
                return;

            if (_saveLoop)
                Loop = PersistentLoop;

            if (_saveSingelton)
                Singleton = PersistentSingleton;

            if (_saveVolume)
                Volume = PersistentVolume;

            if (_saveDistances)
            {
                MinDistance = PersistentMinDistance;
                MaxDistance = PersistentMaxDistance;
            }

            if (_savePitch)
                Pitch = PersistentPitch;
        }

        private void SaveParameters()
        {
#if UNITY_EDITOR
            CheckSaveKeyConflict();
#endif
            // We check isEditor this way to make it easier to develop non-editor code. 
            // If you put the rest of the code in #else intellisense will not work.
            if (Application.isEditor)
                return;

            if (_saveLoop)
                PersistentLoop = Loop;

            if (_saveSingelton)
                PersistentSingleton = Singleton;

            if (_saveVolume)
                PersistentVolume = Volume;

            if (_saveDistances)
            {
                PersistentMinDistance = MinDistance;
                PersistentMaxDistance = MaxDistance;
            }

            if (_savePitch)
                PersistentPitch = Pitch;

            PlayerPrefs.Save();
        }

        private void ReconfigurePlayingAudioSources()
        {
            foreach (AudioSource source in _playingSources.Values)
            {
                if (source == null)
                    return;

                ConfigureParameters(source, true);
            }
        }

        public readonly struct Handle
        {
            private readonly AudioPlayer _player;
            private readonly int _id;

            public Handle(AudioPlayer player, int id)
            {
                _id = id;
                _player = player;
            }

            /// <summary>
            /// Returns true if the audio is currently playing
            /// </summary>
            /// <returns></returns>
            public bool IsPlaying()
            {
                if (_player == null)
                    return false;

                if (_player._playingSources.TryGetValue(_id, out AudioSource source))
                {
                    if (source == null)
                    {
                        _player.Stop(_id);
                        return false;
                    }

                    return source.isPlaying;
                }

                return false;
            }

            /// <summary>
            /// Stops audio player 
            /// </summary>
            /// <returns>true if clip stops, false if clip was already stopped</returns>
            public bool Stop()
            {
                if (_player == null)
                    return false;

                return _player.Stop(_id);
            }
        }
    }
}