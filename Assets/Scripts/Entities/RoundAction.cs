using System;
using GameActions;

public struct RoundAction : IEquatable<RoundAction>
{
    public GameAction selectedAction;
    public ulong playerID;

    public readonly bool Equals(RoundAction other)
    {
        return selectedAction == other.selectedAction && playerID == other.playerID;
    }

    public RoundAction(GameAction selectedAction, ulong playerID)
    {
        this.selectedAction = selectedAction;
        this.playerID = playerID;
    }
}
