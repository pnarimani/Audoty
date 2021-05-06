using System;

namespace Audoty
{
    public class NoClipsFoundException : Exception
    {
        public NoClipsFoundException(AudioPlayer audioPlayer) : base($"No clips found in Audio Player {audioPlayer.name}.")
        {
            AudioPlayer = audioPlayer;
        }
        
        public AudioPlayer AudioPlayer { get; }
    }
}