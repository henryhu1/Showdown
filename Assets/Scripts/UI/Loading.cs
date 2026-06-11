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
        LobbyManager.Instance.OnSearchForGameFailed += LobbyManager_SearchForGameFailed;
        LobbyManager.Instance.OnCancelSearchForGame += LobbyManager_CancelSearchForGame;
        LobbyManager.Instance.OnPlayerJoinedLobby += LobbyManager_PlayerJoinedLobby;
        //LobbyManager.Instance.OnGameStarted += LobbyManager_GameStarted;
        LobbyManager.Instance.OnQuickJoinLobby += LobbyManager_QuickJoinLobby;
        LobbyManager.Instance.OnPlayerLeftLobby += LobbyManager_PlayerLeftLobby;
    }

    private void OnDisable()
    {
        LobbyManager.Instance.OnSearchForGame -= LobbyManager_SearchForGame;
        LobbyManager.Instance.OnSearchForGameFailed -= LobbyManager_SearchForGameFailed;
        LobbyManager.Instance.OnCancelSearchForGame -= LobbyManager_CancelSearchForGame;
        LobbyManager.Instance.OnPlayerJoinedLobby -= LobbyManager_PlayerJoinedLobby;
        //LobbyManager.Instance.OnGameStarted -= LobbyManager_GameStarted;
        LobbyManager.Instance.OnQuickJoinLobby -= LobbyManager_QuickJoinLobby;
        LobbyManager.Instance.OnPlayerLeftLobby -= LobbyManager_PlayerLeftLobby;
    }

    private void Start()
    {
        m_panelImage = m_panel.GetComponent<Image>();
    }

    private void LobbyManager_SearchForGame()
    {
        Show();
        m_searchingForGameText.text = k_searchingForGameText;
    }

    private void LobbyManager_SearchForGameFailed()
    {
        Hide();
    }

    private void LobbyManager_CancelSearchForGame()
    {
        Hide();
    }

    private void LobbyManager_PlayerJoinedLobby()
    {
        m_searchingForGameText.text = $"{k_opponentFoundText}\n{k_startingGameText}";

        m_cancelButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.CancelLobby();
        });
    }

    private void LobbyManager_PlayerLeftLobby()
    {
        Hide();
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
        m_searchingForGameText.text = $"{k_opponentFoundText}\n{k_startingGameText}";

        m_cancelButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.CancelLobby();
        });
    }

    private void Show()
    {
        m_panel.SetActive(true);
        m_cancelButton.gameObject.SetActive(true);

        m_cancelButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.CancelSearchForGame();
        });
    }

    private void Hide()
    {
        m_panel.SetActive(false);
    }
}
