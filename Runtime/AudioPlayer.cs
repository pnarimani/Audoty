using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Audoty
{
    [CreateAssetMenu(fileName = "Audio Player", menuName = "Audio Player", order = 215)]
    public class AudioPlayer : ScriptableObject
    {
        [SerializeField] private List<AudioClip> _clips;

        [SerializeField, BoxGroup("Parameters")]
        private bool _saveParameters;

        [SerializeField, BoxGroup("Parameters")]
        private bool _loop;

        [SerializeField]
        [BoxGroup("Parameters")]
        [Tooltip("When true, only one instance of this AudioPlayer will be played")]
        private bool _singleton;

        [SerializeField, Range(0, 1), BoxGroup("Parameters")]
        private float _volume = 1;

        [SerializeField, BoxGroup("Parameters")]
        private float _minDistance = 1, _maxDistance = 500;

        [SerializeField, MinMaxSlider(-3, 3), BoxGroup("Parameters")]
        private Vector2 _pitch = Vector2.one;

        private static readonly Queue<AudioSource> Pool = new Queue<AudioSource>();
        private readonly Dictionary<int, AudioSource> _playingSources = new Dictionary<int, AudioSource>();

        private int _nextId;
        private Handle? _singletonHandle;

        public List<AudioClip> Clips
        {
            get => _clips;
            set => _clips = value;
        }

        public bool SaveParameters => _saveParameters;

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

        private string PersistentPrefix
        {
            get
            {
                string clip = _clips.Count > 0 ? _clips[0].name : "null";
                return name + "_" + clip + "_";
            }
        }

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

            if (!_saveParameters)
                return;

            Loop = PersistentLoop;
            Singleton = PersistentSingleton;
            Volume = PersistentVolume;
            MinDistance = PersistentMinDistance;
            MaxDistance = PersistentMaxDistance;
            Pitch = PersistentPitch;
        }

        private void OnDisable()
        {
            if (Application.isEditor)
                return;

            if (!_saveParameters)
                return;

            PersistentLoop = Loop;
            PersistentSingleton = Singleton;
            PersistentVolume = Volume;
            PersistentMinDistance = MinDistance;
            PersistentMaxDistance = MaxDistance;
            PersistentPitch = Pitch;
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Plays a random clip Fire & Forget style
        /// </summary>
        [Button("Play Random", ButtonSizes.Large)]
        public void PlayForget()
        {
            Play();
        }

        /// <summary>
        /// Plays a specific clip Fire & Forget style
        /// </summary>
        /// <param name="index"></param>
        [Button("Play Specific", ButtonSizes.Large, ButtonStyle.Box, Expanded = true)]
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
        /// <exception cref="Exception">When randomly selected clip is null</exception>
        public Handle Play(Vector3? position = null)
        {
            if (_clips.Count == 0)
                throw new Exception($"No clips has been set for Audio Player {name}");

            return Play(Random.Range(0, _clips.Count), position);
        }

        /// <summary>
        /// Plays a given clip, optionally at a position, and returns a handle which can be used to stop the clip.
        /// If the position is provided, audio will be 3D, otherwise, audio will be played 2D 
        /// </summary>
        /// <param name="index">The index of the clip to play</param>
        /// <param name="position">Position to play the clip at.</param>
        /// <returns></returns>
        /// <exception cref="Exception">When selected clip is null</exception>
        public Handle Play(int index, Vector3? position = null)
        {
            // If this instance is singleton, and there's an instance of audio that is playing, return that instance of audio
            if (_singleton && _singletonHandle != null && _singletonHandle.Value.IsPlaying())
                return _singletonHandle.Value;

            var audioSource = Spawn(position);

            AudioClip clip = _clips[index];

            if (clip == null)
                throw new Exception($"Null clip in {name} audio player");

            if (position != null)
                audioSource.transform.position = position.Value;

            audioSource.clip = clip;
            audioSource.Play();

            int id = _nextId;
            _nextId++;

            if (!_loop)
                DespawnAfter(id, clip.length).Forget();

            _playingSources.Add(id, audioSource);

            var handle = new Handle(this, id);

            if (_singleton)
                _singletonHandle = handle;

            return handle;
        }

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

        private async UniTask DespawnAfter(int id, float time)
        {
            await UniTask.Delay((int) (time * 1000));
            Stop(id);
        }

        public struct Handle
        {
            private AudioPlayer _player;
            private int _id;

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