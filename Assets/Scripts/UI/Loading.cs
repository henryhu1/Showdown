using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Loading : MonoBehaviour
{
    [SerializeField] private GameObject m_panel;
    private Image m_panelImage;
    [SerializeField] private TextMeshProUGUI m_searchingForGameText;
    [SerializeField] private Button m_cancelButton;

    private const string k_searchingForGameText = "Searching for game...";
    private const string k_opponentFoundText = "Opponent found!";
    private const string k_startingGameText = "Starting game...";
    private const float k_fadeTime = 0.3f;
    private const float k_panelAlpha = 0.39f;
    private const float k_panelSolidAlpha = 0.9f;

    private void OnEnable()
    {
        LobbyManager.Instance.OnSearchForGame += LobbyManager_SearchForGame;
        LobbyManager.Instance.OnCancelSearchForGame += LobbyManager_CancelSearchForGame;
        LobbyManager.Instance.OnPlayerJoinedLobby += LobbyManager_PlayerJoinedLobby;
        //LobbyManager.Instance.OnGameStarted += LobbyManager_GameStarted;
        LobbyManager.Instance.OnQuickJoinLobby += LobbyManager_QuickJoinLobby;
    }

    private void OnDisable()
    {
        LobbyManager.Instance.OnSearchForGame -= LobbyManager_SearchForGame;
        LobbyManager.Instance.OnCancelSearchForGame -= LobbyManager_CancelSearchForGame;
        LobbyManager.Instance.OnPlayerJoinedLobby -= LobbyManager_PlayerJoinedLobby;
        //LobbyManager.Instance.OnGameStarted -= LobbyManager_GameStarted;
        LobbyManager.Instance.OnQuickJoinLobby -= LobbyManager_QuickJoinLobby;
    }

    private void Start()
    {
        m_panelImage = m_panel.GetComponent<Image>();

        m_cancelButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.CancelSearchForGame();
        });
    }

    private void LobbyManager_SearchForGame()
    {
        m_panel.SetActive(true);
        m_cancelButton.gameObject.SetActive(true);
        m_searchingForGameText.text = k_searchingForGameText;
        m_panelImage.color = new(m_panelImage.color.r, m_panelImage.color.r, m_panelImage.color.r, k_panelAlpha);
    }

    private void LobbyManager_CancelSearchForGame()
    {
        m_panel.SetActive(false);
        m_panelImage.color = new(m_panelImage.color.r, m_panelImage.color.r, m_panelImage.color.r, k_panelAlpha);
    }

    private void LobbyManager_PlayerJoinedLobby()
    {
        m_cancelButton.gameObject.SetActive(false);
        m_searchingForGameText.text = $"{k_opponentFoundText}\n{k_startingGameText}";
    }

    //private void LobbyManager_GameStarted(object sender, System.EventArgs e)
    //{
    //    m_cancelButton.gameObject.SetActive(false);
    //    m_searchingForGameText.text = $"{k_opponentFoundText}\n{k_startingGameText}";
    //    m_panelImage.DOFade(k_panelSolidAlpha, k_fadeTime).SetEase(Ease.InSine);
    //}

    private void LobbyManager_QuickJoinLobby()
    {
        m_cancelButton.gameObject.SetActive(false);
        m_searchingForGameText.text = $"{k_opponentFoundText}\n{k_startingGameText}";
    }
}
