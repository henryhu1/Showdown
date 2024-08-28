using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_GAME_MODE = "GameType";
    public const string KEY_START_GAME = "Start";

    public static string k_DefaultLobbyName = "Lobby";
    private const float k_lobbyPollInterval = 1.1f;
    private const float k_lobbyHeartbeatInterval = 15f;

    [HideInInspector]
    public delegate void SearchForGameDelegateHandler();
    [HideInInspector]
    public event SearchForGameDelegateHandler OnSearchForGame;

    [HideInInspector]
    public delegate void CreatedLobbyDelegateHandler(string lobbyCode);
    [HideInInspector]
    public event CreatedLobbyDelegateHandler OnCreatedLobby;

    [HideInInspector]
    public delegate void PlayerJoinedLobbyDelegateHandler();
    [HideInInspector]
    public event PlayerJoinedLobbyDelegateHandler OnPlayerJoinedLobby;

    [HideInInspector]
    public delegate void QuickJoinLobbyDelegateHandler();
    [HideInInspector]
    public event QuickJoinLobbyDelegateHandler OnQuickJoinLobby;

    [HideInInspector]
    public delegate void CancelSearchForGameDelegateHandler();
    [HideInInspector]
    public event CancelSearchForGameDelegateHandler OnCancelSearchForGame;

    [HideInInspector]
    public delegate void CancelLobbyDelegateHandler();
    [HideInInspector]
    public event CancelLobbyDelegateHandler OnCancelLobby;

    [HideInInspector]
    public delegate void ToggleLobbyCodeInputDelegateHandler(bool isInputtingLobbyCode);
    [HideInInspector]
    public event ToggleLobbyCodeInputDelegateHandler OnToggleLobbyCodeInput;

    [HideInInspector]
    public delegate void LobbyWasDeletedDelegateHandler();
    [HideInInspector]
    public event LobbyWasDeletedDelegateHandler OnLobbyWasDeleted;

    public event EventHandler OnLeftLobby;

    public event EventHandler<EventArgs> OnGameStarted;

    [HideInInspector]
    public delegate void FailedToJoinLobbyByCodeDelegateHandler(string reason);
    [HideInInspector]
    public event FailedToJoinLobbyByCodeDelegateHandler OnFailedToJoinLobbyByCode;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler<LobbyEventArgs> OnLobbyGameTypeChanged;
    public class LobbyEventArgs : EventArgs
    {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    [HideInInspector]
    public delegate void GameFailedToStartDelegateHandler();
    [HideInInspector]
    public event GameFailedToStartDelegateHandler OnGameFailedToStart;

    private float m_lobbyHeartbeatTimer = 15f;
    private float m_lobbyPollTimer = 1.1f;
    private Lobby m_joinedLobby;
    private bool m_isInputtingLobbyCode = false;

    private void Awake()
    {
        if (Instance != this && Instance != null)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
    }

    private void Start()
    {
        m_joinedLobby = null;

        Authenticate("temp" + UnityEngine.Random.Range(0, 20));
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        //HandleLobbyPolling();
    }

    public async void Authenticate(string playerName)
    {
        try
        {
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(playerName);

            await UnityServices.InitializeAsync(initializationOptions);

            AuthenticationService.Instance.SignedIn += () => {
#if UNITY_EDITOR
                Debug.Log("Signed in! " + AuthenticationService.Instance.PlayerId);
#endif
            };

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.Log(e.Message);
#endif
        }
    }

    private async void HandleLobbyHeartbeat()
    {
        if (IsLobbyHost())
        {
            m_lobbyHeartbeatTimer -= Time.deltaTime;
            if (m_lobbyHeartbeatTimer < 0f)
            {
                m_lobbyHeartbeatTimer = k_lobbyHeartbeatInterval;

                await LobbyService.Instance.SendHeartbeatPingAsync(m_joinedLobby.Id);
            }
        }
    }

    private async void HandleLobbyPolling()
    {
        if (m_joinedLobby != null)
        {
            m_lobbyPollTimer -= Time.deltaTime;
            if (m_lobbyPollTimer < 0f)
            {
                m_lobbyPollTimer = k_lobbyPollInterval;

                m_joinedLobby = await LobbyService.Instance.GetLobbyAsync(m_joinedLobby.Id);

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = m_joinedLobby });

                if (!IsPlayerInLobby())
                {
                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = m_joinedLobby });

                    m_joinedLobby = null;
                }

                if (m_joinedLobby != null && m_joinedLobby.Data[KEY_START_GAME].Value != "0")
                {
                    if (!IsLobbyHost())
                    {
                        RelayManager.Instance.JoinRelay(m_joinedLobby.Data[KEY_START_GAME].Value);
                        SceneTransitionHandler.Instance.SetSceneState(SceneState.GameScene);
                        OnGameStarted?.Invoke(this, EventArgs.Empty);
                    }

                    m_joinedLobby = null;
                }
            }
        }
    }

    public bool IsLobbyHost()
    {
        return m_joinedLobby != null && m_joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private bool IsPlayerInLobby()
    {
        if (m_joinedLobby != null && m_joinedLobby.Players != null)
        {
            foreach (Player player in m_joinedLobby.Players)
            {
                if (player.Id == AuthenticationService.Instance.PlayerId)
                {
                    // This player is in this lobby
                    return true;
                }
            }
        }
        return false;
    }
    public void CancelSearchForGame()
    {
        OnCancelSearchForGame?.Invoke();
        if (m_joinedLobby == null) return;

        if (IsLobbyHost())
        {
            DeleteLobby();
        }
        else
        {
            LeaveLobby();
        }
    }


    public void CancelLobby()
    {
        OnCancelLobby?.Invoke();
        if (m_joinedLobby == null) return;

        if (IsLobbyHost())
        {
            DeleteLobby();
        }
        else
        {
            LeaveLobby();
        }
    }

    public void ToggleLobbyCodeInput()
    {
        m_isInputtingLobbyCode = !m_isInputtingLobbyCode;
        OnToggleLobbyCodeInput?.Invoke(m_isInputtingLobbyCode);
    }

    public void SearchForLobby()
    {
        try
        {
            OnSearchForGame?.Invoke();
            QuickJoinLobby();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    private Player GetPlayer()
    {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
                { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public) },
            });
    }

    public async void CreateLobby(string lobbyName, LobbyType lobbyType, bool auto = false)
    {
        Player player = GetPlayer();

        CreateLobbyOptions options = new()
        {
            Player = player,
            IsPrivate = lobbyType == LobbyType.Private,
            Data = new Dictionary<string, DataObject> {
                    { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "") },
                }
        };

        try
        {
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, GameManager.k_PlayersPerGame, options);

            m_joinedLobby = lobby;

            LobbyEventCallbacks callbacks = new();
            callbacks.PlayerJoined += Callbacks_PlayerJoined;
            callbacks.PlayerLeft += Callbacks_PlayerLeft;
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);

            if (!auto)
            {
                OnCreatedLobby?.Invoke(lobby.LobbyCode);
            }
            //OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });

#if UNITY_EDITOR
            Debug.Log("Created Lobby " + lobby.Name);
#endif
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.Log(e.Message);
#endif
        }
    }

    public async void JoinLobbyByCode(string joinCode)
    {
        try
        {
            Player player = GetPlayer();

            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(joinCode, new JoinLobbyByCodeOptions
            {
                Player = player
            });

            m_joinedLobby = lobby;

            LobbyEventCallbacks callbacks = new LobbyEventCallbacks();
            callbacks.LobbyDeleted += Callbacks_LobbyDeleted;
            callbacks.DataChanged += Callbacks_DataChanged;
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);

            Debug.Log("Joined lobby by code!");
            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        }
        catch (LobbyServiceException e)
        {
#if UNITY_EDITOR
            Debug.LogError(e.Message);
#endif
            OnFailedToJoinLobbyByCode?.Invoke("Unable to join lobby");
        }
    }

    public async void JoinLobby(Lobby lobby)
    {
        try
        {
            Player player = GetPlayer();

            m_joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions
            {
                Player = player
            });

            LobbyEventCallbacks callbacks = new();
            callbacks.LobbyDeleted += Callbacks_LobbyDeleted;
            callbacks.DataChanged += Callbacks_DataChanged;
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);

            Debug.Log("Joined lobby!");
            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        }
        catch (LobbyServiceException e)
        {
#if UNITY_EDITOR
            Debug.LogError(e.Message);
#endif
        }
    }

    public async void QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions options = new();

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            m_joinedLobby = lobby;

            LobbyEventCallbacks callbacks = new();
            callbacks.LobbyDeleted += Callbacks_LobbyDeleted;
            callbacks.DataChanged += Callbacks_DataChanged;
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);

            Debug.Log("Quick joined lobby!");
            OnQuickJoinLobby?.Invoke();
        }
        catch (LobbyServiceException e)
        {
#if UNITY_EDITOR
            Debug.Log("Could not quick join, creating new lobby");
#endif
            CreateLobby("automatic", LobbyType.Public, true);
        }
    }

    private void Callbacks_PlayerJoined(List<LobbyPlayerJoined> playersJoined)
    {
        //Debug.Log($"Player joined! Now have {m_joinedLobby.Players.Count} players in lobby");
        //foreach (LobbyPlayerJoined p in playersJoined)
        //{
        //    Debug.Log($"joined {p.Player.Id}");
        //}
        //foreach (Player p in m_joinedLobby.Players) {
        //    Debug.Log($"in lobby {p.Id}");
        //}
        OnPlayerJoinedLobby?.Invoke();
        SceneTransitionHandler.Instance.SetSceneState(SceneState.GameScene);
        StartGame();
    }

    private void Callbacks_PlayerLeft(List<int> leaving)
    {
        foreach (int i in leaving)
        {
            Debug.Log($"player at index {i} left lobby");
        }
    }

    private void Callbacks_LobbyDeleted()
    {
        m_joinedLobby = null;
        OnLobbyWasDeleted?.Invoke();
    }

    private void Callbacks_DataChanged(Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> obj)
    {
        Debug.Log("Lobby data changed!");
        if (!string.IsNullOrEmpty(obj[KEY_START_GAME].Value.Value))
        {
            RelayManager.Instance.JoinRelay(obj[KEY_START_GAME].Value.Value);
            SceneTransitionHandler.Instance.SetSceneState(SceneState.GameScene);
            OnGameStarted?.Invoke(this, EventArgs.Empty);
            m_joinedLobby = null;
        }
    }

    public async void LeaveLobby()
    {
        if (m_joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(m_joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                m_joinedLobby = null;

                Debug.Log("Left lobby");
                OnLeftLobby?.Invoke(this, EventArgs.Empty);
            }
            catch (LobbyServiceException e)
            {
#if UNITY_EDITOR
                Debug.LogError(e.Message);
#endif
            }
        }
    }

    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(m_joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
#if UNITY_EDITOR
                Debug.LogError(e.Message);
#endif
            }
        }
    }

    public async void DeleteLobby()
    {
        if (IsLobbyHost())
        {
            try
            {
                await Lobbies.Instance.DeleteLobbyAsync(m_joinedLobby.Id);
                m_joinedLobby = null;
                Debug.Log($"Deleted lobby");
            }
            catch (LobbyServiceException e)
            {
#if UNITY_EDITOR
                Debug.Log($"Could not delete lobby, {e.Message}");
#endif
            }
        }
    }

    public async void StartGame()
    {
        if (IsLobbyHost())
        {
            try
            {
                OnGameStarted?.Invoke(this, EventArgs.Empty);

                string relayCode = await RelayManager.Instance.CreateRelay();

                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(m_joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                        {
                            { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                        }
                });

                m_joinedLobby = lobby;
            }
            catch (LobbyServiceException e)
            {
#if UNITY_EDITOR
                Debug.Log($"Could not start relay, {e.Message}");
#endif
                OnGameFailedToStart?.Invoke();
            }
        }
    }
}
