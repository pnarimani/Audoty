using System;

namespace Audoty
{
    public class ClipNullException : Exception
    {

        public ClipNullException(AudioPlayer audioPlayer, int clipIndex) : base($"Audio Player `{audioPlayer.name}` has a null clip at `{clipIndex}`")
        {
            ClipIndex = clipIndex;
            AudioPlayer = audioPlayer;
        }
        
        public AudioPlayer AudioPlayer { get; }
        public int ClipIndex { get; }
    }
}