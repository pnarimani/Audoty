using System.Collections.Generic;
using UnityEngine;

#if USE_UNITASK
using Cysharp.Threading.Tasks;

#else
using System.Collections;

#endif

#if USE_EDITOR_COROUTINES && UNITY_EDITOR
using Unity.EditorCoroutines.Editor;

#endif

namespace Audoty
{
    internal static class AudioPool
    {
        public static readonly Queue<AudioSource> Pool = new Queue<AudioSource>();

        public static AudioSource Spawn(AudioHandle handle, Vector3? position, Transform parent, float despawnTime)
        {
            AudioSource source;
            if (Pool.Count > 0)
            {
                source = Pool.Dequeue();
            }
            else
            {
                var go = new GameObject("Audio Source", typeof(AudioSource))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                if (Application.isPlaying)
                    Object.DontDestroyOnLoad(go);

                source = go.GetComponent<AudioSource>();
            }

            source.spatialBlend = (position != null || parent != null) ? 1 : 0;
            Transform t = source.transform;
            t.SetParent(parent);
            t.localPosition = position ?? Vector3.zero;

            if (despawnTime > 0)
                DespawnAfter(handle, despawnTime);
            
            return source;
        }

        private static void DespawnAfter(AudioHandle handle, float time)
        {
            // Select despawn strategy based on what packages are present
#if USE_UNITASK
            DespawnAfterInternal(handle, time).Forget();
#else
#if USE_EDITOR_COROUTINES && UNITY_EDITOR
                if (Application.isPlaying)
                    CoroutineRunner.RunCoroutine(DespawnAfterInternal(handle, time));
                else
                    EditorCoroutineUtility.StartCoroutineOwnerless(DespawnAfterInternal(handle, time));
#else
                CoroutineRunner.RunCoroutine(DespawnAfterInternal(handle, time));
#endif

#endif
        }

#if USE_UNITASK
        private static async UniTask DespawnAfterInternal(AudioHandle handle, float time)
        {
            await UniTask.Delay((int) (time * 1000));
            handle.Stop(0);
        }
#else
        private static IEnumerator DespawnAfterInternal(AudioHandle handle, float time)
        {
#if USE_EDITOR_COROUTINES && UNITY_EDITOR
            if (!Application.isPlaying)
            {
                yield return new EditorWaitForSeconds(time);
            }
            else
            {
                yield return new WaitForSeconds(time);
            }
#else
            yield return new WaitForSeconds(time);
#endif
            handle.Stop(0);
        }
#endif
    }
}