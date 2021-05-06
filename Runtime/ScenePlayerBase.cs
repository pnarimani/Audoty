using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Audoty
{
    public abstract class ScenePlayerBase : MonoBehaviour
    {
        [SerializeField] private AudioPlayer _audio;
        [SerializeField] private bool _useRandomClip = true;
        
        [HideIf(nameof(UseRandomClip))]
        [ValueDropdown("@_audio == null ? new string[0] : _audio.ClipNames")]
        [SerializeField]
        private string _clipName;

        [SerializeField, HideInInspector] 
        private int _clipIndex;

        protected AudioPlayer Audio => _audio;

        protected  int ClipIndex => _clipIndex;

        protected bool UseRandomClip => _useRandomClip;

#if UNITY_EDITOR
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
                _clipIndex = Audio.Clips.FindIndex(x => x.name == _clipName);
        }
#endif
    }
}