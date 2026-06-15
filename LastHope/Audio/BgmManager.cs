using System;
using Last_Hope;
using Last_Hope.Engine;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace LastHope.Audio;

/// <summary>
/// Looped main-menu background music via <see cref="SoundEffectInstance"/>.
/// </summary>
public static class BgmManager
{
    private static readonly string[] MainMenuTrackLabels = { "Snyth-1", "Flute", "Synth-2" };
    private static readonly string[] MainMenuTrackPaths =
    {
        "sounds/bgm-piano-1",
        "sounds/bgm-flute-1",
        "sounds/bgm-synth-2",
    };

    private static SoundEffect[] _mainMenuTracks = Array.Empty<SoundEffect>();
    private static SoundEffectInstance _instance;
    private static bool _isMainMenuActive;
    private static int _mainMenuTrackIndex;

    public static float MainMenuBgmVolume { get; set; } = 0.3f;

    public static string CurrentMainMenuTrackLabel =>
        MainMenuTrackLabels[Math.Clamp(_mainMenuTrackIndex, 0, MainMenuTrackLabels.Length - 1)];

    public static void Load(ContentManager content)
    {
        _mainMenuTracks = new SoundEffect[MainMenuTrackPaths.Length];
        for (int i = 0; i < MainMenuTrackPaths.Length; i++)
            _mainMenuTracks[i] = content.Load<SoundEffect>(MainMenuTrackPaths[i]);
    }

    public static void OnGameStateChanged(GameState state)
    {
        bool wantMainMenu = IsMainMenuBgmState(state);

        if (wantMainMenu == _isMainMenuActive)
            return;

        _isMainMenuActive = wantMainMenu;

        if (wantMainMenu)
            PlayMainMenu();
        else
            Stop();
    }

    private static bool IsMainMenuBgmState(GameState state)
    {
        if (state is GameState.MainMenu or GameState.Characters or GameState.CharacterSelect or GameState.ItemsIndex)
            return true;

        if (state == GameState.SettingsMenu)
        {
            GameState returnState = GameManager.GetGameManager().StateAfterClosingSettings;
            return returnState is not (GameState.Running or GameState.Paused);
        }

        return false;
    }

    public static void CycleMainMenuTrack()
    {
        if (_mainMenuTracks.Length == 0)
            return;

        _mainMenuTrackIndex = (_mainMenuTrackIndex + 1) % _mainMenuTracks.Length;
        if (_isMainMenuActive)
            PlaySound(_mainMenuTracks[_mainMenuTrackIndex]);
    }

    public static void PlayMainMenu()
    {
        if (_mainMenuTracks.Length == 0)
            return;

        _isMainMenuActive = true;
        PlaySound(_mainMenuTracks[_mainMenuTrackIndex]);
    }

    public static void Stop()
    {
        _isMainMenuActive = false;
        _instance?.Stop();
        _instance?.Dispose();
        _instance = null;
    }

    public static void RefreshVolume()
    {
        if (_instance == null)
            return;

        _instance.Volume = GetEffectiveVolume();
    }

    private static void PlaySound(SoundEffect sound)
    {
        if (sound == null)
            return;

        _instance?.Stop();
        _instance?.Dispose();

        _instance = sound.CreateInstance();
        _instance.IsLooped = true;
        _instance.Volume = GetEffectiveVolume();
        _instance.Play();
    }

    private static float GetEffectiveVolume() =>
        Math.Clamp(AudioManager.MasterVolume * AudioManager.MusicVolume * MainMenuBgmVolume, 0f, 1f);
}
