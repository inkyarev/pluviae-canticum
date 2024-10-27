using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PluviaeCanticum.Songs;
using Tomlet;
using System.Collections.Concurrent;
using System.Threading;

namespace PluviaeCanticum;

public class AudioProcessingHandler
{
    private Thread _audioThread;
    public readonly ConcurrentQueue<bool> ChargingStateQueue = new();
    private bool _isRunning;

    #region Signals
    public AutoResetEvent PickSongRequestSignal { get; } = new(false);
    
    public AutoResetEvent PausedEventSignal { get; } = new(false);
    public AutoResetEvent UnPausedEventSignal { get; } = new(false);
    
    public AutoResetEvent TeleporterChargingEventSignal { get; } = new(false);
    
    public AutoResetEvent MithrixPreEncounterEventSignal { get; } = new(false);
    #endregion
    
    private float _masterVolume;
    private readonly Settings _settings;
    private WaveOutEvent _outputDevice;
    private readonly Random _random = new();
    private Song _currentSong = new SceneSong();
    private FadeInOutSampleProvider _crushedFadeProvider;
    private FadeInOutSampleProvider _fadeProvider;
    private AudioFileReader _audioFileReader;
    private MixingSampleProvider _mixingSampleProvider;

    private float MusicVolume { get; set; }
    private bool AffectedByMasterVolume { get; set; }
    private  bool ShouldLoop { get; set; }
    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = value;
            UpdateVolume();
        }
    }

    public AudioProcessingHandler()
    {
        #region Binding config values from parent plugin
        MusicVolume = PluviaeCanticumPlugin.MusicVolume.Value / 100f;
        PluviaeCanticumPlugin.MusicVolume.SettingChanged += (sender, _) =>
        {
            MusicVolume = ((ConfigEntry<float>)sender).Value / 100f;
            UpdateVolume();
        };
        AffectedByMasterVolume = PluviaeCanticumPlugin.AffectedByMasterVolume.Value;
        PluviaeCanticumPlugin.AffectedByMasterVolume.SettingChanged += (sender, _) =>
        {
            AffectedByMasterVolume = ((ConfigEntry<bool>)sender).Value;
            UpdateVolume();
        };
        ShouldLoop = PluviaeCanticumPlugin.ShouldLoop.Value;
        PluviaeCanticumPlugin.ShouldLoop.SettingChanged += (sender, _) =>
        {
            ShouldLoop = ((ConfigEntry<bool>)sender).Value;
        };
        #endregion

        #region Retrieve master volume
        var gameConfigFilePath = Path.Combine(Paths.GameRootPath, Paths.ProcessName + "_Data", "Config", "config.cfg");
        var lines = File.ReadAllLines(gameConfigFilePath);
        var masterVolumeLine = lines.First(line => line.StartsWith("volume_master "));
        var masterVolumeString = masterVolumeLine[14..].TrimEnd(';');
        MasterVolume = Convert.ToSingle(masterVolumeString) / 100f;
        #endregion
        
        #region Parse settings
        var pluginPath = Path.Combine(Paths.PluginPath, "PluviaeCanticum");
        var songsPath = Path.Combine(pluginPath, "Songs");
        var settingsPath = Path.Combine(pluginPath, "settings.toml");
        
        try
        {
            var tomlString = File.ReadAllText(settingsPath);
            _settings = TomletMain.To<Settings>(tomlString);
            _settings.Songs = _settings.SceneSongs.Concat<Song>(_settings.TeleporterSongs).Concat(_settings.BossSongs).ToArray();
            
            if (_settings.CustomSongsPath != string.Empty)
            {
                songsPath = _settings.CustomSongsPath;
            }

            var audioFiles = Utils.SearchForAudioFiles(songsPath);
            
            foreach (var song in _settings.Songs)
            {
                if (!song.ExistsWithin(audioFiles))
                {
                    Log.Error($"Found no file for song {song.Name}. This song will not be played.");
                }
            }
        } 
        catch (Exception e)
        {
            Log.Fatal($"Failed to parse settings.toml. Exception: {e}.");
            throw;
        }
        #endregion
        
        _outputDevice = new WaveOutEvent();
    }

    public void Start()
    {
        _isRunning = true;
        _audioThread = new Thread(RunAudioLogic);
        _audioThread.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        PausedEventSignal.Set(); // To make sure the thread exits
        _audioThread.Join();
    }

    private void RunAudioLogic()
    {
        while (_isRunning)
        {
            if (_outputDevice is null) continue;
            
            if (PickSongRequestSignal.WaitOne(0))
            {
                PickSong();
                if (PluviaeCanticumPlugin.CurrentBoss.Phase == -1)
                {
                    PluviaeCanticumPlugin.CurrentBoss = Boss.None;
                }
            }

            #region Pausing
            if (PausedEventSignal.WaitOne(0))
            {
                if (_outputDevice.PlaybackState is PlaybackState.Playing)
                {
                    _fadeProvider.BeginFadeOut(500);
                    Thread.Sleep(500);
                    _outputDevice.Pause();
                }
            }

            if (UnPausedEventSignal.WaitOne(0))
            {
                if (_outputDevice.PlaybackState is PlaybackState.Paused) 
                {
                    _outputDevice.Play();
                    _fadeProvider.BeginFadeIn(250);
                }
            }
            #endregion

            #region Teleporter

            if (TeleporterChargingEventSignal.WaitOne(0))
            {
                while (ChargingStateQueue.TryDequeue(out var isChargingTeleporter))
                {
                    if (isChargingTeleporter)
                    {
                        _mixingSampleProvider.RemoveAllMixerInputs();
                        _mixingSampleProvider.AddMixerInput(_fadeProvider);
                    }
                    else
                    {
                        _mixingSampleProvider.RemoveAllMixerInputs();
                        _mixingSampleProvider.AddMixerInput(_crushedFadeProvider);
                    }
                }
            }
            #endregion

            #region Mithrix
            if (MithrixPreEncounterEventSignal.WaitOne(0))
            {
                _fadeProvider.BeginFadeOut(1000);
            }
            #endregion
            
            if(_audioFileReader is null) continue;
            
            if (_audioFileReader.Position >= _audioFileReader.Length && !ShouldLoop) 
            {
                _outputDevice.Stop();
                _audioFileReader.Position = _audioFileReader.Length - 1;
                if (_currentSong is SceneSong sceneSong && sceneSong.ScenesPlayedAt.Any(scene => scene is "outro" or "loadingbasic"))
                {
                    continue;
                }
                PickSong();
            }
        }
    }
    
    
    private void PickSong()
    {
        var songChoices = _settings.Songs.Where(song => song.MatchesConditions()).ToArray();
        
        Song chosenSong;
        if (songChoices.Length > 0)
        {
            var songChoicesNames = songChoices
                .Aggregate(string.Empty, (str, song) => str + ('\n' + song.Name));
            Log.Message($"""
                             Selecting song;
                             {songChoices[0].GetSelectingSongString()};
                             Choices: {songChoicesNames}.
                             """);
            chosenSong = songChoices[_random.Next(songChoices.Length)];
        }
        else
        {
            Log.Error("Song pick failed. No song will be played.");
            return;
        }
        
        PlaySong(chosenSong);
    }
    
    private void PlaySong(Song song)
    {
        if (song.Name != _currentSong.Name)
        {
            _currentSong = song;

            _audioFileReader = new AudioFileReader(song.FilePath);

            if (AffectedByMasterVolume)
            {
                _audioFileReader.Volume = MasterVolume * MusicVolume * song.Volume;
            }
            else
            {
                _audioFileReader.Volume = MusicVolume * song.Volume;
            }

            var shouldLoop = ShouldLoop;
            if (PluviaeCanticumPlugin.CurrentBoss.Phase == -1 || PluviaeCanticumPlugin.CurrentTeleporterState is TeleporterState.FinishedCharging)
            {
                shouldLoop = false;
            }

            var loopStream = new LoopStream(_audioFileReader, shouldLoop);

            var highPassFilter = BiQuadFilter.HighPassFilter(loopStream.WaveFormat.SampleRate, 500, 0.5f);
            
            var crushed = new FilterSampleProvider(_audioFileReader, highPassFilter);
            var volumeBoosted = new VolumeSampleProvider(crushed) { Volume = 1.5f }; // compensate for filter
            _crushedFadeProvider = new FadeInOutSampleProvider(volumeBoosted);
            
            if (_outputDevice.PlaybackState is PlaybackState.Playing)
            {
                _fadeProvider.BeginFadeOut(song.FadeOutPreviousMS);
                Thread.Sleep(song.FadeOutPreviousMS);
                _outputDevice.Stop();
                _outputDevice.Dispose();
                Thread.Sleep(song.SilenceMS);
            }
            
            _fadeProvider = new FadeInOutSampleProvider(new WaveToSampleProvider(loopStream), true);
            _mixingSampleProvider = new MixingSampleProvider(new[] { _fadeProvider });
            
            _outputDevice = new WaveOutEvent();
            _outputDevice.Init(_mixingSampleProvider);
            
            _outputDevice.Play();
            _fadeProvider.BeginFadeIn(song.FadeInMS);

            Log.Message($"Now Playing: {song.Name}.");
        }
        else
        {
            Log.Message($"Already playing: {song.Name}.");
        }
    }
    
    private void UpdateVolume()
    {
        if(_audioFileReader is null) return;
        
        if (AffectedByMasterVolume)
        {
            _audioFileReader.Volume = MasterVolume * MusicVolume * _currentSong.Volume;
        }
        else
        {
            _audioFileReader.Volume = MusicVolume * _currentSong.Volume;
        }
    }
}
