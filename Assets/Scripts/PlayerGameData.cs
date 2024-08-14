using System;
using System.Collections.Generic;
using System.Linq;
using GameActions;

public class PlayerGameData
{
    private readonly ulong clientId;

    private int goldTotal;
    public int GoldTotal { get { return goldTotal; } }

    private int matchesWon;
    public int MatchesWon { get { return matchesWon; } }

    private List<List<GameAction>> takenActions;
    private Dictionary<GameAction, bool> allowedActions;

    public PlayerGameData(ulong clientID) {
        clientId = clientID;
        goldTotal = 0;
        matchesWon = 0;
        takenActions = new();
        allowedActions = new();
        foreach (GameAction action in Enum.GetValues(typeof(GameAction)).Cast<GameAction>())
        {
            allowedActions.Add(action, false);
        }
    }

    public void SetBeginningGold()
    {
        goldTotal = GameManager.k_GoldStartingAmount;
        UpdatePlayableActions();
    }

    public void AddActionsForMatch()
    {
        takenActions.Add(new List<GameAction>());
    }

    public void ChangeGold(int changeAmount)
    {
        goldTotal += changeAmount;
        UpdatePlayableActions();
    }

    public void UpdatePlayableActions()
    {
        Dictionary<GameAction, bool> playable = new();
        foreach (GameAction action in Enum.GetValues(typeof(GameAction)).Cast<GameAction>())
        {
            playable.Add(action, goldTotal + ActionLogic.GetGoldChange(action) > 0);
        }
        allowedActions = playable;
    }


    public void WonMatch()
    {
        matchesWon++;
    }

    public bool CanPlayAction(GameAction gameAction)
    {
        return goldTotal + ActionLogic.GetGoldChange(gameAction) < 0;
    }

    // TODO: get rid of match indexing, just add to Last() match in list
    public void PlayAction(int match, GameAction action)
    {
        takenActions[match].Add(action);
    }

    public GameAction GetAction(int match, int round)
    {
        if (match >= 0 && round >= 0 && takenActions.Count >= match + 1 && takenActions[match].Count >= round + 1)
        {
            return takenActions[match][round];
        }
        else throw new Exception($"Action for round #{round} does not exist for player {clientId}");
    }

    public int GetNumberOfActionsPlayedInMatch(int match)
    {
        if (match >= 0 && takenActions.Count >= match + 1)
        {
            return takenActions[match].Count;
        }
        else throw new Exception($"Actions for match #{match} does not exist for player {clientId}");
    }

    public void ClearActions()
    {
        takenActions.Clear();
    }
}
