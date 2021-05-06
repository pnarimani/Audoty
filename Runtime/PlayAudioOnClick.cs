using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Audoty
{
    public class PlayAudioOnClick : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        [SerializeField] private AudioPlayer _audio;

        private Selectable _selectable;
        private bool _play;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_play)
                _audio.PlayForget();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _play = _selectable == null || _selectable.interactable && _selectable.enabled;
        }
    }
}