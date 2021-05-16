using UnityEngine;

#if UNITASK
using System;
using Cysharp.Threading.Tasks;
#endif

namespace Audoty
{
    public readonly struct AudioHandle
    {
        private readonly AudioPlayer _player;
        private readonly int _id;
        private readonly float _completionTime;

        public AudioHandle(AudioPlayer player, int id, int clipIndex)
        {
            ClipIndex = clipIndex;
            _id = id;
            _player = player;
            _completionTime = Time.time + _player.Clips[clipIndex].length;
        }
        
        public int ClipIndex { get; }
        public float ClipLength => _player == null ? 0 : _player.Clips[ClipIndex].length;

        /// <summary>
        /// Returns true if the audio is currently playing
        /// </summary>
        /// <returns></returns>
        public bool IsPlaying()
        {
            if (_player == null)
                return false;

            if (_player._playingSources.TryGetValue(_id, out AudioSource source))
            {
                if (source == null)
                {
                    _player.Stop(_id, 0);
                    return false;
                }

                return source.isPlaying;
            }

            return false;
        }

        /// <summary>
        /// Stops audio player. If audio player is playing, audio will be faded out with InterruptFadeTime in AudioPlayer
        /// </summary>
        /// <returns>true if clip stops, false if clip was already stopped</returns>
        public bool Stop()
        {
            if (_player == null)
                return false;

            return _player.Stop(_id, _player.InterruptFadeTime);
        }

        /// <summary>
        /// Stops audio player. If audio player is playing, audio will be faded out using the given parameter
        /// </summary>
        /// <returns>true if clip stops, false if clip was already stopped</returns>
        public bool Stop(float fadeOutTime)
        {
            if (_player == null)
                return false;

            return _player.Stop(_id, fadeOutTime);
        }

#if UNITASK
        public async UniTask WaitUntilCompletion()
        {
            if (_player == null)
                return;
            
            if (_completionTime <= Time.time)
                return;

            DelayType type = Application.isPlaying ? DelayType.DeltaTime : DelayType.Realtime;
            
            await UniTask.Delay(TimeSpan.FromSeconds(_completionTime - Time.time), type);
        }
#endif
    }
}