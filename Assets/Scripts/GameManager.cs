using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using GameActions;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int k_PlayersPerGame = 2;
    public const int k_GoldStartingAmount = 0;
    public const int k_StartingGoldTotalToWin = 5;
    public const int k_BestOf = 3;
    public const int k_TicksPerRound = 4;
    public const float k_TimePerTick = 0.66f;
    public static float RoundCycle { get { return k_TicksPerRound * k_TimePerTick; } }

    private Dictionary<ulong, PlayerGameData> m_PlayerGameData;

    public NetworkVariable<int> m_AtMatch = new(0);
    public NetworkVariable<int> m_GoldTotalToWin = new(k_StartingGoldTotalToWin);
    public NetworkVariable<bool> m_IsSuddenDeath = new(false);

    public Coroutine m_TickCoroutine;

    [HideInInspector]
    public delegate void PlayerGoldChangeDelegateHandler(int newAmount);
    [HideInInspector]
    public event PlayerGoldChangeDelegateHandler OnPlayerGoldChange;

    [HideInInspector]
    public delegate void MatchCountdownDelegateHandler(int countdown);
    [HideInInspector]
    public event MatchCountdownDelegateHandler OnMatchCountdown;

    [HideInInspector]
    public delegate void MatchDecidedDelegateHandler(bool hasPlayerWon, MatchResult resultType);
    [HideInInspector]
    public event MatchDecidedDelegateHandler OnMatchDecided;

    [HideInInspector]
    public delegate void DisableActionsToBePlayedDelegateHandler();
    [HideInInspector]
    public event DisableActionsToBePlayedDelegateHandler OnDisableActionsToBePlayed;

    [HideInInspector]
    public delegate void EnableActionsToBePlayedDelegateHandler();
    [HideInInspector]
    public event EnableActionsToBePlayedDelegateHandler OnEnableActionsToBePlayed;

    [HideInInspector]
    public delegate void BeforeActionSubmitDelegateHandler();
    [HideInInspector]
    public event BeforeActionSubmitDelegateHandler OnBeforeActionSubmit;

    [HideInInspector]
    public delegate void TickBeforeActionSubmitDelegateHandler();
    [HideInInspector]
    public event TickBeforeActionSubmitDelegateHandler OnTickBeforeActionSubmit;

    [HideInInspector]
    public delegate void StartMatchForServerDelegateHandler();
    [HideInInspector]
    public event StartMatchForServerDelegateHandler OnStartMatchForServer;

    [HideInInspector]
    public delegate void GameFinishedDelegateHandler();
    [HideInInspector]
    public event GameFinishedDelegateHandler OnGameFinished;

    [HideInInspector]
    public delegate void ActionsDoneDelegateHandler(GameAction playerAction, GameAction opponentAction);
    [HideInInspector]
    public event ActionsDoneDelegateHandler OnActionsDone;

    [HideInInspector]
    public delegate void OpponentGoldStateDelegateHandler(GoldState opponentGoldState);
    [HideInInspector]
    public event OpponentGoldStateDelegateHandler OnOpponentGoldState;

    [HideInInspector]
    public delegate void AdvanceRoundDelegateHandler();
    [HideInInspector]
    public event AdvanceRoundDelegateHandler OnAdvanceRound;

    [HideInInspector]
    public delegate void TickTimePassesDelegateHandler(float time, int ticksPlayed);
    [HideInInspector]
    public event TickTimePassesDelegateHandler OnTickTimePasses;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
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

        OnDisableActionsToBePlayed?.Invoke();

        RegisterNetworkVariableCallbacks();

        if (IsClient && !IsServer)
        {
            NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        }

        if (IsServer)
        {
            m_PlayerGameData.Clear();
            m_AtMatch.Value = 0;

            SceneTransitionHandler.Instance.RegisterCallbacks();
            SceneTransitionHandler.Instance.OnAllClientsLoadedScene += SceneTransitionHandler_AllClientsLoadedScene;
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

        UnregisterNetworkVariableCallbacks();

        if (IsClient && !IsServer)
        {
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
        }

        if (IsServer)
        {
            m_PlayerGameData.Clear();
            SceneTransitionHandler.Instance.UnregisterCallbacks();
            SceneTransitionHandler.Instance.OnAllClientsLoadedScene -= SceneTransitionHandler_AllClientsLoadedScene;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    private void SceneTransitionHandler_AllClientsLoadedScene()
    {
        if (SceneTransitionHandler.Instance.IsInGameScene())
        {
            StartMatch();
        }
    }
    
    private void RegisterNetworkVariableCallbacks()
    {
        if (SceneTransitionHandler.Instance.IsInGameScene())
        {
            GoldCount.Instance.RegisterNetworkCallbacks();
            ActionButtonsGroup.Instance.RegisterNetworkCallbacks();
        }
    }
    
    private void UnregisterNetworkVariableCallbacks()
    {
        if (SceneTransitionHandler.Instance.IsInGameScene())
        {
            GoldCount.Instance.UnregisterNetworkCallbacks();
            ActionButtonsGroup.Instance.UnregisterNetworkCallbacks();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        AddPlayerGameData(clientId);
    }

    private void OnClientDisconnect(ulong clientId)
    {
        StopGame();
    }

    private void OnServerStopped(bool obj)
    {
        StopGame();
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
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == SceneState.GameScene.ToString())
            {
                StartMatch();
            }
            else
            {
                SceneTransitionHandler.Instance.SwitchToGameScene();
            }
            //StartMatch();
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
            playerData.Value.SetupDataForNewMatch();
            SetupPlayerActionsGivenGoldClientRpc(playerData.Value.GoldTotal, RpcTarget.Single(playerData.Key, RpcTargetUse.Temp));
            // TODO: clean up logic for resetting opponent tower sprite
            OpponentGoldStateClientRpc(GoldState.Low, RpcTarget.Single(playerData.Key, RpcTargetUse.Temp));
        }

        m_GoldTotalToWin.Value = k_StartingGoldTotalToWin;
        m_IsSuddenDeath.Value = false;

        OnStartMatchForServer?.Invoke();

        StartGameClockRpc();

        m_AtMatch.Value++;
    }

    private void StopGame()
    {
        // GameFinishedClientRpc();
        EndGame();
    }

    private void EndGame()
    {
        GoldCount.Instance.UnregisterNetworkCallbacks();
        ActionButtonsGroup.Instance.UnregisterNetworkCallbacks();
        NetworkManager.Singleton.Shutdown();
        SceneTransitionHandler.Instance.SetSceneState(SceneState.StartScene);
        SceneTransitionHandler.Instance.SwitchToMainMenuScene();
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
        int ticksPlayed = -k_TicksPerRound;
        OnDisableActionsToBePlayed?.Invoke();
        while (true) // TODO: condition for round timer to be playing
        {
            time += Time.deltaTime;

            OnTickTimePasses?.Invoke(time, ticksPlayed); // TODO: should each time update be invoking an event?
            // Other solution: have GameTickVisualizer run its own coroutine, but these ticks and the visualizer may be out of sync?

            if (time >= k_TimePerTick)
            {
                time = 0;
                ticksPlayed++;

                bool isEndTick = ticksPlayed % k_TicksPerRound == 0;
                bool isTickBeforeActionSubmit = (ticksPlayed + 1) % k_TicksPerRound == 0;

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
                else if (ticksPlayed == 0)
                {
                    OnEnableActionsToBePlayed?.Invoke();
                }
                // TODO: consolidate ActionManager to subscribe to events instead of directly calling from GameManager
                else if (ticksPlayed > 0 && isEndTick)
                {
                    if (IsServer) OnAdvanceRound?.Invoke();

                    OnBeforeActionSubmit?.Invoke();
                    ActionManager.Instance.SubmitAction();
                }
                else if (isTickBeforeActionSubmit)
                {
                    ActionManager.Instance.EnqueueAction(GameAction.Block, false);
                    OnTickBeforeActionSubmit?.Invoke();
                    //OnEnableActionsToBePlayed?.Invoke();
                }
            }
            yield return null;
        }
    }

    public void RoundEnd(RoundAction playerRoundAction, RoundAction opponentRoundAction)
    {
        if (!IsServer) return;

        ulong player = playerRoundAction.playerID;
        GameAction playerAction = playerRoundAction.selectedAction;
        GameAction opponentAction = opponentRoundAction.selectedAction;

        if (!m_PlayerGameData.ContainsKey(player)) return;

        ActionsDoneClientRpc(playerAction, opponentAction, RpcTarget.Single(player, RpcTargetUse.Temp));
    }

    // TODO: return bool indicating if player won by gold, which requires throwing exception
    public void HandlePlayerGoldChange(ulong player, GameAction action)
    {
        if (!IsServer) return;

        if (!m_PlayerGameData.ContainsKey(player)) return;

        PlayerGameData playerGameData = m_PlayerGameData[player];

        int changeAmount = ActionLogic.GetGoldChange(action);
        playerGameData.ChangeGold(changeAmount);
        Debug.LogFormat("player {0} now has {1} gold", player, playerGameData.GoldTotal);

        SetupPlayerActionsGivenGoldClientRpc(playerGameData.GoldTotal, RpcTarget.Single(player, RpcTargetUse.Temp));
    }

    public GoldState GetPlayerGoldState(ulong player)
    {
        // TODO: do not return Low, throw exception instead
        if (!IsServer) return GoldState.Low;

        if (!m_PlayerGameData.ContainsKey(player)) return GoldState.Low;

        PlayerGameData playerGameData = m_PlayerGameData[player];
        GoldState playerGoldState;
        if (playerGameData.GoldTotal + ActionLogic.GetGoldChange(GameAction.Collect) == m_GoldTotalToWin.Value)
        {
            playerGoldState = GoldState.High;
        }
        else if (playerGameData.GoldTotal >= m_GoldTotalToWin.Value / 2)
        {
            playerGoldState = GoldState.Mid;
        }
        else
        {
            playerGoldState = GoldState.Low;
        }
        return playerGoldState;
    }

    public void HandleGoldChange(Dictionary<ulong, GameAction> playerRoundData)
    {
        if (!IsServer) return;

        var enumerator = playerRoundData.GetEnumerator();
        enumerator.MoveNext();
        ulong player1 = enumerator.Current.Key;
        GameAction action1 = enumerator.Current.Value;

        HandlePlayerGoldChange(player1, action1);

        PlayerGameData playerGameData1 = m_PlayerGameData[player1];
        bool reachedGoldPlayer1 = playerGameData1.GoldTotal >= m_GoldTotalToWin.Value;

        if (!enumerator.MoveNext()) return;

        ulong player2 = enumerator.Current.Key;
        GameAction action2 = enumerator.Current.Value;
        HandlePlayerGoldChange(player2, action2);

        GoldState goldStatePlayer1 = GetPlayerGoldState(player1);
        OpponentGoldStateClientRpc(goldStatePlayer1, RpcTarget.Single(player2, RpcTargetUse.Temp));

        GoldState goldStatePlayer2 = GetPlayerGoldState(player2);
        OpponentGoldStateClientRpc(goldStatePlayer2, RpcTarget.Single(player1, RpcTargetUse.Temp));

        PlayerGameData playerGameData2 = m_PlayerGameData[player2];
        bool reachedGoldPlayer2 = playerGameData2.GoldTotal >= m_GoldTotalToWin.Value;

        if (reachedGoldPlayer1 ^ reachedGoldPlayer2)
        {
            ulong winner = reachedGoldPlayer1 ? player1 : player2;
            ulong loser = reachedGoldPlayer1 ? player2 : player1;
            MatchDecided(winner, loser, MatchResult.GoldReached);
            return;
        }
        else if (reachedGoldPlayer1 && reachedGoldPlayer2)
        {
            TriggerSuddenDeath();
        }
    }

    private void TriggerSuddenDeath()
    {
        m_IsSuddenDeath.Value = true;
        m_GoldTotalToWin.Value++;
    }

    public int FirstTo()
    {
        return Mathf.FloorToInt(k_BestOf / 2 + 1);
    }

    public void MatchDecided(ulong winner, ulong loser, MatchResult resultType)
    {
        if (!IsServer) return;

        if (winner != loser)
        {
            MatchDecidedClientRpc(false, resultType, RpcTarget.Single(loser, RpcTargetUse.Temp));
        }
        MatchDecidedClientRpc(true, resultType, RpcTarget.Single(winner, RpcTargetUse.Temp));

        m_PlayerGameData[winner].WonMatch();
        if (m_PlayerGameData[winner].MatchesWon == FirstTo())
        {
            StopGame();
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
        ActionManager.Instance.SetLocalAllowedActions(playerGold);
        OnPlayerGoldChange?.Invoke(playerGold);

        // OnDisableActionsToBePlayed?.Invoke();

        //ActionManager.Instance.SetLocalAllowedActions(playerGold); this has been made to respond to OnPlayerGoldChange above
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void ActionsDoneClientRpc(GameAction playerAction, GameAction opponentAction, RpcParams rpcParams)
    {
        OnActionsDone?.Invoke(playerAction, opponentAction);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void OpponentGoldStateClientRpc(GoldState opponentGoldState, RpcParams rpcParams)
    {
        OnOpponentGoldState?.Invoke(opponentGoldState);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendActionServerRpc(GameAction gameAction, ServerRpcParams serverRpcParams = default)
    {
        ulong playerID = serverRpcParams.Receive.SenderClientId;
        Debug.LogFormat("player {0} has sent action {1}", playerID, gameAction);

        if (m_PlayerGameData.TryGetValue(playerID, out PlayerGameData playerData)) {

            if (playerData.CanPlayAction(gameAction))
            {
                Debug.LogFormat("player {0} only has {1} gold which is not enough for {2}, requiring {3} gold, using {4} instead", playerID, playerData.GoldTotal, gameAction, Mathf.Abs(ActionLogic.GetGoldChange(gameAction)), GameAction.Block);
                gameAction = GameAction.Block;
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
            Dictionary<ulong, GameAction> currentRoundActions = m_PlayerGameData.Select(
                playerData => new KeyValuePair<ulong, GameAction>(playerData.Key, playerData.Value.GetAction(m_AtMatch.Value - 1, RoundManager.Instance.m_CurrentRound.Value - 1))
            ).ToDictionary(x => x.Key, x => x.Value);
            RoundManager.Instance.DecideRound(currentRoundActions);
        }
        else
        {
            Debug.LogFormat("could not find player {0}, player will do nothing", playerID);
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void MatchDecidedClientRpc(bool hasPlayerWon, MatchResult resultType, RpcParams rpcParams)
    {
        Debug.LogFormat("has this player won? {0}", hasPlayerWon);
        OnMatchDecided?.Invoke(hasPlayerWon, resultType);
        OnDisableActionsToBePlayed?.Invoke();
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void GameFinishedClientRpc(RpcParams rpcParams)
    {
        OnGameFinished?.Invoke();
    }
}
