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
    internal static class Fade
    {
        public static void In(AudioSource source, float volume, float fadeTime)
        {
#if USE_UNITASK
            InInternal(source, volume, fadeTime).Forget();
#else
#if USE_EDITOR_COROUTINES && UNITY_EDITOR
            if (Application.isPlaying)
                CoroutineRunner.RunCoroutine(InInternal(source, volume, fadeTime));
            else
                EditorCoroutineUtility.StartCoroutineOwnerless(InInternal(source, volume, fadeTime));
#else
            CoroutineRunner.RunCoroutine(InInternal(source, volume, fadeTime));
#endif
#endif
        }

        public static void Out(AudioSource source, float fadeTime)
        {
#if USE_UNITASK
            OutInternal(source, fadeTime).Forget();
#elif USE_EDITOR_COROUTINES && UNITY_EDITOR
            if (!Application.isPlaying)
                EditorCoroutineUtility.StartCoroutineOwnerless(OutInternal(source, fadeTime));
            else
                CoroutineRunner.RunCoroutine(OutInternal(source, fadeTime));
#else
                    CoroutineRunner.RunCoroutine(OutInternal(source, fadeTime));
#endif
        }

        private static
#if USE_UNITASK
            async UniTask
#else
            IEnumerator
#endif
            InInternal(AudioSource source, float volume, float fadeTime)
        {
            if (fadeTime > 0)
            {
                float startTime = Time.time;

                source.volume = 0;
                float stepPerSecond = volume / fadeTime;

                while (source != null && source.volume < volume && Time.time - startTime < fadeTime)
                {
                    source.volume += stepPerSecond * Time.deltaTime;
#if USE_UNITASK
                    // Only Yield works here because in Editor mode we don't have frames
                    await UniTask.Yield();
#else
                    yield return null;
#endif
                }
            }

            if (source != null)
                source.volume = volume;
        }

        private static
#if USE_UNITASK
            async UniTask
#else
            IEnumerator
#endif
            OutInternal(AudioSource source, float fadeTime)
        {
            if (fadeTime > 0)
            {
                float startTime = Time.time;

                float stepPerSecond = source.volume / fadeTime;

                while (source != null && source.volume >= 0.05f && Time.time - startTime < fadeTime)
                {
                    source.volume -= stepPerSecond * Time.deltaTime;

#if USE_UNITASK
                    // Only Yield works here because in Editor mode we don't have frames
                    await UniTask.Yield();
#else
                    yield return null;
#endif
                }
            }

            if (source != null)
            {
                source.Stop();
                AudioPool.Pool.Enqueue(source);
            }
        }
    }
}