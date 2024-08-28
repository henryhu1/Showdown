using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MainUI : MonoBehaviour
{
    [SerializeField] private GameObject m_buttonList;
    [SerializeField] private Button m_findGameButton;
    [SerializeField] private Button m_joinGameButton;
    [SerializeField] private Button m_createGameButton;
    [SerializeField] private Button m_howToPlayButton;

    [SerializeField] private LobbyCodeInput m_lobbyCodeInput;

    private RectTransform m_buttonListRectTransform;
    private Vector3 m_buttonListOriginalPos;
    private Camera m_mainCamera;
    private float m_offScreenRight;
    private Tween m_transition;

    private const float k_tweenDuration = 0.3f;

    private void Start()
    {
        m_mainCamera = Camera.main;
        m_buttonListRectTransform = m_buttonList.GetComponent<RectTransform>();
        m_offScreenRight = m_mainCamera.ViewportToWorldPoint(new(1.5f, 0.5f, m_mainCamera.nearClipPlane)).x;
        Debug.Log($"off screen right: {m_offScreenRight}, {m_buttonListRectTransform.rect.width}");
        m_buttonListOriginalPos = m_buttonList.transform.position;

        m_findGameButton.onClick.AddListener(() =>
        {
            if (m_transition != null) return;
            LobbyManager.Instance.SearchForLobby();
        });

        m_joinGameButton.onClick.AddListener(() =>
        {
            if (m_transition != null) return;
            LobbyManager.Instance.ToggleLobbyCodeInput();
        });

        m_createGameButton.onClick.AddListener(() =>
        {
            if (m_transition != null) return;
            LobbyManager.Instance.CreateLobby("temp", LobbyType.Private);
        });

        m_howToPlayButton.onClick.AddListener(() =>
        {

        });
    }

    private void OnEnable()
    {
        LobbyManager.Instance.OnSearchForGame += LobbyManager_SearchForGame;
        LobbyManager.Instance.OnCancelSearchForGame += LobbyManager_CancelSearchForGame;
        LobbyManager.Instance.OnCreatedLobby += LobbyManager_CreatedLobby;
        LobbyManager.Instance.OnCancelLobby += LobbyManager_CancelLobby;
        LobbyManager.Instance.OnToggleLobbyCodeInput += LobbyManager_ToggleLobbyCodeInput;
        LobbyManager.Instance.OnLobbyWasDeleted += LobbyManager_LobbyWasDeleted;
    }

    private void OnDisable()
    {
        LobbyManager.Instance.OnSearchForGame -= LobbyManager_SearchForGame;
        LobbyManager.Instance.OnCancelSearchForGame -= LobbyManager_CancelSearchForGame;
        LobbyManager.Instance.OnCreatedLobby -= LobbyManager_CreatedLobby;
        LobbyManager.Instance.OnCancelLobby -= LobbyManager_CancelLobby;
        LobbyManager.Instance.OnToggleLobbyCodeInput -= LobbyManager_ToggleLobbyCodeInput;
        LobbyManager.Instance.OnLobbyWasDeleted -= LobbyManager_LobbyWasDeleted;
    }

    private void LobbyManager_SearchForGame()
    {
        m_buttonList.SetActive(false);
    }

    private void LobbyManager_CancelSearchForGame()
    {
        m_buttonList.SetActive(true);
    }

    private void LobbyManager_CreatedLobby(string _)
    {
        Hide();
    }

    private void LobbyManager_CancelLobby()
    {
        Show();
    }

    private void LobbyManager_ToggleLobbyCodeInput(bool isInputtingLobbyCode)
    {
        if (isInputtingLobbyCode)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    private void LobbyManager_LobbyWasDeleted()
    {
        Show();
    }

    private void Show()
    {
        if (m_buttonList.activeInHierarchy) return;
        if (m_transition != null) return;

        m_buttonList.SetActive(true);
        m_transition = m_buttonListRectTransform.DOMoveX(m_offScreenRight, k_tweenDuration)
            .From()
            .SetEase(Ease.OutQuad)
            .OnComplete(() => m_transition = null);
    }

    private void Hide()
    {
        if (!m_buttonList.activeInHierarchy) return;
        if (m_transition != null) return;

        m_transition = m_buttonListRectTransform.DOMoveX(m_offScreenRight, k_tweenDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                m_buttonList.SetActive(false);
                m_buttonListRectTransform.position = m_buttonListOriginalPos;
                m_transition = null;
            });
    }
}
