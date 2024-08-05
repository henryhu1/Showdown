using System;
using GameActions;

public struct RoundResult : IEquatable<RoundResult>
{
    RoundAction winningAction;
    RoundAction losingAction;

    public readonly bool Equals(RoundResult other)
    {
        return winningAction.Equals(other.winningAction) && losingAction.Equals(other.losingAction);
    }
}
