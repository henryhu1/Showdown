using System;
using Unity.Netcode;

public struct RoundAction : IEquatable<RoundAction>, INetworkSerializable
{
    public ulong playerID;
    public GameAction selectedAction;

    public readonly bool Equals(RoundAction other)
    {
        return selectedAction == other.selectedAction && playerID == other.playerID;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref playerID);
        serializer.SerializeValue(ref selectedAction);
    }

    public RoundAction(ulong playerID, GameAction selectedAction)
    {
        this.playerID = playerID;
        this.selectedAction = selectedAction;
    }
}
