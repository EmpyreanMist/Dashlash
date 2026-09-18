namespace Phasebreak.Gameplay
{
    public interface ICombatCastSource
    {
        bool IsCasting { get; }
        string CastName { get; }
        float CastProgress { get; }
        bool IsCastInterruptible { get; }
    }
}
