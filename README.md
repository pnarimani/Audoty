# Audoty
Complete solution for workig with audio files in Unity.

# Table of Content


## Installation
Add `https://github.com/mnarimani/Audoty.git` as git url package in Package Manager window.

Or add this line to your `manifest.json` file:  
`"com.mnarimani.audoty": "https://github.com/mnarimani/Audoty.git"`

### Requirements
Audoty does not require any packages to function in Play Mode.

However, There are some requirements for Edit Mode.

If you want to play/test audio in Edit Mode, you need one of these packages for async handling:
* [UniTask](https://github.com/Cysharp/UniTask.git)
* [Editor Coroutines](https://docs.unity3d.com/Packages/com.unity.editorcoroutines@0.0/manual/index.html)

And one of these package for drawing inspector:
* [Odin Inspector](https://odininspector.com/)
* [Naughty Attributes](https://github.com/dbrizov/NaughtyAttributes.git)

**Audoty works best with Odin Inspector and UniTask**

## Creating Audio Player
Creating a new audio player is simple. Right click somewhere in project window, and select `Create/Audio Player` from the context menu.

A new scriptable object will be created named "Audio Player". You can use this scriptable object to configure your audio.

## Configuration
When you create a new audio player, it will look like this:
![inspector](https://github.com/mnarimani/Audoty/raw/master/Documentations~/audio-player-inspector.png)

These parameters help you to create perfect audio. 

### Clips
You can drag and drop your clips into the `Clips` list. Later, you can play them by index, by name, or randomly.

It is recommended to create more AudioPlayers instead of dragging all the clips into one AudioPlayer. 

For example, If your normal buttons and back buttons sound differently, it's recommended to create an AudioPlayer named "BackButton" and another named "NormalButton". This gives you more control on how your audio is played.

### Loop
`Loop` will loop the audio clip once you play it. `Loop` is useful when you are dealing with Background Music or Ambient sounds. 

### Singleton
When an AudioPlayer is marked as `Singleton`, only one of the clips in that AudioPlayer will play. Even if you call `Play()` multiple times.

### AllowInterrupt
`AllowInterrupt` is a parameter which is available if you have `Singleton` enabled. When `AllowInterrupt` is enabled, another call to `Play()` will interrupt previous live/playing audio and will replace it with the audio which is given in `Play()` method. Keep in mind that if you play the same clip multiple times with `AllowInterrupt`, it **DOES NOT** restart the clip.

### Volume
`Volume` of the AudioPlayer.

### MinDistance and MaxDistance
When audio is played in 3D, `MinDistance` and `MaxDistance` will be assigned to the values of `MinDistance` and `MaxDistance` in the audio source.

### Pitch
`Pitch` is a Min-Max slider which allows you to choose/randomize the pitch of the AudioClip.

### DopplerLevel
`DopplerLevel` changes how audio sounds on a moving object. 

### PlayFadeTime
How long does it take for AudioSource in seconds to reach to `Volume`. By default, `PlayFadeTime` is 0 which means AudioSource will reach to `Volume` instantly.

### InterruptFadeTime
When a `Singleton` audio player gets interrupted, previous clip will fade out in `InterruptFadeTime` seconds.

## Live Link
Live link allows you to modify playing/live audio sources. When live link is enabled for a parameter, any changes to that parameter will apply to all playing/live audio sources.   
For example, if you have a Background Music audio player, it is a good idea to keep `Live Link Volume` enabled so when player changes music volume in the settings, changes to volume apply to the playing music. 

## Save
If Save is enabled for any parameter, changes to that parameter in runtime will be saved to `PlayerPrefs` and will be restored in the next play session.   
For example, when `Save Volume` is enabled, any changes to volume while game is playing will be saved.

**Please note that saving only works in the build.**

## Editor Play Buttons
You can play and test how your audio clips will sound right in the editor **without playing the game**.
There are two buttons at the end of AudioPlayer inspector:
* Play Randomly
* Play Specific

Play Randomly will play a random clip from `Clips` list with the current configurations.  
Play Specific will play a clip by name. 

All the audio played with editor play buttons will be played in 2D.

## Utility Classes
Audoty comes with two utility classes which you can use to quickly play some audio:
* PlayAmbientAudio
* PlayAudioOnClick

### PlayAmbientAudio
![ambient](https://github.com/mnarimani/Audoty/raw/master/Documentations~/ambient.png)

`PlayAmbientAudio` is useful when you want to play a looping audio as ambient or background music. You can choose the clip by name or enable random selection.

Assigning a `Transform` to `TrackingTarget` will make the audio 3D. Otherwise the audio will play in 2D mode.

When a `Transform` is assigned to `TrackingTarget`, audio source will always have the same position as the specified transform. (AudioSource will not be a child of the `TrackingTarget`).

### PlayAudioOnClick
![click](https://github.com/mnarimani/Audoty/raw/master/Documentations~/click.png)

`PlayAudioOnClick` is useful when you have an interactable object which will play an audio when user clicks on it.  
Same as `PlayAmbientAudio`, you can choose a clip by name or enable random selection.

`PlayAudioOnClick` will always play the audio in 2D mode.

---
**Even though `PlayAudioOnClick` and `PlayAmbientAudio` ask for "Clip Name" in the inspector, internally they keep a reference to the clip using CLIP INDEX and not CLIP NAME.  
This allows you to change the clip in AudioPlayer without the need to reconfigure every component in the scene.  
But the drawback is that you CANNOT reorder the audio clips in an AudioPlayer.**

## API
If Utility classes don't suit your needs, you can always use code to play an AudioPlayer.  
There are two overloads of `Play` function. One takes a clip name and another takes clip index:

```c#
public class AudioPlayer
{
    /// <summary>
    /// Finds and plays a given clip, optionally at a position, and returns a handle which can be used to stop the clip.
    /// If clipName is not given, a random clip will be chosen.
    /// If position or tracking is provided, audio will be 3D, otherwise, audio will be played 2D 
    /// </summary>
    /// <param name="clipName">The name of clip to play</param>
    /// <param name="position">Position to play the clip at.</param>
    /// <param name="tracking">Audio player will track this transform's movement</param>
    /// <param name="delay">Delay in seconds before AudioPlayer actually plays the audio. AudioPlayers in delay are considered playing/live</param>
    /// <returns></returns>
    AudioHandle Play(string clipName = null, Vector3? position = null, Transform tracking = null, float delay = 0);
    
    /// <summary>
    /// Plays a given clip, optionally at a position, and returns a handle which can be used to stop the clip.
    /// If position or tracking is provided, audio will be 3D, otherwise, audio will be played 2D.
    /// </summary>
    /// <param name="index">The index of the clip to play</param>
    /// <param name="position">Position to play the clip at.</param>
    /// <param name="tracking">Audio player will track this transform's movement</param>
    /// <param name="delay">Delay in seconds before AudioPlayer actually plays the audio. AudioPlayers in delay are considered playing/live</param>
    /// <returns></returns>
    AudioHandle Play(int index, Vector3? position = null, Transform tracking = null, float delay = 0);
}
```

Calling `Play` method will return an instance of `AudioHandle` which you can use to check the state or stop the clip:

```c#
public readonly struct AudioHandle
{
    int ClipIndex { get; }
    float ClipLength { get; }
   
    /// <summary>
    /// Returns true if the audio is currently playing
    /// </summary>
    /// <returns></returns>
    public bool IsPlaying();
    
    /// <summary>
    /// Stops audio player. If audio player is playing, audio will be faded out with InterruptFadeTime in AudioPlayer
    /// </summary>
    /// <returns>true if clip stops, false if clip was already stopped</returns>
    public bool Stop();
    
    /// <summary>
    /// Stops audio player. If audio player is playing, audio will be faded out using the given parameter
    /// </summary>
    /// <returns>true if clip stops, false if clip was already stopped</returns>
    public bool Stop(float fadeOutTime);
    
    // Only available when UniTask is present:
    /// <summary>
    /// Waits until the audio has finished playing. This method will return instantly for looping AudioPlayer.
    /// </summary>
    public UniTask WaitUntilCompletion();
}
```

Other than `Play` method, you can change all the parameters through `AudioPlayer` object.