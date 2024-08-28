using System;
using System.Collections.Generic;
using Unity.Netcode;

public struct RoundData : IEquatable<RoundData>, INetworkSerializable
{
    ulong winningPlayer;
    List<GameAction> winningActions;

    ulong losingPlayer;
    List<GameAction> losingActions;

    public readonly bool Equals(RoundData other)
    {
        if (!winningPlayer.Equals(other.winningPlayer)) return false;
        if (winningActions.Count != other.winningActions.Count) return false;
        if (losingActions.Count != other.losingActions.Count) return false;

        for (int i = 0; i < winningActions.Count; i++)
        {
            if (winningActions[i] != other.winningActions[i]) return false;
        }

        for (int i = 0; i < losingActions.Count; i++)
        {
            if (losingActions[i] != other.losingActions[i]) return false;
        }

        return true;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref winningPlayer);
        serializer.SerializeValue(ref losingPlayer);
    }
}
