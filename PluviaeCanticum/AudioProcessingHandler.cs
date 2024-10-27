using System;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PluviaeCanticum.Tracks;
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
    public AutoResetEvent PickTrackRequestSignal { get; } = new(false);
    
    public AutoResetEvent PausedEventSignal { get; } = new(false);
    public AutoResetEvent UnPausedEventSignal { get; } = new(false);
    
    public AutoResetEvent TeleporterChargingEventSignal { get; } = new(false);
    
    public AutoResetEvent MithrixPreEncounterEventSignal { get; } = new(false);
    #endregion
    
    private float _masterVolume;
    private readonly Settings _settings;
    private WaveOutEvent _outputDevice;
    private readonly Random _random = new();
    private Track _currentTrack = Track.None;
    private FadeInOutSampleProvider _crushedFadeProvider;
    private FadeInOutSampleProvider _fadeProvider;
    private AudioFileReader _audioFileReader;
    private MixingSampleProvider _mixingSampleProvider;

    private float MusicVolume { get; set; }
    private bool AffectedByMasterVolume { get; set; }
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
        var tracksPath = Path.Combine(pluginPath, "Tracks");
        var settingsPath = Path.Combine(pluginPath, "settings.toml");
        
        try
        {
            var tomlString = File.ReadAllText(settingsPath);
            _settings = TomletMain.To<Settings>(tomlString);
            _settings.Tracks = _settings.SceneTracks.Concat<Track>(_settings.TeleporterTracks).Concat(_settings.BossTracks).ToArray();
            
            if (_settings.CustomTracksPath != string.Empty)
            {
                tracksPath = _settings.CustomTracksPath;
            }

            var audioFiles = Utils.SearchForAudioFiles(tracksPath);
            
            foreach (var track in _settings.Tracks)
            {
                if (!track.ExistsWithin(audioFiles))
                {
                    Log.Error($"Found no file for track {track.Name}. This track will not be played.");
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
            
            if (PickTrackRequestSignal.WaitOne(0))
            {
                PickTrack();
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
                    _mixingSampleProvider.RemoveAllMixerInputs();
                    _mixingSampleProvider.AddMixerInput(isChargingTeleporter ? _fadeProvider : _crushedFadeProvider);
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
            
            if (_audioFileReader.Position >= _audioFileReader.Length && !_currentTrack.ShouldLoop) 
            {
                _outputDevice.Stop();
                _audioFileReader.Position = _audioFileReader.Length - 1;
                PickTrack();
            }
        }
    }
    
    
    private void PickTrack()
    {
        var trackChoices = _settings.Tracks.Where(track => track.MatchesConditions()).ToArray();
        
        Track chosenTrack;
        if (trackChoices.Length > 0)
        {
            var trackChoicesNames = trackChoices
                .Aggregate(string.Empty, (str, track) => str + ('\n' + track.Name));
            Log.Message($"""
                             Selecting track;
                             {trackChoices[0].GetSelectingTrackString()};
                             Choices: {trackChoicesNames}.
                             """);
            chosenTrack = trackChoices[_random.Next(trackChoices.Length)];
        }
        else
        {
            Log.Error("Track pick failed. No track will be played.");
            if (_outputDevice.PlaybackState is PlaybackState.Playing)
            {
                _crushedFadeProvider.BeginFadeOut(_currentTrack.FadeOutMS);
                _fadeProvider.BeginFadeOut(_currentTrack.FadeOutMS);
                Thread.Sleep(_currentTrack.FadeOutMS);
            }
            _outputDevice.Dispose();
            _currentTrack = Track.None;
            return;
        }
        
        PlayTrack(chosenTrack);
    }
    
    private void PlayTrack(Track track)
    {
        if (track.Name != _currentTrack.Name)
        {
            _audioFileReader = new AudioFileReader(track.FilePath);

            var shouldLoop = track.ShouldLoop;
            if (PluviaeCanticumPlugin.CurrentTeleporterState is TeleporterState.FinishedCharging)
            {
                shouldLoop = false;
            }

            var loopStream = new LoopStream(_audioFileReader, shouldLoop);

            var highPassFilter = BiQuadFilter.HighPassFilter(loopStream.WaveFormat.SampleRate, 500, 0.5f);
            
            var crushed = new FilterSampleProvider(new WaveToSampleProvider(loopStream), highPassFilter);
            var volumeBoosted = new VolumeSampleProvider(crushed) { Volume = 1.5f }; // compensate for filter
            
            if (_outputDevice.PlaybackState is PlaybackState.Playing)
            {
                _crushedFadeProvider.BeginFadeOut(_currentTrack.FadeOutMS);
                _fadeProvider.BeginFadeOut(_currentTrack.FadeOutMS);
                Thread.Sleep(_currentTrack.FadeOutMS);
                _outputDevice.Dispose();
                Thread.Sleep(track.SilenceMS);
            }
            else
            {
                _outputDevice.Dispose();
            }
            
            if (AffectedByMasterVolume)
            {
                _audioFileReader.Volume = MasterVolume * MusicVolume * track.Volume;
            }
            else
            {
                _audioFileReader.Volume = MusicVolume * track.Volume;
            }
            
            _currentTrack = track;
            
            _crushedFadeProvider = new FadeInOutSampleProvider(volumeBoosted);
            _fadeProvider = new FadeInOutSampleProvider(new WaveToSampleProvider(loopStream), true);
            _mixingSampleProvider = new MixingSampleProvider([_fadeProvider]);
            
            _outputDevice = new WaveOutEvent();
            _outputDevice.Init(_mixingSampleProvider);
            
            _outputDevice.Play();
            _fadeProvider.BeginFadeIn(track.FadeInMS);

            Log.Message($"Now Playing: {track.Name}.");
        }
        else
        {
            Log.Message($"Already playing: {track.Name}.");
        }
    }
    
    private void UpdateVolume()
    {
        if(_audioFileReader is null) return;
        
        if (AffectedByMasterVolume)
        {
            _audioFileReader.Volume = MasterVolume * MusicVolume * _currentTrack.Volume;
        }
        else
        {
            _audioFileReader.Volume = MusicVolume * _currentTrack.Volume;
        }
    }
}
