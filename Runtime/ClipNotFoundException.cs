using System;

namespace Audoty
{
    public class ClipNotFoundException : Exception
    {
        public ClipNotFoundException(AudioPlayer audioPlayer, string clipName) : base($"Clip with name `{clipName}` was not found in Audio Player `{audioPlayer.name}`.")
        {
            AudioPlayer = audioPlayer;
            ClipName = clipName;
        }
        
        public AudioPlayer AudioPlayer { get; }
        
        public string ClipName { get; }
    }
}