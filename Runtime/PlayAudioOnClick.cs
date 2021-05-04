using UnityEngine;
using UnityEngine.EventSystems;

namespace Audoty
{
    public class PlayAudioOnClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private AudioPlayer _audio;

        public void OnPointerClick(PointerEventData eventData)
        {
            _audio.PlayForget();
        }
    }
}