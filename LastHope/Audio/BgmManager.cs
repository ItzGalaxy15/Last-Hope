using System;
using Last_Hope;
using Last_Hope.Engine;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace LastHope.Audio;

/// <summary>
/// Looped background music via <see cref="SoundEffectInstance"/> (BGM assets are SoundEffects in the content pipeline).
/// </summary>
public static class BgmManager
{
    private enum BgmContext
    {
        None,
        MainMenu,
        Overworld,
    }

    private static readonly string[] MainMenuTrackLabels = { "Snyth-1", "Flute", "Synth-2" };
    private static readonly string[] MainMenuTrackPaths =
    {
        "sounds/bgm-piano-1",
        "sounds/bgm-flute-1",
        "sounds/bgm-synth-2",
    };

    private const string OverworldTrackPath = "sounds/bgm-synth-1";

    private static SoundEffect _overworldTrack;
    private static SoundEffect[] _mainMenuTracks = Array.Empty<SoundEffect>();
    private static SoundEffectInstance _instance;
    private static BgmContext _activeContext = BgmContext.None;
    private static int _mainMenuTrackIndex;

    public static float MainMenuBgmVolume { get; set; } = 0.3f;
    public static float OverworldBgmVolume { get; set; } = 0.3f;

    public static string CurrentMainMenuTrackLabel =>
        MainMenuTrackLabels[Math.Clamp(_mainMenuTrackIndex, 0, MainMenuTrackLabels.Length - 1)];

    public static void Load(ContentManager content)
    {
        _mainMenuTracks = new SoundEffect[MainMenuTrackPaths.Length];
        for (int i = 0; i < MainMenuTrackPaths.Length; i++)
            _mainMenuTracks[i] = content.Load<SoundEffect>(MainMenuTrackPaths[i]);

        _overworldTrack = content.Load<SoundEffect>(OverworldTrackPath);
    }

    public static void OnGameStateChanged(GameState state)
    {
        BgmContext wanted = ResolveContext(state);

        if (wanted == _activeContext)
            return;

        _activeContext = wanted;

        switch (wanted)
        {
            case BgmContext.MainMenu:
                PlayMainMenu();
                break;
            case BgmContext.Overworld:
                PlayOverworld();
                break;
            default:
                Stop();
                break;
        }
    }

    private static BgmContext ResolveContext(GameState state)
    {
        if (state is GameState.Running or GameState.Paused)
            return BgmContext.Overworld;

        if (state is GameState.MainMenu or GameState.Characters or GameState.CharacterSelect or GameState.ItemsIndex)
            return BgmContext.MainMenu;

        if (state == GameState.SettingsMenu)
        {
            GameState returnState = GameManager.GetGameManager().StateAfterClosingSettings;
            if (returnState is GameState.Running or GameState.Paused)
                return BgmContext.Overworld;
            return BgmContext.MainMenu;
        }

        return BgmContext.None;
    }

    public static void CycleMainMenuTrack()
    {
        if (_mainMenuTracks.Length == 0)
            return;

        _mainMenuTrackIndex = (_mainMenuTrackIndex + 1) % _mainMenuTracks.Length;
        if (_activeContext == BgmContext.MainMenu)
            PlaySound(_mainMenuTracks[_mainMenuTrackIndex]);
    }

    public static void PlayMainMenu()
    {
        if (_mainMenuTracks.Length == 0)
            return;

        _activeContext = BgmContext.MainMenu;
        PlaySound(_mainMenuTracks[_mainMenuTrackIndex]);
    }

    public static void PlayOverworld()
    {
        if (_overworldTrack == null)
            return;

        _activeContext = BgmContext.Overworld;
        PlaySound(_overworldTrack);
    }

    public static void Stop()
    {
        _activeContext = BgmContext.None;
        _instance?.Stop();
        _instance?.Dispose();
        _instance = null;
    }

    public static void RefreshVolume()
    {
        if (_instance == null)
            return;

        _instance.Volume = GetEffectiveVolume(_activeContext);
    }

    private static void PlaySound(SoundEffect sound)
    {
        if (sound == null)
            return;

        _instance?.Stop();
        _instance?.Dispose();

        _instance = sound.CreateInstance();
        _instance.IsLooped = true;
        _instance.Volume = GetEffectiveVolume(_activeContext);
        _instance.Play();
    }

    private static float GetEffectiveVolume(BgmContext context)
    {
        float contextVolume = context switch
        {
            BgmContext.MainMenu => MainMenuBgmVolume,
            BgmContext.Overworld => OverworldBgmVolume,
            _ => 0f,
        };

        return Math.Clamp(AudioManager.MasterVolume * AudioManager.MusicVolume * contextVolume, 0f, 1f);
    }
}
