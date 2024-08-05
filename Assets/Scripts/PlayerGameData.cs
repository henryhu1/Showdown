using System;
using System.Collections.Generic;
using GameActions;

public class PlayerGameData
{
    //private ulong m_ClientID;
    public ulong ClientID; // { get { return m_ClientID; } private set { m_ClientID = value; } }

    //private int m_GoldTotal;
    public int GoldTotal; // { get { return m_GoldTotal; } private set { m_GoldTotal = value; } }

    //private int m_AtRound;
    public int AtRound; // { get { return m_AtRound; } private set { m_AtRound = value; } }

    public int MatchesWon;

    private List<ActionType> m_TakenActions;

    public PlayerGameData(ulong clientID) {
        ClientID = clientID;
        AtRound = 0;
        GoldTotal = 0;
        MatchesWon = 0;
        m_TakenActions = new List<ActionType>();
    }

    public void PlayAction(ActionType action)
    {
        AtRound++;
        m_TakenActions.Add(action);
    }

    public ActionType GetAction(int round)
    {
        if (round > 0 && m_TakenActions.Count < round)
        {
            return m_TakenActions[round];
        }
        else throw new Exception($"Action for round #{round} does not exist for player {ClientID}");
    }
}
