using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#elif NAUGHTY_ATTRIBUTES
using NaughtyAttributes;
using ValueDropdown = NaughtyAttributes.DropdownAttribute;
#endif

namespace Audoty
{
    public abstract class ScenePlayerBase : MonoBehaviour
    {
        [SerializeField] private AudioPlayer _audio;
        [SerializeField] private bool _useRandomClip = true;

#if UNITY_EDITOR && (ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES)
        [HideIf(nameof(UseRandomClip))]
        [ValueDropdown(nameof(ClipNames))]
#endif
        [SerializeField]
        private string _clipName;

        [SerializeField, HideInInspector] private int _clipIndex;
        
        protected AudioPlayer Audio => _audio;

        protected int ClipIndex => _clipIndex;

        protected bool UseRandomClip => _useRandomClip;

        
#if UNITY_EDITOR
        private string[] ClipNames => _audio == null ? new string[0] : _audio.ClipNames;
        
        private void OnValidate()
        {
            if (_audio == null)
            {
                _clipName = null;
                _clipIndex = -1;
                return;
            }

            if (string.IsNullOrEmpty(_clipName))
                _clipIndex = -1;
            else
                _clipIndex = Audio.FindIndex(_clipName);
        }
#endif
    }
}