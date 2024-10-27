using System;
using System.Collections.Generic;
using Unity.Netcode;

[Serializable]
public class MatchData : IEquatable<MatchData>, INetworkSerializable
{
    int matchNumber;

    ulong winningPlayer;
    GameAction[] winningActions;

    ulong losingPlayer;
    GameAction[] losingActions;

    public MatchData()
    {
        matchNumber = 0;
        winningPlayer = 0;
        winningActions = new GameAction[0];
        losingPlayer = 0;
        losingActions = new GameAction[0];
    }

    public MatchData(int matchNumber, ulong winningPlayer, GameAction[] winningActions, ulong losingPlayer, GameAction[] losingActions)
    {
        this.matchNumber = matchNumber;
        this.winningPlayer = winningPlayer;
        this.winningActions = winningActions;
        this.losingPlayer = losingPlayer;
        this.losingActions = losingActions;
    }

    public void SetMatchNumber(int match)
    {
        matchNumber = match;
    }

    public int GetMatchNumber()
    {
        return matchNumber;
    }

    public void SetWinner(ulong winner, List<GameAction> actions)
    {
        winningPlayer = winner;
        winningActions = actions.ToArray();
    }

    public ulong GetWinner()
    {
        return winningPlayer;
    }

    public void SetLoser(ulong loser, List<GameAction> actions)
    {
        losingPlayer = loser;
        losingActions = actions.ToArray();
    }

    public ulong GetLoser()
    {
        return losingPlayer;
    }

    public int GetNumberOfActions()
    {
        return winningActions.Length;
    }

    public GameAction GetAction(ulong id, int index)
    {
        if (id == winningPlayer)
        {
            return winningActions[index];
        }
        else
        {
            return losingActions[index];
        }
    }

    public bool Equals(MatchData other)
    {
        if (!winningPlayer.Equals(other.winningPlayer)) return false;
        if (winningActions.Length != other.winningActions.Length) return false;
        if (losingActions.Length != other.losingActions.Length) return false;

        for (int i = 0; i < winningActions.Length; i++)
        {
            if (winningActions[i] != other.winningActions[i]) return false;
        }

        for (int i = 0; i < losingActions.Length; i++)
        {
            if (losingActions[i] != other.losingActions[i]) return false;
        }

        return true;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            winningActions = new GameAction[winningActions.Length];
            losingActions = new GameAction[losingActions.Length];
        }

        serializer.SerializeValue(ref matchNumber);
        serializer.SerializeValue(ref winningActions);
        serializer.SerializeValue(ref losingActions);
        serializer.SerializeValue(ref winningPlayer);
        serializer.SerializeValue(ref losingPlayer);
    }
}
