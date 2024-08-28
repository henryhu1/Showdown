using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCodeInput : MonoBehaviour
{
    [SerializeField] private GameObject m_panel;
    [SerializeField] private Button m_submitButton;
    [SerializeField] private Button m_cancelButton;
    [SerializeField] private TMP_InputField m_input;
    [SerializeField] private TextMeshProUGUI m_errorTextUI;

    private const string k_errorMessage = "Lobby does not exist";
    private const float k_moveDuration = 0.3f;
    private const float k_fadeDuration = 0.3f;
    private const float k_fadePause = 1.0f;

    private RectTransform m_panelRectTransform;
    private Camera m_mainCamera;
    private float m_offScreenLeft;
    private string m_lobbyCode;
    private Vector3 m_panelOriginalPos;
    private Sequence m_errorTextFading;
    private Tween m_transition;

    private void Start()
    {
        m_mainCamera = Camera.main;
        m_panelRectTransform = m_panel.GetComponent<RectTransform>();
        m_offScreenLeft = m_mainCamera.ViewportToWorldPoint(new(-0.5f, 0.5f, m_mainCamera.nearClipPlane)).x - m_panelRectTransform.sizeDelta.x * 2;
        m_panelOriginalPos = m_panel.transform.position;

        m_submitButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.JoinLobbyByCode(m_lobbyCode);
        });

        m_cancelButton.onClick.AddListener(() =>
        {
            if (m_transition != null) return;
            LobbyManager.Instance.ToggleLobbyCodeInput();
        });

        m_input.onValueChanged.AddListener((string lobbyCode) => m_lobbyCode = lobbyCode);
    }

    private void OnEnable()
    {
        LobbyManager.Instance.OnToggleLobbyCodeInput += LobbyManager_ToggleLobbyCodeInput;
        LobbyManager.Instance.OnJoinedLobby += LobbyManager_JoinedLobby;
        LobbyManager.Instance.OnFailedToJoinLobbyByCode += LobbyManager_FailedToJoinLobbyByCode;
    }

    private void OnDisable()
    {
        LobbyManager.Instance.OnToggleLobbyCodeInput -= LobbyManager_ToggleLobbyCodeInput;
        LobbyManager.Instance.OnJoinedLobby -= LobbyManager_JoinedLobby;
        LobbyManager.Instance.OnFailedToJoinLobbyByCode -= LobbyManager_FailedToJoinLobbyByCode;
    }

    private void LobbyManager_ToggleLobbyCodeInput(bool isInputtingLobbyCode)
    {
        if (isInputtingLobbyCode)
        {
            Show();
        }
        else
        {
            Hide();
        }
        m_errorTextUI.text = "";
    }

    private void LobbyManager_JoinedLobby(object sender, LobbyManager.LobbyEventArgs e)
    {
        Hide();
    }

    private void LobbyManager_FailedToJoinLobbyByCode(string reason)
    {
        m_errorTextUI.text = k_errorMessage;
        m_errorTextUI.color = new(m_errorTextUI.color.r, m_errorTextUI.color.g, m_errorTextUI.color.b, 0);
        m_errorTextUI.enabled = true;
        Tween fadeIn = m_errorTextUI.DOFade(1, k_fadeDuration).SetEase(Ease.InQuad).Pause();
        Tween fadeOut = m_errorTextUI.DOFade(0, k_fadeDuration).SetEase(Ease.InQuad).Pause();
        m_errorTextFading = DOTween.Sequence()
            .Append(fadeIn)
            .AppendInterval(k_fadePause)
            .Append(fadeOut)
            .OnComplete(() => m_errorTextUI.enabled = false);
    }

    public void Show()
    {
        if (m_panel.activeInHierarchy) return;

        m_errorTextUI.enabled = false;
        m_panel.SetActive(true);
        m_transition = m_panel.transform.DOMoveX(m_offScreenLeft, k_moveDuration)
            .SetEase(Ease.OutQuad)
            .From()
            .OnComplete(() => m_transition = null);
    }

    public void Hide()
    {
        if (!m_panel.activeInHierarchy) return;

        if (m_errorTextFading != null)
        {
            m_errorTextFading.Kill();
            m_errorTextFading = null;
        }
        m_errorTextUI.enabled = false;

        m_transition = m_panel.transform.DOMoveX(m_offScreenLeft, k_moveDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                m_panel.SetActive(false);
                m_panel.transform.position = m_panelOriginalPos;
                m_transition = null;
            });
    }
}
