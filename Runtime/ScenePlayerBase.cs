using System;
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
        // Because of shitty NaughtyAttributes, we need to check if ClipNames are empty or not.
        private string[] ClipNames => _audio == null || _audio.ClipNames.Length == 0 ? new string[] {""} : _audio.ClipNames;

        private void OnValidate()
        {

        }
#endif

        void ISerializationCallbackReceiver.OnBeforeSerialize()
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

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (_audio == null)
                return;

            if (_clipIndex >= 0)
                _clipName = Audio.ClipNames[_clipIndex];
            else
                _clipName = "";
#endif
        }
    }
}