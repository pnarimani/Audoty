using UnityEngine;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

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
#if ADDRESSABLES
        [SerializeField] private ReferenceMode _referenceMode;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [ShowIf(nameof(_referenceMode), ReferenceMode.AssetReference)]
#endif
        [SerializeField]
        private AssetReferenceAudioPlayer _audioReference;
#endif

#if ADDRESSABLES && (ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES)
        [ShowIf(nameof(_referenceMode), ReferenceMode.DirectReference)]
#endif
        [SerializeField]
        private AudioPlayer _audio;

        [SerializeField] private bool _useRandomClip = true;

#if UNITY_EDITOR && (ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES)
        [HideIf(nameof(UseRandomClip)), ValueDropdown(nameof(ClipNames))]
#endif
        // Don't include SerializeField in #if because it can cause some problems on IL2CPP
        [SerializeField]
        private string _clipName = "";

        [SerializeField, HideInInspector] private int _clipIndex;

#if ADDRESSABLES
        private AsyncOperationHandle<AudioPlayer> _assetReferenceLoadOp;
#endif

        public AudioPlayer Audio
        {
            get => _audio;
            set
            {
                _audio = value;
#if ADDRESSABLES
                _referenceMode = ReferenceMode.DirectReference;
#endif
            }
        }

#if ADDRESSABLES
        public AssetReferenceAudioPlayer AudioReference
        {
            get => _audioReference;
            set
            {
                _audioReference = value;
                _referenceMode = ReferenceMode.AssetReference;
            }
        }
#endif

        protected int ClipIndex => _clipIndex;

        protected bool UseRandomClip => _useRandomClip;

        protected bool IsAudioPlayerReady { get; private set; }
        protected AudioPlayer AudioPlayerToUse { get; private set; }


#if UNITY_EDITOR
        private string[] ClipNames
        {
            get
            {
                AudioPlayer a = GetActiveAudioPlayer();
                // Because of NaughtyAttributes, we need to check if ClipNames are empty or not.
                return a == null || a.ClipNames.Length == 0 ? new string[] {""} : a.ClipNames;
            }
        }
#endif

        private void Awake()
        {
#if ADDRESSABLES
            if (_referenceMode == ReferenceMode.DirectReference)
            {
                IsAudioPlayerReady = true;
                AudioPlayerToUse = _audio;
            }
            else
            {
                _assetReferenceLoadOp = Addressables.LoadAssetAsync<AudioPlayer>(_audioReference);
                _assetReferenceLoadOp.Completed += handle =>
                {
                    IsAudioPlayerReady = true;
                    AudioPlayerToUse = handle.Result;
                };
            }
#else
            IsAudioPlayerReady = true;
            AudioPlayerToUse = _audio;
#endif
        }

        private void OnDestroy()
        {
#if ADDRESSABLES
            if (_referenceMode == ReferenceMode.AssetReference)
            {
                Addressables.Release(_assetReferenceLoadOp);
            }
#endif
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (GetActiveAudioPlayer() == null)
            {
                _clipName = null;
                _clipIndex = -1;
                return;
            }

            if (string.IsNullOrEmpty(_clipName))
            {
                _clipIndex = -1;
            }
            else
            {
                _clipIndex = GetActiveAudioPlayer().FindIndex(_clipName);
            }
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (GetActiveAudioPlayer() == null)
                return;

            if (_clipIndex >= 0)
            {
                _clipName = GetActiveAudioPlayer().ClipNames[_clipIndex];
            }
            else
            {
                _clipName = "";
            }
#endif
        }

#if UNITY_EDITOR
        private AudioPlayer GetActiveAudioPlayer()
        {
#if ADDRESSABLES
            return _referenceMode == ReferenceMode.AssetReference ? _audioReference?.editorAsset : _audio;
#else       
            return _audio;
#endif
        }
#endif
    }
}