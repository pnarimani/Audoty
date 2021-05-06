using System.Collections.Generic;
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
        private bool _saveVolume;

        [Space] [SerializeField, BoxGroup("Parameters")]
        private float _minDistance = 1;

        [SerializeField, BoxGroup("Parameters")]
        private float _maxDistance = 500;

        [SerializeField, BoxGroup("Parameters")]
        private bool _saveDistances;

        [Space] [SerializeField, MinMaxSlider(-3, 3), BoxGroup("Parameters")]
        private Vector2 _pitch = Vector2.one;

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
            set => _loop = value;
        }

        public bool Singleton
        {
            get => _singleton;
            set => _singleton = value;
        }

        public float Volume
        {
            get => _volume;
            set => _volume = value;
        }

        public float MinDistance
        {
            get => _minDistance;
            set => _minDistance = value;
        }

        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = value;
        }

        public Vector2 Pitch
        {
            get => _pitch;
            set => _pitch = value;
        }

#if UNITY_EDITOR
        private bool ShowStopButton => _lastPlayedAudio != null && _lastPlayedAudio.Value.IsPlaying();
#endif
        
        private string PersistentPrefix => name + "_" + _randomizedSaveKey + "_";

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

        private void OnDisable()
        {
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

        /// <summary>
        /// Plays a random clip Fire & Forget style
        /// </summary>
        [Button("Play Random", ButtonSizes.Large)]
#if !USE_UNITASK && !USE_EDITOR_COROUTINES
        [InfoBox("In order to return Audio Sources to pool in Edit Mode, you need to install UniTask or Editor Coroutines package.\n(There will be no problems in play mode)", InfoMessageType.Error, VisibleIf = "@UnityEngine.Application.isPlaying == false")]
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
        /// Plays a given clip, optionally at a position, and returns a handle which can be used to stop the clip.
        /// If the position is provided, audio will be 3D, otherwise, audio will be played 2D 
        /// </summary>
        /// <param name="index">The index of the clip to play</param>
        /// <param name="position">Position to play the clip at.</param>
        /// <returns></returns>
        public Handle Play(int index, Vector3? position = null)
        {
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
                source.Stop();
                Pool.Enqueue(source);
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

            source.pitch = Random.Range(_pitch.x, _pitch.y);
            source.volume = _volume;
            source.minDistance = _minDistance;
            source.maxDistance = _maxDistance;
            source.spatialBlend = position != null ? 1 : 0;
            source.loop = _loop;
            source.transform.position = position ?? Vector3.zero;

            return source;
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
        private void Reset()
        {
            while (_randomizedSaveKey == 0)
                _randomizedSaveKey = Random.Range(int.MinValue + 1, int.MaxValue - 1);
        }

        private void OnValidate()
        {
            while (_randomizedSaveKey == 0)
                _randomizedSaveKey = Random.Range(int.MinValue + 1, int.MaxValue - 1);
        }
#endif

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

                return _player._playingSources.ContainsKey(_id);
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