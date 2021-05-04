using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Audioty
{
    [CreateAssetMenu(fileName = "Audio Player", menuName = "Audio Player", order = 215)]
    public class AudioPlayer : ScriptableObject
    {
        [SerializeField] private AudioClip[] _clips;

        [SerializeField, BoxGroup("Parameters")]
        private bool _loop;

        [SerializeField, BoxGroup("Parameters")]
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

        [TabGroup("Random Clip")]
        [Button(ButtonSizes.Large, ButtonStyle.Box, Expanded = true)]
        public Handle Play(Vector3? position = null)
        {
            if (_clips.Length == 0)
                throw new Exception($"No clips has been set for Audio Player {name}");

            return Play(Random.Range(0, _clips.Length), position);
        }

        [TabGroup("Specific Clip")]
        [Button(ButtonSizes.Large, ButtonStyle.Box, Expanded = true)]
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

            public bool IsPlaying()
            {
                return _player._playingSources.ContainsKey(_id);
            }

            public bool Stop()
            {
                return _player.Stop(_id);
            }
        }
    }
}