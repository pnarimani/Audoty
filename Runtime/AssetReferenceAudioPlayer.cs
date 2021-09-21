using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

#if UNITASK
using AudioHandleTask = Cysharp.Threading.Tasks.UniTask<Audoty.AudioHandle>;
using AudioPlayerTask = Cysharp.Threading.Tasks.UniTask<Audoty.AudioPlayer>;

#else

using AudioHandleTask = System.Threading.Tasks.Task<Audoty.AudioHandle>;
using AudioPlayerTask = System.Threading.Tasks.Task<Audoty.AudioPlayer>;
#endif

#if ADDRESSABLES

namespace Audoty
{
    public class AssetReferenceAudioPlayer : AssetReferenceT<AudioPlayer>
    {
        public AssetReferenceAudioPlayer(string guid) : base(guid)
        {
        }

        /// <inheritdoc cref="AudioPlayer.Play(string,System.Nullable{UnityEngine.Vector3},UnityEngine.Transform,float)"/>
        public async AudioHandleTask Play(
            string clipName = null,
            Vector3? position = null,
            Transform tracking = null,
            float delay = 0)
        {
            return (await LoadAudioPlayer()).Play(clipName, position, tracking, delay);
        }

        /// <inheritdoc cref="AudioPlayer.Play(int,System.Nullable{UnityEngine.Vector3},UnityEngine.Transform,float)"/>
        public async AudioHandleTask Play(int index, Vector3? position = null, Transform tracking = null, float delay = 0)
        {
            return (await LoadAudioPlayer()).Play(index, position, tracking, delay);
        }

        /// <inheritdoc cref="AudioPlayer.StopSingleton"/>
        public async void StopSingleton()
        {
            (await LoadAudioPlayer()).StopSingleton();
        }

        public async AudioPlayerTask LoadAudioPlayer()
        {
            AudioPlayer player;

            if (IsValid())
            {
#if UNITASK
                await OperationHandle.ToUniTask();
#else
                await OperationHandle.Task;
#endif

                player = (AudioPlayer) OperationHandle.Result;
            }
            else
            {
                player = await LoadAssetAsync();
            }

            return player;
        }
    }
}
#endif