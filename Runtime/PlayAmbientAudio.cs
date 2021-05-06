using UnityEngine;

namespace Audoty
{
    /// <summary>
    /// Starts playing an AudioPlayer when gets enabled and stops the audio when gets disabled.
    /// </summary>
    public class PlayAmbientAudio : ScenePlayerBase
    {
        [Tooltip("If target position is provided, audio will be played in 3D, otherwise audio will be played in 2D")]
        [SerializeField]
        private Transform _targetPosition;
        
        [SerializeField] private bool _stopOnDisable = true;

        private AudioPlayer.Handle _handle;

        private void OnEnable()
        {
            if (Audio == null)
            {
                Debug.LogError("PlayAmbientAudio does not have AudioPlayer assigned.", this);
                return;
            }

            if (Audio.Clips.Count == 0)
                return;

            Vector3? pos = _targetPosition != null ? _targetPosition.position : (Vector3?) null;
            int index = UseRandomClip ? Random.Range(0, Audio.Clips.Count) : ClipIndex;

            if (index == -1)
            {
                Debug.LogError("No clip is selected in PlayAmbientAudio", this);
                return;
            }
            
            _handle = Audio.Play(index, pos);
        }

        private void OnDisable()
        {
            if (Audio == null)
                return;

            if (_stopOnDisable)
                _handle.Stop();
        }
    }
}