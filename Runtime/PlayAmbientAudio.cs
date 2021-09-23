using System.Collections;
using UnityEngine;

namespace Audoty
{
    /// <summary>
    /// Starts playing an AudioPlayer when gets enabled and stops the audio when gets disabled.
    /// </summary>
    public class PlayAmbientAudio : ScenePlayerBase
    {
        [Tooltip("If tracking target is provided, audio will be played in 3D, otherwise audio will be played in 2D")]
        [SerializeField]
        private Transform _trackingTarget;
        
        [SerializeField] private bool _stopOnDisable = true;

        private AudioHandle _handle;
        private Coroutine _coroutine;

        private void OnEnable()
        {
            _coroutine = StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            yield return new WaitUntil(() => IsAudioPlayerReady);
            
            if (AudioPlayerToUse == null)
            {
                Debug.LogError("PlayAmbientAudio does not have AudioPlayer assigned.", this);
                yield break;
            }

            if (AudioPlayerToUse.Clips.Count == 0)
                yield break;

            int index = UseRandomClip ? Random.Range(0, AudioPlayerToUse.Clips.Count) : ClipIndex;

            if (index == -1)
            {
                Debug.LogError("No clip is selected in PlayAmbientAudio", this);
                yield break;
            }

            _handle = AudioPlayerToUse.Play(index, tracking: _trackingTarget);
        }

        private void OnDisable()
        {
            if (_stopOnDisable)
            {
                StopCoroutine(_coroutine);
                _handle.Stop();
            }
        }
    }
}