using UnityEngine.SceneManagement;

namespace PluviaeCanticum;

public static class VoidlingPhaseCounter
{
    public delegate void OnPhaseChangedDelegate(int phase);
    public static event OnPhaseChangedDelegate OnPhaseChanged;

    private static int _phaseBackingField;
    private static int Phase
    {
        get => _phaseBackingField;
        set
        {
            _phaseBackingField = value;
            if(value == 0) return;
            OnPhaseChanged?.Invoke(value);
        }
    }
    static VoidlingPhaseCounter()
    {
        SceneManager.activeSceneChanged += (_, _) =>
        {
            Phase = 0;
        };
        On.EntityStates.VoidRaidCrab.SpawnState.OnEnter += (orig, self) =>
        {
            orig(self);
            if (Phase <= 0)
            {
                Phase = 1;
            }
        };
        On.EntityStates.VoidRaidCrab.EscapeDeath.OnExit += (orig, self) =>
        {
            orig(self);
            Phase++;
        };
        On.EntityStates.VoidRaidCrab.DeathState.OnEnter += (orig, self) =>
        {
            orig(self);
            Phase = -1;
        };
    }
}