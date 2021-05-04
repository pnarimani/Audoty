using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Audioty
{
    [CreateAssetMenu(fileName = "Audio Player", menuName = "Audio Player", order = 40)]
    public class AudioPlayer : ScriptableObject
    {
        private static readonly Queue<AudioSource> Pool = new Queue<AudioSource>();

        [SerializeField] private AudioClip[] _clips;
        [SerializeField, Range(0, 1)] private float _volume = 1;
        [SerializeField] private float _minDistance = 1, _maxDistance = 500;
        [SerializeField] private bool _isAmbient;
        [SerializeField, MinMaxSlider(-3, 3)] private Vector2 _pitch = Vector2.one;


        // If the AudioPlayer is ambient, then we only want to play one instance of the audio
        private bool _isPlaying;

        [Button]
        public void Play()
        {
            if (_clips.Length == 0)
                throw new Exception($"No clips has been set for Audio Player {name}");

            Play(Random.Range(0, _clips.Length));
        }

        [Button]
        public void Play(int index)
        {
            if (_isPlaying)
                return;

            var audioSource = Spawn();

            ConfigureAudioSource(audioSource, false);

            AudioClip c = _clips[index];

            if (c == null)
                throw new Exception($"Null clip in {name} audio player");

            audioSource.clip = c;
            audioSource.Play();
            _isPlaying = true;

            if (!_isAmbient)
                Despawn(audioSource, c.length).Forget();
            else
                _isPlaying = true;
        }

        public void Play(Vector3 position)
        {
            if (_clips.Length == 0)
                throw new Exception($"No clips has been set for Audio Player {name}");

            Play(Random.Range(0, _clips.Length), position);
        }

        public void Play(int index, Vector3 position)
        {
            if (_isPlaying)
                return;

            var audioSource = Spawn();

            ConfigureAudioSource(audioSource, true);
            audioSource.transform.position = position;

            var c = _clips[index];

            if (c == null)
                throw new Exception($"Null clip in {name} audio player");

            audioSource.clip = c;
            audioSource.Play();


            if (!_isAmbient)
                Despawn(audioSource, c.length).Forget();
            else
                _isPlaying = true;
        }

        private void ConfigureAudioSource(AudioSource source, bool spatial)
        {
            source.pitch = Random.Range(_pitch.x, _pitch.y);
            source.volume = _volume;
            source.minDistance = _minDistance;
            source.maxDistance = _maxDistance;
            source.spatialBlend = spatial ? 1 : 0;
            source.loop = _isAmbient;
        }

        private static AudioSource Spawn()
        {
            if (Pool.Count > 0)
                return Pool.Dequeue();

            var go = new GameObject("Audio Source", typeof(AudioSource))
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            if (Application.isPlaying)
                DontDestroyOnLoad(go);

            return go.GetComponent<AudioSource>();
        }

        private static async UniTask Despawn(AudioSource source, float time)
        {
            await UniTask.Delay((int) (time * 1000));
            Pool.Enqueue(source);
        }
    }
}