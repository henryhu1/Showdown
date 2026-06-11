using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HowToPlay : MonoBehaviour
{
    [SerializeField] private GameObject m_panel;
    private Button m_panelButton;

    private RectTransform m_panelRectTransform;
    private Vector3 m_panelOriginalPos;
    private Camera m_mainCamera;
    private float m_offScreenLeft;
    private Tween m_transition;

    private const float k_tweenDuration = 0.3f;

    private void Awake()
    {
        m_panelButton = m_panel.GetComponent<Button>();
    }

    private void Start()
    {
        m_mainCamera = Camera.main;
        m_panelRectTransform = m_panel.GetComponent<RectTransform>();
        m_offScreenLeft = m_mainCamera.ViewportToWorldPoint(new(-0.5f, 0.5f, m_mainCamera.nearClipPlane)).x;
        m_panelOriginalPos = m_panel.transform.position;
    }

    private void OnEnable()
    {
        m_panelButton.onClick.AddListener(() => { LobbyManager.Instance.OnCloseHowToPlay?.Invoke(); });

        LobbyManager.Instance.OnShowHowToPlay += ShowHowToPlay;
        LobbyManager.Instance.OnCloseHowToPlay += CloseHowToPlay;
    }

    private void OnDisable()
    {
        m_panelButton.onClick.RemoveAllListeners();

        LobbyManager.Instance.OnShowHowToPlay -= ShowHowToPlay;
        LobbyManager.Instance.OnCloseHowToPlay -= CloseHowToPlay;
    }

    private void ShowHowToPlay()
    {
        Show();
    }

    private void CloseHowToPlay()
    {
        Hide();
    }

    private void Show(bool isLobbyOwner = true)
    {
        if (m_panel.activeInHierarchy) return;
        if (m_transition != null) return;

        m_panel.SetActive(true);
        m_transition = m_panelRectTransform.DOMoveX(m_offScreenLeft, k_tweenDuration)
            .From()
            .SetEase(Ease.OutQuad)
            .OnComplete(() => m_transition = null);
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
