using System.Collections.Generic;
using UnityEngine;
#if UNITASK
using Cysharp.Threading.Tasks;
#else
using System.Collections;
#endif
#if EDITOR_COROUTINES && UNITY_EDITOR
using Unity.EditorCoroutines.Editor;

#endif

namespace Audoty
{
    internal static class AudioPool
    {
        private static readonly Queue<AudioSource> Pool = new Queue<AudioSource>();

        public static AudioSource Spawn(AudioHandle handle, Vector3? position, Transform tracking, float despawnTime)
        {
            AudioSource source;
            if (Pool.Count > 0)
            {
                source = Pool.Dequeue();
                source.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("Audio Source", typeof(AudioSource), typeof(Tracker))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                if (Application.isPlaying)
                    Object.DontDestroyOnLoad(go);

                source = go.GetComponent<AudioSource>();
            }

            source.spatialBlend = (position != null || tracking != null) ? 1 : 0;

            if (tracking != null)
            {
                var tracker = source.GetComponent<Tracker>();
                tracker.Target = tracking;
                tracker.Offset = position ?? Vector3.zero;
            }

            source.transform.position = position ?? Vector3.zero;

            if (despawnTime > 0)
                StopAfter(handle, despawnTime);

            return source;
        }

        public static void Despawn(AudioSource source)
        {
            if (source != null)
            {
                source.Stop();
                // It is necessary to set the clip to null or the AudioSource delay will not work for some fucked up reason.
                source.clip = null;
                source.gameObject.SetActive(false);
                Pool.Enqueue(source);
            }
        }

        private static void StopAfter(AudioHandle handle, float time)
        {
            // Select despawn strategy based on what packages are present
#if UNITASK
            StopAfterInternal(handle, time).Forget();
#else
#if EDITOR_COROUTINES && UNITY_EDITOR
                if (Application.isPlaying)
                    CoroutineRunner.RunCoroutine(StopAfterInternal(handle, time));
                else
                    EditorCoroutineUtility.StartCoroutineOwnerless(StopAfterInternal(handle, time));
#else
                CoroutineRunner.RunCoroutine(StopAfterInternal(handle, time));
#endif

#endif
        }

#if UNITASK
        private static async UniTask StopAfterInternal(AudioHandle handle, float time)
        {
            await UniTask.Delay((int) (time * 1000));
            handle.Stop(0);
        }
#else
        private static IEnumerator StopAfterInternal(AudioHandle handle, float time)
        {
#if EDITOR_COROUTINES && UNITY_EDITOR
            if (!Application.isPlaying)
                yield return new EditorWaitForSeconds(time);
            else
                yield return new WaitForSeconds(time);
#else
            yield return new WaitForSeconds(time);
#endif
            handle.Stop(0);
        }
#endif
    }
}