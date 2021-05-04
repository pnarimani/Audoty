using UnityEngine;

namespace Audoty
{
    public class PlayAmbientAudio : MonoBehaviour
    {
        [SerializeField] private AudioPlayer _audio;
        private AudioPlayer.Handle _handle;

        private void OnEnable()
        {
            _handle = _audio.Play();
        }

        private void OnDisable()
        {
            _handle.Stop();
        }
    }
}