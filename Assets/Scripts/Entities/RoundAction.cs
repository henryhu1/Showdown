using System;
using GameActions;

public struct RoundAction : IEquatable<RoundAction>
{
    public ActionType selectedAction;
    public ulong playerID;

    public readonly bool Equals(RoundAction other)
    {
        return selectedAction == other.selectedAction && playerID == other.playerID;
    }

    public RoundAction(ActionType selectedAction, ulong playerID)
    {
        this.selectedAction = selectedAction;
        this.playerID = playerID;
    }
}
