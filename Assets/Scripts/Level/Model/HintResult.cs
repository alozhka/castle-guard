namespace CastleGuard.Level.Model;

public enum HintStatus
{
    Unavailable,
    Forward,
    Undo,
}

public readonly struct HintResult
{
    public HintStatus Status { get; }
    public Move? Step { get; }

    private HintResult(HintStatus status, Move step)
    {
        Status = status;
        Step = step;
    }

    public static HintResult Forward(Move step) => new(HintStatus.Forward, step);

    public static HintResult Undo(Move lastMove) =>
        new(HintStatus.Undo, new Move(lastMove.To, lastMove.From, lastMove.WallIndex));
}
