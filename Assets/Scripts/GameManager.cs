using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using GameActions;
using UnityEngine.SceneManagement;
using DG.Tweening;
using GooglePlayGames;
using System;

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

    public const float k_TimeoutThreshold = 10;

    private Dictionary<ulong, PlayerGameData> m_PlayerGameData;
    private Dictionary<ulong, float> m_PlayerHeartbeat;
    private float m_ServerSentHeartbeat;

    public NetworkVariable<int> m_AtMatch = new(0);
    public NetworkVariable<int> m_GoldTotalToWin = new(k_StartingGoldTotalToWin);
    public NetworkVariable<bool> m_IsSuddenDeath = new(false);

    public Coroutine m_TickCoroutine;
    public Coroutine m_HeartbeatCoroutine;

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
    public delegate void GameFinishedDelegateHandler(List<MatchData> allMatchData);
    [HideInInspector]
    public event GameFinishedDelegateHandler OnGameFinished;

    [HideInInspector]
    public delegate void OpponentLeftAfterGameDelegateHandler();
    [HideInInspector]
    public event OpponentLeftAfterGameDelegateHandler OnOpponentLeftAfterGame;

    [HideInInspector]
    public delegate void OpponentRequestedRematchDelegateHandler();
    [HideInInspector]
    public event OpponentRequestedRematchDelegateHandler OnOpponentRequestedRematch;

    [HideInInspector]
    public delegate void ResetGameDelegateHandler();
    [HideInInspector]
    public event ResetGameDelegateHandler OnResetGame;

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
        DOTween.SetTweensCapacity(500, 312);
    }

    private void Start()
    {
        m_PlayerGameData = new Dictionary<ulong, PlayerGameData>();
        m_PlayerHeartbeat = new Dictionary<ulong, float>();
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

    public ulong GetPlayerId()
    {
        return NetworkManager.Singleton.LocalClientId;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        OnDisableActionsToBePlayed?.Invoke();

        RegisterNetworkVariableCallbacks();

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

        if (IsServer)
        {
            m_PlayerGameData.Clear();
            m_PlayerHeartbeat.Clear();
            m_AtMatch.Value = 0;

            SceneTransitionHandler.Instance.RegisterCallbacks();
            SceneTransitionHandler.Instance.OnAllClientsLoadedScene += SceneTransitionHandler_AllClientsLoadedScene;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        StopGameClock();

        UnregisterNetworkVariableCallbacks();

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;

        if (IsServer)
        {
            m_PlayerGameData.Clear();
            m_PlayerHeartbeat.Clear();
            SceneTransitionHandler.Instance.UnregisterCallbacks();
            SceneTransitionHandler.Instance.OnAllClientsLoadedScene -= SceneTransitionHandler_AllClientsLoadedScene;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void SceneTransitionHandler_AllClientsLoadedScene(SceneState inScene)
    {
        bool isGameScene = inScene == SceneState.GameScene;
        Debug.Log($"all clients loaded scene, is in game scene? {isGameScene}");
        if (isGameScene)
        {
            StartMatch();
        }
    }

    private void RegisterNetworkVariableCallbacks()
    {
        if (SceneManager.GetActiveScene().name == SceneState.GameScene.ToString())
        {
            GoldCount.Instance.RegisterNetworkCallbacks();
            ActionButtonsGroup.Instance.RegisterNetworkCallbacks();
        }
    }

    private void UnregisterNetworkVariableCallbacks()
    {
        if (SceneManager.GetActiveScene().name == SceneState.GameScene.ToString())
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
        StopGameClock();
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
        m_PlayerHeartbeat.Add(clientId, Time.time);

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
            m_PlayerHeartbeat[playerData.Key] = Time.time;
            Debug.Log("new match!!");
            SetupPlayerActionsGivenGoldClientRpc(playerData.Value.GoldTotal, RpcTarget.Single(playerData.Key, RpcTargetUse.Temp));
            // TODO: clean up logic for resetting opponent tower sprite
            OpponentGoldStateClientRpc(GoldState.Low, RpcTarget.Single(playerData.Key, RpcTargetUse.Temp));
        }

        m_GoldTotalToWin.Value = k_StartingGoldTotalToWin;
        m_IsSuddenDeath.Value = false;

        OnStartMatchForServer?.Invoke();

        StartGameClockRpc();
        StartHeartbeatsRpc();

        m_AtMatch.Value++;
    }

    private void FinishGame(ulong winner, ulong? loser)
    {
        List<MatchData> allMatchData = new();
        for (int i = 0; i < m_AtMatch.Value; i++)
        {
            if (!loser.HasValue)
            {
                allMatchData.Add(new(i + 1, winner, Array.Empty<GameAction>(), 0, Array.Empty<GameAction>()));
            }
            else
            {
                allMatchData.Add(new());
            }
        }

        foreach (var playerData in m_PlayerGameData)
        {
            int recordingMatch = 1;
            foreach (List<GameAction> actionsByPlayer in playerData.Value.TakenActions())
            {
                allMatchData[recordingMatch - 1].SetMatchNumber(recordingMatch);
                if (playerData.Value.HasWonMatch(recordingMatch))
                {
                    Debug.Log($"player {playerData.Key} has won match {recordingMatch}");
                    allMatchData[recordingMatch - 1].SetWinner(playerData.Key, playerData.Value.TakenActionsInMatch(recordingMatch - 1));
                }
                else
                {
                    Debug.Log($"player {playerData.Key} has lost match {recordingMatch}");
                    allMatchData[recordingMatch - 1].SetLoser(playerData.Key, playerData.Value.TakenActionsInMatch(recordingMatch - 1));
                }
                recordingMatch++;
            }
        }
        GameFinishedWinnerClientRpc(allMatchData.ToArray(), RpcTarget.Single(winner, RpcTargetUse.Temp));

        if (loser.HasValue)
        {
            GameFinishedClientRpc(allMatchData.ToArray(), RpcTarget.Single(loser.Value, RpcTargetUse.Temp));
        }
    }

    public void ExitGame()
    {
        OpponentLeftAfterMatchClientRpc();
        NetworkManager.Singleton.Shutdown();
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

    private IEnumerator StartHeartbeat()
    {
        float time = 0;
        while (true)
        {
            time += Time.deltaTime;
            if (time > 2)
            {
                SendHeartbeatServerRpc();
                time = 0;
            }

            if (Time.time - m_ServerSentHeartbeat > k_TimeoutThreshold)
            {
                MatchDecidedClientRpc(true, MatchResult.AutomaticWin, RpcTarget.Single(GetPlayerId(), RpcTargetUse.Temp));
                FinishGame(GetPlayerId(), null);
                break;
            }

            yield return null;
        }
    }

    private IEnumerator StartRoundCountdown()
    {
        float time = 0;
        int ticksPlayed = -k_TicksPerRound;
        OnDisableActionsToBePlayed?.Invoke();
        while (ticksPlayed < 0)
        {
            time += Time.deltaTime;
            OnTickTimePasses?.Invoke(time, ticksPlayed); // TODO: should each time update be invoking an event?
            if (time >= k_TimePerTick)
            {
                time = 0;
                ticksPlayed++;
                if (ticksPlayed < 0)
                {
                    OnMatchCountdown?.Invoke(-ticksPlayed);
                    SoundFXManager.Instance.PlayTickSound(100);
                }
                else if (ticksPlayed == 0)
                {
                    SoundFXManager.Instance.PlayEndTickSound(100);
                    OnEnableActionsToBePlayed?.Invoke();
                }
            }
            yield return null;
        }
        m_TickCoroutine = StartCoroutine(TickCount());
    }

    private IEnumerator TickCount()
    {
        float time = 0;
        int ticksPlayed = 0;
        while (ticksPlayed < k_TicksPerRound)
        {
            time += Time.deltaTime;

            OnTickTimePasses?.Invoke(time, ticksPlayed); // TODO: should each time update be invoking an event?
            // Other solution: have GameTickVisualizer run its own coroutine, but these ticks and the visualizer may be out of sync?

            if (time >= k_TimePerTick)
            {
                time = 0;
                ticksPlayed++;

                bool isEndTick = ticksPlayed == k_TicksPerRound;
                bool isTickBeforeActionSubmit = ticksPlayed + 1 == k_TicksPerRound;

                if (isEndTick)
                {
                    SoundFXManager.Instance.PlayEndTickSound(100);
                }
                else
                {
                    SoundFXManager.Instance.PlayTickSound(100);
                }

                //if (ticksPlayed < 0)
                //{
                //    OnMatchCountdown?.Invoke(-ticksPlayed);
                //}
                //else if (ticksPlayed == 0)
                //{
                //    OnEnableActionsToBePlayed?.Invoke();
                //}
                // TODO: consolidate ActionManager to subscribe to events instead of directly calling from GameManager
                //else if (ticksPlayed > 0 && isEndTick)
                if (isEndTick)
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
        else
        {
            ResetSuddenDeath();
        }
    }

    private void TriggerSuddenDeath()
    {
        m_IsSuddenDeath.Value = true;
        m_GoldTotalToWin.Value++;
    }

    private void ResetSuddenDeath()
    {
        m_IsSuddenDeath.Value = false;
        m_GoldTotalToWin.Value = Mathf.Max(k_StartingGoldTotalToWin, m_GoldTotalToWin.Value - 1);
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

        m_PlayerGameData[winner].WonMatch(m_AtMatch.Value);
        if (resultType == MatchResult.AutomaticWin || m_PlayerGameData[winner].NumberOfMatchesWon == FirstTo())
        {
            Debug.Log($"after match {m_AtMatch.Value}, player {winner} has reached {FirstTo()} matches won, stopping game");
            FinishGame(winner, loser);
        }
        else
        {
            Debug.Log($"after match {m_AtMatch.Value}, player {winner} now has {m_PlayerGameData[winner].NumberOfMatchesWon} matches won, moving on to nect round");
            StartMatch();
        }
    }

    private void ResetGame()
    {
        if (!IsServer) return;

        m_AtMatch.Value = 0;
        foreach (var kvp in m_PlayerGameData)
        {
            kvp.Value.ResetData();
            ResetGameClientRpc();
        }
    }

    private void StopGameClock()
    {
        if (m_TickCoroutine != null)
        {
            StopCoroutine(m_TickCoroutine);
            m_TickCoroutine = null;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void StartGameClockRpc()
    {
        StopGameClock();
        m_TickCoroutine = StartCoroutine(StartRoundCountdown());
    }

    [Rpc(SendTo.Everyone)]
    public void StartHeartbeatsRpc()
    {
        m_ServerSentHeartbeat = Time.time;
        m_HeartbeatCoroutine = StartCoroutine(StartHeartbeat());
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void RespondHeartbeatClientRpc(RpcParams rpcParams)
    {
        m_ServerSentHeartbeat = Time.time;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendHeartbeatServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong sender = serverRpcParams.Receive.SenderClientId;
        m_PlayerHeartbeat[sender] = Time.time;

        RespondHeartbeatClientRpc(RpcTarget.Single(sender, RpcTargetUse.Temp));

        if (m_TickCoroutine == null) return;

        foreach (var heartbeat in m_PlayerHeartbeat)
        {
            if (Time.time - heartbeat.Value > k_TimeoutThreshold)
            {
                MatchDecided(m_PlayerHeartbeat.Keys.First(id => id != heartbeat.Key), heartbeat.Key, MatchResult.AutomaticWin);
            }
        }
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
        PlayGamesPlatform.Instance.UnlockAchievement(ActionLogic.GetActionAchievement(playerAction), (bool success) =>
        {

        });
        OnActionsDone?.Invoke(playerAction, opponentAction);
        m_TickCoroutine = StartCoroutine(TickCount());
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
        StopGameClock();
        StopCoroutine(m_HeartbeatCoroutine);
        Debug.LogFormat("has this player won? {0}", hasPlayerWon);
        if (hasPlayerWon)
        {
            PlayGamesPlatform.Instance.UnlockAchievement(GPGSIds.achievement_are_ya_winning, (bool success) => {
                // handle success or failure
            });
        }
        OnMatchDecided?.Invoke(hasPlayerWon, resultType);
        OnDisableActionsToBePlayed?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRematchServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong wantsRematch = serverRpcParams.Receive.SenderClientId;
        if (m_PlayerGameData[wantsRematch].RequestedRematch) return;
        m_PlayerGameData[wantsRematch].SetRequestedRematch(true);
        foreach (var kvp in m_PlayerGameData)
        {
            if (kvp.Key != wantsRematch)
            {
                if (kvp.Value.RequestedRematch)
                {
                    ResetGame();
                    StartMatch();
                    return;
                }
                else
                {
                    AskForRematchClientRpc(RpcTarget.Single(kvp.Key, RpcTargetUse.Temp));
                }
            }
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void AskForRematchClientRpc(RpcParams rpcParams)
    {
        OnOpponentRequestedRematch?.Invoke();
    }

    [Rpc(SendTo.Everyone)]
    public void OpponentLeftAfterMatchClientRpc()
    {
        OnOpponentLeftAfterGame?.Invoke();
    }

    [Rpc(SendTo.Everyone)]
    public void ResetGameClientRpc()
    {
        OnResetGame?.Invoke();
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void GameFinishedClientRpc(MatchData[] matchData, RpcParams rpcParams)
    {
        // if (matchData == null || matchData.Length == 0) return;

        StopGameClock();
        OnGameFinished?.Invoke(matchData.ToList());
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void GameFinishedWinnerClientRpc(MatchData[] matchData, RpcParams rpcParams)
    {
        // if (matchData == null || matchData.Length == 0) return;
        Debug.Log("Game finished, you are the winner!");

        StopGameClock();
        OnGameFinished?.Invoke(matchData.ToList());
        PlayGamesPlatform.Instance.UnlockAchievement(GPGSIds.achievement_youre_the_best_around, (bool success) => {
            // handle success or failure
        });
    }
}
