using BepInEx;
using R2API.Utils;
using UnityEngine.SceneManagement;
using System;
using BepInEx.Configuration;
using On.RoR2.UI;
using PluviaeCanticum.Songs;
using RiskOfOptions;
using RiskOfOptions.Options;

namespace PluviaeCanticum;

// no idea tf this for
[BepInDependency("com.bepis.r2api")]
// this below fine tho
[BepInDependency("com.rune580.riskofoptions")]
[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
public class PluviaeCanticumPlugin : BaseUnityPlugin 
{
    #region Plugin Metadata
    private const string PluginGUID = PluginAuthor + "." + PluginName;
    private const string PluginAuthor = "InkyaRev";
    private const string PluginName = "PluviaeCanticum";
    private const string PluginVersion = "1.0.0";
    #endregion

    // ReSharper disable MemberCanBePrivate.Global
    public static ConfigEntry<float> MusicVolume { get; set; }
    public static ConfigEntry<bool> AffectedByMasterVolume { get; set; }
    public static ConfigEntry<bool> ShouldLoop { get; set; }

    // ReSharper restore MemberCanBePrivate.Global

    public static TeleporterState CurrentTeleporterState { get; private set; }
    public static Boss CurrentBoss { get; set; } = Boss.None;
    private string _lastMusicVolume = string.Empty;
    private AudioProcessingHandler _audioProcessingHandler;

    public void Awake() 
    {
        Log.Init(Logger);

        #region Config stuff
        MusicVolume = Config.Bind("Settings", "Music Volume", 1f);
        ModSettingsManager.AddOption(new SliderOption(MusicVolume));
        
        AffectedByMasterVolume = Config.Bind("Settings", "Affected By Master Volume", true, "Whether the music volume is multiplied by the in-game master volume.");
        ModSettingsManager.AddOption(new CheckBoxOption(AffectedByMasterVolume));
        
        ShouldLoop = Config.Bind("Settings", "Should Loop", true, "Whether songs should loop. If false, another song is selected from scene matches. \nWARNING: Updates only when a new song is selected.");
        ModSettingsManager.AddOption(new CheckBoxOption(ShouldLoop));
        #endregion

        _audioProcessingHandler = new AudioProcessingHandler();
        _audioProcessingHandler.Start();

        #region Pausing
        PauseScreenController.OnEnable += (orig, self) => 
        {
            orig(self);
            _audioProcessingHandler.PausedEventSignal.Set();
        };

        PauseScreenController.OnDisable += (orig, self) =>
        {
            orig(self);
            _audioProcessingHandler.UnPausedEventSignal.Set();
        };
        #endregion
        
        #region Teleporter interaction
        On.RoR2.TeleporterInteraction.OnInteractionBegin += (orig, self, activator) => 
        {
            orig(self, activator);
            CurrentTeleporterState = TeleporterState.Charging;
            _audioProcessingHandler.PickSongRequestSignal.Set();
        };
        On.RoR2.TeleporterInteraction.AttemptToSpawnAllEligiblePortals += (orig, self) =>
        {
            orig(self);
            CurrentTeleporterState = TeleporterState.FinishedCharging;
            _audioProcessingHandler.PickSongRequestSignal.Set();
        };
        On.RoR2.TeleporterInteraction.FixedUpdate += (orig, self) =>
        {
            orig(self);
            if(self.isCharged || self.isIdle) return;
            _audioProcessingHandler.ChargingStateQueue.Enqueue(self.holdoutZoneController.isAnyoneCharging);
            _audioProcessingHandler.TeleporterChargingEventSignal.Set();
        };
        #endregion

        #region Mithrix
        On.EntityStates.Missions.BrotherEncounter.PreEncounter.OnEnter += (orig, self) =>
        {
            orig(self);
            _audioProcessingHandler.MithrixPreEncounterEventSignal.Set();
        };
        On.EntityStates.Missions.BrotherEncounter.Phase1.OnEnter += (orig, self) =>
        {
            orig(self);
            CurrentBoss = Boss.Mithrix;
            CurrentBoss.Phase = 1;
            _audioProcessingHandler.PickSongRequestSignal.Set();
        };
        On.EntityStates.Missions.BrotherEncounter.Phase2.OnEnter += (orig, self) =>
        {
            orig(self);
            CurrentBoss = Boss.Mithrix;
            CurrentBoss.Phase = 2;
            _audioProcessingHandler.PickSongRequestSignal.Set();
        };
        On.EntityStates.Missions.BrotherEncounter.Phase3.OnEnter += (orig, self) =>
        {
            orig(self);
            CurrentBoss = Boss.Mithrix;
            CurrentBoss.Phase = 3;
            _audioProcessingHandler.PickSongRequestSignal.Set();
        };
        On.EntityStates.Missions.BrotherEncounter.Phase4.OnEnter += (orig, self) =>
        {
            orig(self);
            CurrentBoss = Boss.Mithrix;
            CurrentBoss.Phase = 4;
            _audioProcessingHandler.PickSongRequestSignal.Set();
        };
        On.EntityStates.Missions.BrotherEncounter.BossDeath.OnEnter += (orig, self) =>
        {
            orig(self);
            CurrentBoss = Boss.Mithrix;
            CurrentBoss.Phase = -1;
            _audioProcessingHandler.PickSongRequestSignal.Set();
        };
        #endregion

        #region FalseSon
        On.RoR2.MeridianEventTriggerInteraction.Phase1.OnEnter += (orig, self) =>
        {
            orig(self);
            CurrentBoss = Boss.FalseSon;
            CurrentBoss.Phase = 1;
            _audioProcessingHandler.PickSongRequestSignal.Set();
        };
        On.EntityStates.FalseSonBoss.CrystalDeathState.PlayDeathAnimation += (orig, self, duration) =>
        {
            orig(self, duration);
            CurrentBoss = Boss.FalseSon;
            CurrentBoss.Phase = 2;
            _audioProcessingHandler.PickSongRequestSignal.Set();
        };
        On.EntityStates.FalseSonBoss.BrokenCrystalDeathState.PlayDeathAnimation += (orig, self, duration) =>
        {
            orig(self, duration);
            CurrentBoss = Boss.FalseSon;
            CurrentBoss.Phase = 3;
            _audioProcessingHandler.PickSongRequestSignal.Set();
        };
        On.RoR2.MeridianEventTriggerInteraction.RpcOnFalseSonDefeated += (orig, self) =>
        {
            orig(self);
            CurrentBoss = Boss.FalseSon;
            CurrentBoss.Phase = -1;
            _audioProcessingHandler.PickSongRequestSignal.Set();
        };
        #endregion

        #region Voidling
        VoidlingPhaseCounter.OnPhaseChanged += phase =>
        {
            CurrentBoss = Boss.Voidling;
            CurrentBoss.Phase = phase;
            _audioProcessingHandler.PickSongRequestSignal.Set();
        };
        #endregion

        #region Scene changed
        SceneManager.activeSceneChanged += (_, _) =>
        {
            CurrentTeleporterState = TeleporterState.None;
            CurrentBoss = Boss.None;
            _audioProcessingHandler.PickSongRequestSignal.Set();
        };
        #endregion
    }
    
    private void FixedUpdate()
    {
        if (RoR2.Console.instance is null) return;
        
        var masterVolume = RoR2.Console.instance.FindConVar("volume_master");
        if (masterVolume is not null)
        {
            _audioProcessingHandler.MasterVolume = Convert.ToSingle(masterVolume.GetString()) / 100f;
        }

        if (_lastMusicVolume == string.Empty)
        {
            var musicVolumeConVar = RoR2.Console.instance.FindConVar("volume_music");
            if (musicVolumeConVar is not null)
            {
                _lastMusicVolume = musicVolumeConVar.GetString();
                musicVolumeConVar.SetString("0");
            }
        }
    }

    private void OnDestroy() 
    {
        var conVar = RoR2.Console.instance.FindConVar("volume_music");
        conVar?.SetString(_lastMusicVolume);
        _audioProcessingHandler.Stop();
    }
}



