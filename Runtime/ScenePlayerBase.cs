using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;

#elif NAUGHTY_ATTRIBUTES
using NaughtyAttributes;
using ValueDropdown = NaughtyAttributes.DropdownAttribute;

#endif

namespace Audoty
{
    public abstract class ScenePlayerBase : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField] private AudioPlayer _audio;
        [SerializeField] private bool _useRandomClip = true;

#if UNITY_EDITOR && (ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES)
        [HideIf(nameof(UseRandomClip)), ValueDropdown(nameof(ClipNames))] 
#endif
        // Don't include SerializeField in #if because it can cause some problems on IL2CPP
        [SerializeField]
        private string _clipName = "";  

        [SerializeField, HideInInspector] private int _clipIndex;

        protected AudioPlayer Audio => _audio;

        protected int ClipIndex => _clipIndex;

        protected bool UseRandomClip => _useRandomClip;


#if UNITY_EDITOR
        private string[] ClipNames => _audio == null ? new string[] {null} : _audio.ClipNames;
#endif

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (_audio == null)
            {
                _clipName = "";
                _clipIndex = -1;
                return;
            }

            if (string.IsNullOrEmpty(_clipName))
                _clipIndex = -1;
            else
                _clipIndex = Audio.FindIndex(_clipName);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_audio == null)
                return;

            _clipName = Audio.Clips[_clipIndex].name;
        }
    }
}