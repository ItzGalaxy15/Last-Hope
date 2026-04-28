using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace LastHope.Audio;

// Reference: https://docs.monogame.net/articles/tutorials/building_2d_games/15_audio_controller/index.html

public static class AudioManager
{
    private static float _masterVolume;
    public static float MasterVolume
    { 
        get
        {
            return _masterVolume;
        }
        set
        {
            _masterVolume = Math.Clamp(value, 0.0f, 1.0f);
        }
    }

    private static float _musicVolume;
    public static float MusicVolume
    { 
        get
        {
            return _musicVolume;
        }
        set
        {
            _musicVolume = Math.Clamp(value, 0.0f, 1.0f);
        }
    }

    private static float _sfxVolume;
    public static float SfxVolume
    { 
        get
        {
            return _sfxVolume;
        }
        set
        {
            _sfxVolume = Math.Clamp(value, 0.0f, 1.0f);
        }
    }

    public static void Apply()
    {
        SoundEffect.MasterVolume = MasterVolume;
        MediaPlayer.Volume = MusicVolume * MasterVolume;
    }

    public static void PlaySfx(SoundEffect sound)
    {
        sound.Play(SfxVolume, 0f, 0f);
    }

    public static void PlayMusic(Song song, bool isRepeating = true)
    {
        MediaPlayer.IsRepeating = isRepeating;
        MediaPlayer.Play(song);
    }
}