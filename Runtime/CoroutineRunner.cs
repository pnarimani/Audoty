#if !USE_UNITASK
using System.Collections;
using UnityEngine;

namespace Audoty
{
    [ExecuteAlways]
    internal class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _coroutineRunner;

        public static void RunCoroutine(IEnumerator coroutine)
        {
            if (_coroutineRunner == null)
            {
                _coroutineRunner = new GameObject("Coroutine Runner", typeof(CoroutineRunner))
                {
                    hideFlags = HideFlags.HideAndDontSave
                }.GetComponent<CoroutineRunner>();
            }

            _coroutineRunner.StartCoroutine(coroutine);
        }
    }
}
#endif