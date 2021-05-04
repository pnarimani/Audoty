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
        private static readonly Queue<AudioSource> Pool = new Queue<AudioSource>();

        [SerializeField] private AudioClip[] _clips;
        [SerializeField, BoxGroup("Parameters")] private bool _isAmbient;
        [SerializeField, Range(0, 1), BoxGroup("Parameters")] private float _volume = 1;
        [SerializeField, BoxGroup("Parameters")] private float _minDistance = 1, _maxDistance = 500;
        [SerializeField, MinMaxSlider(-3, 3), BoxGroup("Parameters")] private Vector2 _pitch = Vector2.one;


        // If the AudioPlayer is ambient, then we only want to play one instance of the audio
        private bool _isPlayingAmbient;

        [TabGroup("Random Clip")]
        [Button(ButtonSizes.Large, ButtonStyle.Box, Expanded = true)]
        public void Play(Vector3? position = null)
        {
            if (_clips.Length == 0)
                throw new Exception($"No clips has been set for Audio Player {name}");

            Play(Random.Range(0, _clips.Length), position);
        }

        [TabGroup("Specific Clip")]
        [Button(ButtonSizes.Large, ButtonStyle.Box, Expanded = true)]
        public void Play(int index, Vector3? position = null)
        {
            if (_isPlayingAmbient)
                return;

            var audioSource = Spawn();

            ConfigureAudioSource(audioSource, position != null);

            AudioClip c = _clips[index];

            if (c == null)
                throw new Exception($"Null clip in {name} audio player");

            if (position != null)
                audioSource.transform.position = position.Value;
            
            audioSource.clip = c;
            audioSource.Play();

            if (_isAmbient)
                _isPlayingAmbient = true;
            else
                Despawn(audioSource, c.length).Forget();
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
