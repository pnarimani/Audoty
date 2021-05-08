using System;
using UnityEngine;

namespace Audoty
{
    internal class Tracker : MonoBehaviour
    {
        public Transform Target;
        public Vector3 Offset;

        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (_source.isPlaying && Target != null)
                transform.position = Target.position + Offset;
        }

        private void OnDisable()
        {
            Target = null;
        }
    }
}