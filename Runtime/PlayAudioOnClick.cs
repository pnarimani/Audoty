using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Audoty
{
    /// <summary>
    /// Plays an AudioPlayer when receives OnPointerClick callback.
    /// It will not play the AudioPlayer if it's attached to a Selectable component which is not interactable or disabled. 
    /// </summary>
    public class PlayAudioOnClick : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        [SerializeField] private AudioPlayer _audio;
        [SerializeField] private bool _useRandomClip = true;

        [SerializeField, HideIf(nameof(_useRandomClip))]
        private int _clipIndex;

        private Selectable _selectable;
        private bool _play;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_play)
            {
                int index = _useRandomClip ? Random.Range(0, _audio.Clips.Count) : _clipIndex;
                _audio.PlayForget(index);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _play = _selectable == null || _selectable.interactable && _selectable.enabled;
        }
    }
}