using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

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

        [SerializeField, BoxGroup("Parameters"), ShowIf(nameof(_singleton))]
        [Tooltip(
            "When true, a live singleton audio source will be interrupted to play a new clip (from the same Audio Player)")]
        private bool _allowInterrupt = true;

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

        [Space] [SerializeField, BoxGroup("Parameters")]
        private float _dopplerLevel;

        [SerializeField, BoxGroup("Parameters")]
        [Tooltip("When live link is enabled, changes to the parameter will apply to existing/live audio sources.")]
        private bool _liveLinkDopplerLevel = true;

        [SerializeField, BoxGroup("Parameters")]
        private bool _saveDopplerLevel;

        [Space]
        [SerializeField, BoxGroup("Parameters")]
        [Tooltip("Volume fade-in time when AudioPlayer plays an audio")]
        private float _playFadeTime;

        [SerializeField, BoxGroup("Parameters")]
        [Tooltip(
            "When AudioPlayer gets interrupted (stopped mid playing), instead of cutting the audio, audio will fade out")]
        private float _interruptFadeTime = 0.2f;

        [SerializeField, HideInInspector] private int _randomizedSaveKey;

        internal readonly Dictionary<int, AudioSource> _playingSources = new Dictionary<int, AudioSource>();

        private int _nextId;
        private AudioHandle? _singletonHandle;

#if UNITY_EDITOR
        // Keep record of keys to make sure there is no conflict
        private static readonly Dictionary<int, AudioPlayer> SaveKeys = new Dictionary<int, AudioPlayer>();
        private AudioHandle? _lastPlayedAudio;
#endif

        public IReadOnlyList<AudioClip> Clips => _clips;

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

        public float DopplerLevel
        {
            get => _dopplerLevel;
            set => _dopplerLevel = value;
        }


        /// <summary>
        /// When true, a live singleton audio source will be interrupted to play a new clip from the same AudioPlayer
        /// </summary>
        public bool AllowInterrupt
        {
            get => _allowInterrupt;
            set => _allowInterrupt = value;
        }

        /// <summary>
        /// Volume fade-in time when AudioPlayer plays an audio
        /// </summary>
        public float PlayFadeTime
        {
            get => _playFadeTime;
            set => _playFadeTime = value;
        }

        /// <summary>
        /// When AudioPlayer gets interrupted (stopped mid playing), instead of cutting the audio, audio will fade out
        /// </summary>
        public float InterruptFadeTime
        {
            get => _interruptFadeTime;
            set => _interruptFadeTime = value;
        }

#if UNITY_EDITOR
        internal string[] ClipNames => _clips?.Select(x => x.name).ToArray();
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

        private float PersistentDopplerLevel
        {
            get => PlayerPrefs.GetFloat(PersistentPrefix + "dopplerLevel", _dopplerLevel);
            set => PlayerPrefs.SetFloat(PersistentPrefix + "dopplerLevel", value);
        }


        private void OnEnable()
        {
            LoadParameters();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            int[] keys = _playingSources.Keys.ToArray();
            foreach (int id in keys)
            {
                Stop(id, 0);
            }
#endif
        }

        /// <summary>
        /// Plays a random clip Fire & Forget style
        /// </summary>
        [Button("Play Random", ButtonSizes.Large)]
#if !USE_UNITASK && !USE_EDITOR_COROUTINES
        [InfoBox(
            "You need to install UniTask or Editor Coroutines package to use AudioPlayer in Edit Mode.\n" +
            "(There will be no problems in play mode)",
            InfoMessageType.Error,
            VisibleIf = "@UnityEngine.Application.isPlaying == false")]
#endif
        public void PlayForget()
        {
            PlayForget(Random.Range(0, _clips.Count));
        }

        /// <summary>
        /// Plays a specific clip Fire & Forget style
        /// </summary>
        /// <param name="index"></param>
        public void PlayForget(int index)
        {
            Play(index);
        }

        /// <summary>
        /// Finds and Plays a clip by clip name in Fire & Forget style
        /// </summary>
        /// <param name="clipName"></param>
        [Button("Play Specific", ButtonSizes.Large, ButtonStyle.Box, Expanded = true)]
        public void PlayForget([ValueDropdown("ClipNames")] string clipName)
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
        /// Finds and plays a given clip, optionally at a position, and returns a handle which can be used to stop the clip.
        /// If clipName is not given, a random clip will be chosen.
        /// If position or tracking is provided, audio will be 3D, otherwise, audio will be played 2D 
        /// </summary>
        /// <param name="clipName">The name of clip to play</param>
        /// <param name="position">Position to play the clip at.</param>
        /// <param name="tracking">Audio player will track this transform's movement</param>
        /// <param name="delay">Delay in seconds before AudioPlayer actually plays the audio. AudioPlayers in delay are considered playing/live</param>
        /// <returns></returns>
        public AudioHandle Play(string clipName = null, Vector3? position = null, Transform tracking = null, float delay = 0)
        {
            if (_clips.Count == 0)
                throw new NoClipsFoundException(this);

            int index;

            if (string.IsNullOrEmpty(clipName))
            {
                index = Random.Range(0, _clips.Count);
            }
            else
            {
                index = FindIndex(clipName);

                if (index == -1)
                    throw new ClipNotFoundException(this, clipName);
            }

            return Play(index, position, tracking, delay);
        }

        /// <summary>
        /// Plays a given clip, optionally at a position, and returns a handle which can be used to stop the clip.
        /// If position or tracking is provided, audio will be 3D, otherwise, audio will be played 2D.
        /// </summary>
        /// <param name="index">The index of the clip to play</param>
        /// <param name="position">Position to play the clip at.</param>
        /// <param name="tracking">Audio player will track this transform's movement</param>
        /// <param name="delay">Delay in seconds before AudioPlayer actually plays the audio. AudioPlayers in delay are considered playing/live</param>
        /// <returns></returns>
        public AudioHandle Play(int index, Vector3? position = null, Transform tracking = null, float delay = 0)
        {
            if (_clips.Count == 0)
                throw new NoClipsFoundException(this);

#if UNITY_EDITOR
            CheckSaveKeyConflict();
#endif

            // If this instance is singleton, and there's an instance of audio that is playing, return that instance of audio
            if (_singleton && _singletonHandle != null && _singletonHandle.Value.IsPlaying())
            {
                if (!_allowInterrupt || index == _singletonHandle.Value.ClipIndex)
                    return _singletonHandle.Value;

                _singletonHandle.Value.Stop();
            }


            AudioClip clip = _clips[index];

            if (clip == null)
                throw new ClipNullException(this, index);

            int id = _nextId;
            _nextId++;
            var handle = new AudioHandle(this, id, index);

            AudioSource audioSource = AudioPool.Spawn(handle, position, tracking, _loop ? -1 : clip.length);

            ConfigureParameters(audioSource, false);
            audioSource.clip = clip;
            if (delay <= 0)
                audioSource.Play();
            else
                audioSource.PlayDelayed(delay);

            Fade.In(audioSource, audioSource.volume, _playFadeTime, delay);

            _playingSources.Add(id, audioSource);

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

        public int FindIndex(string clipName)
        {
            return _clips.FindIndex(x => x.name == clipName);
        }

        internal bool Stop(int id, float fadeTime)
        {
            if (_playingSources.TryGetValue(id, out AudioSource source))
            {
                if (source != null)
                {
                    Fade.Out(source, fadeTime);
                }

                _playingSources.Remove(id);
                return true;
            }

            return false;
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

            if (!live || _liveLinkDopplerLevel)
                source.dopplerLevel = _dopplerLevel;
        }

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

                    Debug.LogError(
                        $"[Audoty] Found conflicting save keys between existing Audio Player `{existingName}` and `{name}`. Resolving the conflict by changing save key of {name}");
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

            if (_saveDopplerLevel)
                DopplerLevel = PersistentDopplerLevel;
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

            if (_saveDopplerLevel)
                PersistentDopplerLevel = DopplerLevel;

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
    }
}