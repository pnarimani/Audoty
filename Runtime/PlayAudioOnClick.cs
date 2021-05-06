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
    public class PlayAudioOnClick : ScenePlayerBase, IPointerClickHandler, IPointerDownHandler
    {
        private Selectable _selectable;
        private bool _play;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_play) return;

            if (Audio == null)
            {
                Debug.LogError("PlayAudioOnClick does not have AudioPlayer assigned.", this);
                return;
            }

            if (Audio.Clips.Count == 0)
                return;

            int index = UseRandomClip ? Random.Range(0, Audio.Clips.Count) : ClipIndex;

            if (index == -1)
            {
                Debug.LogError("No clip is selected in PlayAudioOnClick", this);
                return;
            }

            Audio.PlayForget(index);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _play = _selectable == null || _selectable.interactable && _selectable.enabled;
        }
    }
}