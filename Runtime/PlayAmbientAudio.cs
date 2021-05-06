using Sirenix.OdinInspector;
using UnityEngine;

namespace Audoty
{
    /// <summary>
    /// Starts playing an AudioPlayer when gets enabled and stops the audio when gets disabled.
    /// </summary>
    public class PlayAmbientAudio : MonoBehaviour
    {
        [Tooltip("If target position is provided, audio will be played in 3D, otherwise audio will be played in 2D")]
        [SerializeField]
        private Transform _targetPosition;

        [SerializeField] private bool _useRandomClip = true;

        [SerializeField, HideIf(nameof(_useRandomClip))] private int _clipIndex;
        
        [SerializeField] private AudioPlayer _audio;

        private AudioPlayer.Handle _handle;

        private void OnEnable()
        {
            if (_audio == null)
                return;

            Vector3? pos = _targetPosition != null ? _targetPosition.position : (Vector3?) null;
            int index = _useRandomClip ? Random.Range(0, _audio.Clips.Count) : _clipIndex;
            _handle = _audio.Play(index, pos);
        }

        private void OnDisable()
        {
            if (_audio == null)
                return;

            _handle.Stop();
        }
    }
}