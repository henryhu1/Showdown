using System.Collections.Generic;
using System;

namespace GameActions
{
    public static class ActionLogic
    {
        // TODO: refactor to extensions
        private static readonly Dictionary<GameAction, int> actionGoldChange = new()
        {
            { GameAction.Collect, 1 },
            { GameAction.Block, 0 },
            { GameAction.Attack, -1 },
            { GameAction.Fire, -3 },
            { GameAction.Water, -3 },
            { GameAction.Egg, -4 },
            { GameAction.Reflect, -1 },
        };

        private static readonly Dictionary<GameAction, HashSet<GameAction>> beatsMap = new()
        {
            { GameAction.Collect, new() },
            { GameAction.Block, new() },
            { GameAction.Attack, new() { GameAction.Collect, GameAction.Egg, } },
            { GameAction.Fire, new() { GameAction.Collect, GameAction.Fire, GameAction.Egg, } },
            { GameAction.Water, new() { GameAction.Collect, GameAction.Attack, GameAction.Fire, } },
            { GameAction.Egg, new() { GameAction.Collect, GameAction.Water, GameAction.Reflect, } },
            { GameAction.Reflect, new() { GameAction.Attack, } },
        };

        private static readonly Dictionary<GameAction, ActionType> actionTypes = new()
        {
            { GameAction.Collect, ActionType.Passive },
            { GameAction.Block, ActionType.Defensive },
            { GameAction.Reflect, ActionType.Defensive },
            { GameAction.Attack, ActionType.Offensive },
            { GameAction.Fire, ActionType.Offensive },
            { GameAction.Water, ActionType.Offensive },
            { GameAction.Egg, ActionType.Offensive },
        };

        public static bool Beats(GameAction action1, GameAction action2)
        {
            if (beatsMap.TryGetValue(action1, out HashSet<GameAction> beats))
            {
                return beats.Contains(action2);
            }
            throw new ArgumentException($"Game Action {action1} not recognized.");
        }

        public static int GetGoldChange(GameAction gameAction)
        {
            if (actionGoldChange.TryGetValue(gameAction, out int cost))
            {
                return cost;
            }
            throw new ArgumentException($"Game Action {gameAction} not recognized.");
        }

        public static ActionType GetActionType(GameAction gameAction)
        {
            if (actionTypes.TryGetValue(gameAction, out ActionType actionType))
            {
                return actionType;
            }
            throw new ArgumentException($"Game Action {gameAction} not recognized.");
        }

        public static HashSet<GameAction> GetPlayableActionsGivenGold(int goldCount)
        {
            HashSet<GameAction> playable = new();
            foreach (KeyValuePair<GameAction, int> actionCost in actionGoldChange)
            {
                if (goldCount + actionCost.Value > 0)
                {
                    playable.Add(actionCost.Key);
                }
            }
            return playable;
        }

        public static ActionMatchupResult GetResult(GameAction action1, GameAction action2)
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
