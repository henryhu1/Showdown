using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using GameActions;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    public NetworkVariable<int> m_CurrentRound = new(0);

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
            GameManager.Instance.ClearPlayerActions();
            NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnectCallback;
        }
    }

    public void ClientConnectedCallback(ulong clientID)
    {
    }

    public void ClientDisconnectCallback(ulong clientID)
    {
    }

    private void GameManager_AdvanceRound()
    {
        m_CurrentRound.Value++;
    }

    private void GameManager_StartMatchForServer()
    {
        if (IsServer)
        {
            m_CurrentRound.Value = 0;
        }
    }

    public void DecideRound(Dictionary<ulong, ActionType> playerRoundData)
    {
        if (!IsServer)
        {
            return;
        }

        var enumerator = playerRoundData.GetEnumerator();
        //for (int i = 0; i < GameManager.k_PlayersPerGame; i++)
        //{
        //    if (enumerator.MoveNext())
        //    {
        //        HACK: support more than two players?
        //    }
        //}
        enumerator.MoveNext();
        ulong player1 = enumerator.Current.Key;
        ActionType action1 = enumerator.Current.Value;

        if (enumerator.MoveNext())
        {
            ulong player2 = enumerator.Current.Key;
            ActionType action2 = enumerator.Current.Value;

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
            // HACK: for development when only one build is run
            GameManager.Instance.GoldChange(player1, ActionLogic.GetGoldChange(action1));
        }
    }
}
