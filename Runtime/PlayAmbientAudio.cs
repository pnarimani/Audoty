using UnityEngine;

namespace Audoty
{
    public class PlayAmbientAudio : MonoBehaviour
    {
        [Tooltip("If target position is provided, audio will be played in 3D, otherwise audio will be played in 2D")]
        [SerializeField]
        private Transform _targetPosition;

        [SerializeField] private AudioPlayer _audio;

        private AudioPlayer.Handle _handle;

        private void OnEnable()
        {
            if (_audio == null)
                return;
            
            _handle = _targetPosition != null ? _audio.Play(_targetPosition.position) : _audio.Play();
        }

        private void OnDisable()
        {
            if (_audio == null)
                return;
            
            _handle.Stop();
        }
    }
}