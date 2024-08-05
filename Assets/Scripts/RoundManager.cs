using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using GameActions;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    private Dictionary<ulong, List<List<RoundAction>>> m_PlayerRoundData;
    private int m_CurrentRound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        Instance = this;
    }

    private void Start()
    {
        m_PlayerRoundData = new Dictionary<ulong, List<List<RoundAction>>>();

        GameManager.Instance.OnStartMatchForServer += GameManager_StartMatchForServer;
        GameManager.Instance.OnAdvanceRound += GameManager_AdvanceRound;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnStartMatchForServer -= GameManager_StartMatchForServer;
        GameManager.Instance.OnAdvanceRound -= GameManager_AdvanceRound;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += ClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnectCallback;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
        {
            m_PlayerRoundData.Clear();
            NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnectCallback;
        }
    }

    public void ClientConnectedCallback(ulong clientID)
    {
        Debug.LogFormat("Round Manager has added player {0}", clientID);
        m_PlayerRoundData.Add(clientID, new List<List<RoundAction>>());
    }

    public void ClientDisconnectCallback(ulong clientID)
    {
        Debug.LogFormat("Round Manager has removed player {0}", clientID);
        m_PlayerRoundData.Remove(clientID);
    }

    private void GameManager_AdvanceRound()
    {
        m_CurrentRound++;
    }

    private void GameManager_StartMatchForServer()
    {
        if (IsServer)
        {
            foreach (KeyValuePair<ulong, List<List<RoundAction>>> playerRoundData in m_PlayerRoundData)
            {
                playerRoundData.Value.Add(new List<RoundAction>());
            }
            m_CurrentRound = 0;
        }
    }

    private void DecideRound()
    {
        if (!IsServer)
        {
            return;
        }

        var enumerator = m_PlayerRoundData.GetEnumerator();
        //for (int i = 0; i < GameManager.k_PlayersPerGame; i++)
        //{
        //    if (enumerator.MoveNext())
        //    {
        //        HACK: support more than two players?
        //    }
        //}
        enumerator.MoveNext();
        ulong player1 = enumerator.Current.Key;
        ActionType action1 = enumerator.Current.Value[GameManager.Instance.m_AtMatch.Value - 1][m_CurrentRound - 1].selectedAction;

        if (enumerator.MoveNext())
        {
            ulong player2 = enumerator.Current.Key;
            ActionType action2 = enumerator.Current.Value[GameManager.Instance.m_AtMatch.Value - 1][m_CurrentRound - 1].selectedAction;

            ActionMatchupResult result = ActionLogic.GetResult(action1, action2);

            if (result == ActionMatchupResult.Beats)
            {
                GameManager.Instance.MatchDecided(player1);
            }
            else if (result == ActionMatchupResult.Loses)
            {
                GameManager.Instance.MatchDecided(player2);
            }
            else
            {
                // HACK: support more than two players?
                //Dictionary<ulong, ActionType> currentRoundActions = m_PlayerRoundData.Select(
                //    playerRoundData => new KeyValuePair<ulong, ActionType>(playerRoundData.Key, playerRoundData.Value[GameManager.Instance.m_AtMatch.Value - 1][m_CurrentRound - 1].selectedAction)
                //).ToDictionary(x => x.Key, x => x.Value);

                //foreach (KeyValuePair<ulong, ActionType> takenAction in currentRoundActions)
                //{
                //    GameManager.Instance.GoldChange(takenAction.Key, ActionLogic.GetGoldChange(takenAction.Value));
                //}
                GameManager.Instance.GoldChange(player1, ActionLogic.GetGoldChange(action1));
                GameManager.Instance.GoldChange(player2, ActionLogic.GetGoldChange(action2));
            }
        }
        else
        {
            GameManager.Instance.GoldChange(player1, ActionLogic.GetGoldChange(action1));
        }
    }

    public void ReceiveAction(ulong playerID, ActionType gameAction)
    {
        if (!IsServer)
        {
            return;
        }

        RoundAction roundAction = new(gameAction, playerID);
        m_PlayerRoundData[playerID][GameManager.Instance.m_AtMatch.Value - 1].Add(roundAction);

        foreach (KeyValuePair<ulong, List<List<RoundAction>>> playerRoundData in m_PlayerRoundData)
        {
            if (playerRoundData.Value[GameManager.Instance.m_AtMatch.Value - 1].Count != m_CurrentRound)
            {
                Debug.LogFormat("player {0} has not sent enough actions, currently on round {1} with only {2} actions sent", playerRoundData.Key, m_CurrentRound, playerRoundData.Value[GameManager.Instance.m_AtMatch.Value - 1].Count);
                return;
            }
        }

        Debug.LogFormat("all {0} players have sent actions for round {1}, deciding round", m_PlayerRoundData.Count, m_CurrentRound);
        DecideRound();
    }
}
