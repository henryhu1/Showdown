using System.Collections.Generic;
using System;

namespace GameActions
{
    public static class ActionLogic
    {
        private static readonly Dictionary<ActionType, int> actionGoldChange = new()
        {
            { ActionType.Collect, 1 },
            { ActionType.Block, 0 },
            { ActionType.Attack, -1 },
            { ActionType.Fire, -3 },
            { ActionType.Water, -3 },
            { ActionType.Egg, -4 },
            { ActionType.Reflect, -1 },
        };

        private static readonly Dictionary<ActionType, HashSet<ActionType>> beatsMap = new()
        {
            { ActionType.Collect, new() },
            { ActionType.Block, new() },
            { ActionType.Attack, new() { ActionType.Collect, ActionType.Egg, } },
            { ActionType.Fire, new() { ActionType.Collect, ActionType.Fire, ActionType.Egg, } },
            { ActionType.Water, new() { ActionType.Collect, ActionType.Attack, ActionType.Fire, } },
            { ActionType.Egg, new() { ActionType.Collect, ActionType.Water, ActionType.Reflect, } },
            { ActionType.Reflect, new() { ActionType.Attack, } },
        };

        public static bool Beats(ActionType action1, ActionType action2)
        {
            if (beatsMap.TryGetValue(action1, out HashSet<ActionType> beats))
            {
                return beats.Contains(action2);
            }
            throw new ArgumentException($"Game Action {action1} not recognized.");
        }

        public static int GetGoldChange(ActionType gameAction)
        {
            if (actionGoldChange.TryGetValue(gameAction, out int cost))
            {
                return cost;
            }
            throw new ArgumentException($"Game Action {gameAction} not recognized.");
        }

        public static HashSet<ActionType> GetPlayableActionsGivenGold(int goldCount)
        {
            HashSet<ActionType> playable = new();
            foreach (KeyValuePair<ActionType, int> actionCost in actionGoldChange)
            {
                if (goldCount + actionCost.Value > 0)
                {
                    playable.Add(actionCost.Key);
                }
            }
            return playable;
        }

        public static ActionMatchupResult GetResult(ActionType action1, ActionType action2)
        {
            if (Beats(action1, action2))
            {
                return ActionMatchupResult.Beats;
            }
            else if (Beats(action2, action1))
            {
                return ActionMatchupResult.Loses;
            }
            else
            {
                return ActionMatchupResult.Ties;
            }
        }
    }
}
