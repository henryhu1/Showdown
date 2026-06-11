using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyDisplay : MonoBehaviour
{
    [SerializeField] private GameObject m_panel;
    [SerializeField] private TextMeshProUGUI m_lobbyCodeText;
    [SerializeField] private TextMeshProUGUI m_lobbyStatusText;
    [SerializeField] private TextMeshProUGUI m_startingGameText;
    [SerializeField] private Button m_startButton;
    [SerializeField] private Button m_cancelButton;

    private RectTransform m_panelRectTransform;
    private Vector3 m_panelOriginalPos;
    private Camera m_mainCamera;
    private float m_offScreenLeft;
    private Tween m_transition;

    private const float k_tweenDuration = 0.3f;
    private const string k_waitingForOpponentText = "Waiting for\nopponent...";
    private const string k_playerJoinedText = "Opponent\njoined!";
    private const string k_lobbyJoinedText = "Lobby joined!";
    private const string k_startingGameText = "Starting game...";

    private void Awake()
    {
        m_lobbyCodeText.enabled = false;
        m_lobbyStatusText.text = k_waitingForOpponentText;
    }

    private void OnEnable()
    {
        LobbyManager.Instance.OnCreatedLobby += LobbyManager_CreatedLobby;
        LobbyManager.Instance.OnPlayerJoinedLobby += LobbyManager_PlayerJoinedLobby;
        LobbyManager.Instance.OnPlayerLeftLobby += LobbyManager_PlayerLeftLobby;
        LobbyManager.Instance.OnCancelLobby += LobbyManager_CancelLobby;
        LobbyManager.Instance.OnJoinedLobby += LobbyManager_JoinedLobby;
        LobbyManager.Instance.OnLobbyWasDeleted += LobbyManager_LobbyWasDeleted;
        LobbyManager.Instance.OnGameStarted += LobbyManager_GameStarted;
    }

    private void OnDisable()
    {
        LobbyManager.Instance.OnCreatedLobby -= LobbyManager_CreatedLobby;
        LobbyManager.Instance.OnPlayerJoinedLobby -= LobbyManager_PlayerJoinedLobby;
        LobbyManager.Instance.OnPlayerLeftLobby -= LobbyManager_PlayerLeftLobby;
        LobbyManager.Instance.OnCancelLobby -= LobbyManager_CancelLobby;
        LobbyManager.Instance.OnJoinedLobby -= LobbyManager_JoinedLobby;
        LobbyManager.Instance.OnLobbyWasDeleted -= LobbyManager_LobbyWasDeleted;
        LobbyManager.Instance.OnGameStarted -= LobbyManager_GameStarted;
    }

    private void Start()
    {
        m_mainCamera = Camera.main;
        m_panelRectTransform = m_panel.GetComponent<RectTransform>();
        m_offScreenLeft = m_mainCamera.ViewportToWorldPoint(new(-0.5f, 0.5f, m_mainCamera.nearClipPlane)).x;
        m_panelOriginalPos = m_panel.transform.position;

        m_cancelButton.onClick.AddListener(() =>
        {
            if (m_transition != null) return;
            LobbyManager.Instance.CancelLobby();
        });

        m_startButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.StartGame();
        });
    }

    private void LobbyManager_CreatedLobby(string lobbyCode)
    {
        m_lobbyCodeText.text = lobbyCode;
        Show(true);
    }

    private void LobbyManager_PlayerJoinedLobby()
    {
        m_lobbyStatusText.text = k_playerJoinedText;
        m_startButton.gameObject.SetActive(true);
    }

    private void LobbyManager_PlayerLeftLobby()
    {
        Hide();
    }

    private void LobbyManager_CancelLobby()
    {
        Hide();
    }

    private void LobbyManager_JoinedLobby(object sender, LobbyManager.LobbyEventArgs e)
    {
        Show(false);
    }

    private void LobbyManager_LobbyWasDeleted()
    {
        Hide();
    }

    private void LobbyManager_GameStarted(object sender, System.EventArgs e)
    {
        if (!m_panel.activeInHierarchy) return;

        m_startButton.gameObject.SetActive(false);
        m_startingGameText.text = k_startingGameText;
        m_startingGameText.enabled = true;
    }

    private void Show(bool isLobbyOwner = true)
    {
        if (m_panel.activeInHierarchy) return;
        if (m_transition != null) return;

        m_panel.SetActive(true);
        m_startButton.gameObject.SetActive(false);
        m_transition = m_panelRectTransform.DOMoveX(m_offScreenLeft, k_tweenDuration)
            .From()
            .SetEase(Ease.OutQuad)
            .OnComplete(() => m_transition = null);

        m_lobbyStatusText.enabled = true;
        if (!isLobbyOwner)
        {
            m_lobbyStatusText.text = k_lobbyJoinedText;
        }
        m_lobbyCodeText.enabled = isLobbyOwner;
    }

    private void Hide()
    {
        if (!m_panel.activeInHierarchy) return;
        if (m_transition != null) return;

        m_transition = m_panelRectTransform.DOMoveX(m_offScreenLeft, k_tweenDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                m_panel.SetActive(false);
                m_panelRectTransform.position = m_panelOriginalPos;
                m_transition = null;
            });
    }
}
