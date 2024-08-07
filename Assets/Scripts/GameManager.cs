using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using GameActions;
using System;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int k_PlayersPerGame = 1;
    public const int k_GoldStartingAmount = 0;
    public static int s_goldTotalToWin { get; private set; } = 5;
    public static int s_BestOf { get; private set; } = 3;
    public static int s_TicksPerRound { get; private set; } = 4;
    public static float s_TimePerTick { get; private set; } = 0.66f;

    private Dictionary<ulong, PlayerGameData> m_PlayerGameData;
    public NetworkVariable<int> m_AtMatch = new(0);

    private Coroutine m_TickCoroutine;

    [HideInInspector]
    public delegate void PlayerGoldChangeDelegateHandler(int newAmount);
    [HideInInspector]
    public event PlayerGoldChangeDelegateHandler OnPlayerGoldChange;

    [HideInInspector]
    public delegate void MatchCountdownDelegateHandler(int countdown);
    [HideInInspector]
    public event MatchCountdownDelegateHandler OnMatchCountdown;

    [HideInInspector]
    public delegate void MatchWonDelegateHandler();
    [HideInInspector]
    public event MatchWonDelegateHandler OnMatchWon;

    [HideInInspector]
    public delegate void MatchLostDelegateHandler();
    [HideInInspector]
    public event MatchLostDelegateHandler OnMatchLost;

    [HideInInspector]
    public delegate void DisableActionsToBePlayedDelegateHandler();
    [HideInInspector]
    public event DisableActionsToBePlayedDelegateHandler OnDisableActionsToBePlayed;

    [HideInInspector]
    public delegate void EnableActionsToBePlayedDelegateHandler();
    [HideInInspector]
    public event EnableActionsToBePlayedDelegateHandler OnEnableActionsToBePlayed;

    [HideInInspector]
    public delegate void StartMatchForServerDelegateHandler();
    [HideInInspector]
    public event StartMatchForServerDelegateHandler OnStartMatchForServer;

    [HideInInspector]
    public delegate void AdvanceRoundDelegateHandler();
    [HideInInspector]
    public event AdvanceRoundDelegateHandler OnAdvanceRound;

    [HideInInspector]
    public delegate void TickTimePassesDelegateHandler(float time, int ticksPlayed);
    [HideInInspector]
    public event  TickTimePassesDelegateHandler OnTickTimePasses;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        Instance = this;
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        m_PlayerGameData = new Dictionary<ulong, PlayerGameData>();
    }

    private void Update()
    {
        if (NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.ShutdownInProgress)
        {

        }
        else
        {
            // TODO: Disconnected
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log("Network spawn!");

        if (IsClient && !IsServer)
        {
        }

        SceneTransitionHandler.Instance.SetSceneState(SceneStates.InGame);

        if (IsServer)
        {
            m_PlayerGameData.Clear();
            m_AtMatch.Value = 0;

            // SceneTransitionHandler.Instance.OnAllClientsLoadedScene += SceneTransitionHandler_AllClientsLoadedScene;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (m_TickCoroutine != null)
        {
            StopCoroutine(m_TickCoroutine);
            m_TickCoroutine = null;
        }

        if (IsServer)
        {
            m_PlayerGameData.Clear();
            // SceneTransitionHandler.Instance.OnAllClientsLoadedScene -= SceneTransitionHandler_AllClientsLoadedScene;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        AddPlayerGameData(clientId);
    }

    private void OnClientDisconnect(ulong clientId)
    {
        NetworkManager.Singleton.Shutdown();
    }

    private void AddPlayerGameData(ulong clientId)
    {
        if (m_PlayerGameData.Count == k_PlayersPerGame)
        {
            Debug.LogFormat("Already have {0} (should be {1}) player(s) in game, player {2} will not be playing", NetworkManager.Singleton.ConnectedClients.Count, k_PlayersPerGame, clientId);
            return;
        }

        if (m_PlayerGameData.ContainsKey(clientId))
        {
            Debug.LogFormat("Player {0} already in game, currently at {1} player(s)", clientId, NetworkManager.Singleton.ConnectedClients.Count);
            return;
        }

        PlayerGameData playerGameData = new(clientId);
        m_PlayerGameData.Add(clientId, playerGameData);

        Debug.LogFormat("Player {0} in game, now have {1} player(s)", clientId, NetworkManager.Singleton.ConnectedClients.Count);
        if (NetworkManager.Singleton.ConnectedClients.Count == k_PlayersPerGame)
        {
            StartMatch();
        }
    }

    private void StartMatch()
    {
        if (!IsServer)
        {
            return;
        }

        foreach (var playerData in m_PlayerGameData)
        {
            playerData.Value.SetBeginningGold();
            playerData.Value.AddActionsForMatch();
            SetupPlayerActionsGivenGoldClientRpc(playerData.Value.GoldTotal, RpcTarget.Single(playerData.Key, RpcTargetUse.Temp));
        }

        OnStartMatchForServer?.Invoke();

        StartGameClockRpc();

        m_AtMatch.Value++;
    }

    public void ClearPlayerActions()
    {
        if (!IsServer) return;

        foreach (var playerData in m_PlayerGameData.Values)
        {
            playerData.ClearActions();
        }
    }

    private IEnumerator TickCount()
    {
        float time = 0;
        int ticksPlayed = -s_TicksPerRound;
        OnDisableActionsToBePlayed?.Invoke();
        while (true) // TODO: condition for round timer to be playing
        {
            time += Time.deltaTime;

            OnTickTimePasses?.Invoke(time, ticksPlayed); // TODO: should each time update be invoking an event?
            // Other solution: have GameTickVisualizer run its own coroutine, but these ticks and the visualizer may be out of sync?

            if (time >= s_TimePerTick)
            {
                time = 0;
                ticksPlayed++;

                bool isEndTick = ticksPlayed % s_TicksPerRound == 0;
                bool isEnableActionsTick = (ticksPlayed + 1) % s_TicksPerRound == 0;

                if (isEndTick)
                {
                    SoundFXManager.Instance.PlayEndTickSound(100);
                }
                else
                {
                    SoundFXManager.Instance.PlayTickSound(100);
                }

                if (ticksPlayed < 0)
                {
                    OnMatchCountdown?.Invoke(-ticksPlayed);
                }
                // TODO: consolidate ActionManager to subscribe to events instead of directly calling from GameManager
                else if (ticksPlayed > 0 && isEndTick)
                {
                    OnAdvanceRound?.Invoke();
                    OnDisableActionsToBePlayed?.Invoke();
                    ActionManager.Instance.SubmitAction();
                }
                else if (isEnableActionsTick)
                {
                    ActionManager.Instance.EnqueueAction(ActionType.Block, false);
                    OnEnableActionsToBePlayed?.Invoke();
                }
            }
            yield return null;
        }
    }

    public void GoldChange(ulong player, int changeAmount)
    {
        if (!IsServer)
        {
            return;
        }

        if (m_PlayerGameData.TryGetValue(player, out PlayerGameData playerGameData))
        {
            playerGameData.ChangeGold(changeAmount);
            Debug.LogFormat("player {0} now has {1} gold", player, playerGameData.GoldTotal);
            SetupPlayerActionsGivenGoldClientRpc(playerGameData.GoldTotal, RpcTarget.Single(player, RpcTargetUse.Temp));
            if (playerGameData.GoldTotal >= s_goldTotalToWin)
            {
                MatchDecided(player);
            }
            //GoldChangeClientRpc(playerGameData.goldTotal, RpcTarget.Single(player, RpcTargetUse.Temp));
            //if (changeAmount < 0 && playerGameData.goldTotal < Mathf.Abs(s_AttackCost))
            //{
            //    DisableAttackClientRpc(RpcTarget.Single(player, RpcTargetUse.Temp));
            //}
            //else if (playerGameData.goldTotal - changeAmount < Mathf.Abs(s_AttackCost) && playerGameData.goldTotal >= Mathf.Abs(s_AttackCost))
            //{
            //    EnableAttackClientRpc(RpcTarget.Single(player, RpcTargetUse.Temp));
            //}
        }
    }

    public int FirstTo()
    {
        return Mathf.FloorToInt(s_BestOf / 2 + 1);
    }

    public void MatchDecided(ulong winner)
    {
        if (!IsServer)
        {
            return;
        }

        List<ulong> losers = NetworkManager.Singleton.ConnectedClientsIds.ToList();
        losers.Remove(winner);
        if (losers.Count > 0)
        {
            MatchLoserClientRpc(RpcTarget.Group(losers, RpcTargetUse.Temp));
        }
        MatchWinnerClientRpc(RpcTarget.Single(winner, RpcTargetUse.Temp));

        m_PlayerGameData[winner].WonMatch();
        if (m_PlayerGameData[winner].MatchesWon == FirstTo())
        {
            NetworkManager.Singleton.Shutdown();
        }
        else
        {
            StartMatch();
        }
    }

    [Rpc(SendTo.Everyone)]
    public void StartGameClockRpc()
    {
        if (m_TickCoroutine != null)
        {
            StopCoroutine(m_TickCoroutine);
            m_TickCoroutine = null;
        }
        m_TickCoroutine = StartCoroutine(TickCount());
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void SetupPlayerActionsGivenGoldClientRpc(int playerGold, RpcParams rpcParams)
    {
        //clientRpcParams.Send.TargetClientIds = new ulong[0];
        //onActionDone?.Invoke();
        Debug.LogFormat("this client now has {0} gold", playerGold);
        OnPlayerGoldChange?.Invoke(playerGold);

        // OnDisableActionsToBePlayed?.Invoke();

        ActionManager.Instance.SetLocalAllowedActions(playerGold);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendActionServerRpc(ActionType gameAction, ServerRpcParams serverRpcParams = default)
    {
        ulong playerID = serverRpcParams.Receive.SenderClientId;
        Debug.LogFormat("player {0} has sent action {1}", playerID, gameAction);

        if (m_PlayerGameData.TryGetValue(playerID, out PlayerGameData playerData)) {

            if (playerData.CanPlayAction(gameAction))
            {
                Debug.LogFormat("player {0} only has {1} gold which is not enough for {2}, requiring {3} gold, using {4} instead", playerID, playerData.GoldTotal, gameAction, Mathf.Abs(ActionLogic.GetGoldChange(gameAction)), ActionType.Block);
                gameAction = ActionType.Block;
            }

            Debug.LogFormat("player {0} using action {1}", playerID, gameAction);
            //RoundAction roundAction = new(gameAction, playerID);
            m_PlayerGameData[playerID].PlayAction(m_AtMatch.Value - 1, gameAction);

            foreach (var player in m_PlayerGameData)
            {
                if (player.Value.GetNumberOfActionsPlayedInMatch(m_AtMatch.Value - 1) != RoundManager.Instance.m_CurrentRound.Value)
                {
                    Debug.LogFormat(
                        "player {0} has not sent enough actions, currently on round {1} with only {2} actions sent",
                        player.Key,
                        RoundManager.Instance.m_CurrentRound.Value,
                        player.Value.GetNumberOfActionsPlayedInMatch(m_AtMatch.Value - 1)
                    );
                    return;
                }
            }

            Debug.LogFormat("all {0} players have sent actions for round {1}, deciding round", m_PlayerGameData.Count, RoundManager.Instance.m_CurrentRound.Value);
            Dictionary<ulong, ActionType> currentRoundActions = m_PlayerGameData.Select(
                playerData => new KeyValuePair<ulong, ActionType>(playerData.Key, playerData.Value.GetAction(m_AtMatch.Value - 1, RoundManager.Instance.m_CurrentRound.Value - 1))
            ).ToDictionary(x => x.Key, x => x.Value);
            RoundManager.Instance.DecideRound(currentRoundActions);
        }
        else
        {
            Debug.LogFormat("could not find player {0}, player will do nothing", playerID);
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void MatchWinnerClientRpc(RpcParams rpcParams)
    {
        Debug.LogFormat("This player has won!");
        OnMatchWon?.Invoke();
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void MatchLoserClientRpc(RpcParams rpcParams)
    {
        Debug.LogFormat("This player has lost!");
        OnMatchLost?.Invoke();
    }
}
