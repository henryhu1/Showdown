using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OverGameDisplay : MonoBehaviour
{
    public static OverGameDisplay Instance { get; private set; }

    [SerializeField] private GameObject m_panel;
    private Image m_panelImage;

    [SerializeField] private GameObject m_afterGame;
    [SerializeField] private GameObject m_afterGameContent;
    [SerializeField] private RoundLog m_roundLogPrefab;
    [SerializeField] private MatchHeading m_matchHeadingPrefab;
    [SerializeField] private Button m_rematchButton;
    [SerializeField] private TextMeshProUGUI m_rematchButtonText;
    [SerializeField] private Button m_leaveButton;
    [SerializeField] private TextMeshProUGUI m_helpfulMessage;

    private const string k_rematchRequestedText = "Waiting for opponent...";
    private const string k_opponentRequestedRematch = "Opponent requested rematch";
    private const string k_opponentLeft = "Opponent has left";
    private const float k_fadeTime = 0.3f;
    private const float k_moveTime = 0.3f;
    private const float k_panelSolidAlpha = 0.9f;
    private const float k_buttonFadedAlpha = 0.2f;

    private Color m_solidPanelColor;
    private RectTransform m_panelRectTransform;
    private Vector3 m_panelOriginalPos;
    private Camera m_mainCamera;
    private float m_offScreenUp;
    private List<GameObject> m_matchDataDisplay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        Instance = this;

        m_panelImage = m_panel.GetComponent<Image>();

        m_solidPanelColor = new(m_panelImage.color.r, m_panelImage.color.g, m_panelImage.color.b, k_panelSolidAlpha);
        m_panelImage.color = m_solidPanelColor;
        m_panel.SetActive(true);

        m_afterGame.SetActive(false);
    }

    private void Start()
    {
        m_mainCamera = Camera.main;
        m_panelRectTransform = m_panel.GetComponent<RectTransform>();
        m_offScreenUp = m_mainCamera.ViewportToWorldPoint(new(0.5f, 1.5f, m_mainCamera.nearClipPlane)).y;
        m_panelOriginalPos = m_panel.transform.position;
        m_helpfulMessage.gameObject.SetActive(true);
        m_helpfulMessage.enabled = false;
        m_matchDataDisplay = new();

        m_rematchButton.onClick.AddListener(() =>
        {
            m_helpfulMessage.enabled = true;
            m_helpfulMessage.text = k_rematchRequestedText;
            m_rematchButtonText.color = Color.white;
            GameManager.Instance.RequestRematchServerRpc();
        });

        m_leaveButton.onClick.AddListener(() =>
        {
            m_helpfulMessage.gameObject.SetActive(false);
            GameManager.Instance.ExitGame();
        });

        m_panelImage.DOFade(0, k_fadeTime).OnComplete(() => m_panel.SetActive(false));

    }

    private void OnEnable()
    {
        GameManager.Instance.OnGameFinished += GameManager_OnGameFinished;
        GameManager.Instance.OnOpponentLeftAfterGame += GameManager_OpponentLeftAfterGame;
        GameManager.Instance.OnOpponentRequestedRematch += GameManager_OpponentRequestedRematch;
        GameManager.Instance.OnResetGame += GameManager_ResetGame;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnGameFinished -= GameManager_OnGameFinished;
        GameManager.Instance.OnOpponentLeftAfterGame -= GameManager_OpponentLeftAfterGame;
        GameManager.Instance.OnOpponentRequestedRematch -= GameManager_OpponentRequestedRematch;
        GameManager.Instance.OnResetGame -= GameManager_ResetGame;
    }

    private void GameManager_OnGameFinished(List<MatchData> allMatchData)
    {
        PopulateMatchData(allMatchData);

        m_panel.SetActive(true);
        m_panelImage.color = m_solidPanelColor;
        m_panelRectTransform.DOMoveY(m_offScreenUp, k_moveTime)
            .From()
            .OnComplete(() =>
            {
                m_panelRectTransform.position = m_panelOriginalPos;
                m_afterGame.SetActive(true);
            });
    }

    private void GameManager_OpponentLeftAfterGame()
    {
        m_rematchButton.enabled = false;
        m_rematchButtonText.color = new(1, 1, 1, k_buttonFadedAlpha); 
        m_helpfulMessage.enabled = true;
        m_helpfulMessage.text = k_opponentLeft;
    }

    private void GameManager_OpponentRequestedRematch()
    {
        m_helpfulMessage.enabled = true;
        m_helpfulMessage.text = k_opponentRequestedRematch;
        m_rematchButtonText.color = Color.white;
    }

    private void GameManager_ResetGame()
    {
        foreach (GameObject data in m_matchDataDisplay)
        {
            Destroy(data);
        }
        m_matchDataDisplay.Clear();
        m_helpfulMessage.enabled = false;

        m_panelRectTransform.DOMoveY(m_offScreenUp, k_moveTime)
            .OnComplete(() =>
            {
                m_panel.SetActive(false);
                m_afterGame.SetActive(false);
                m_rematchButtonText.color = Color.white;
            });
    }

    private void PopulateMatchData(List<MatchData> allMatchData)
    {
        ulong myId = GameManager.Instance.GetPlayerId();
        foreach (MatchData matchData in allMatchData)
        {
            MatchHeading heading = Instantiate(m_matchHeadingPrefab, m_afterGameContent.transform);
            m_matchDataDisplay.Add(heading.gameObject);
            bool didPlayerWinMatch = myId == matchData.GetWinner();
            ulong opponentId = didPlayerWinMatch ? matchData.GetLoser() : matchData.GetWinner();
            heading.SetMatchResult(didPlayerWinMatch);
            heading.SetMatchNumber(matchData.GetMatchNumber());

            int winnerActionsCount = matchData.GetWinnerNumberOfActions();
            int loserActionsCount = matchData.GetLoserNumberOfActions();
            for (int i = 0; i < Mathf.Max(winnerActionsCount, loserActionsCount) ; i++)
            {
                RoundLog item = Instantiate(m_roundLogPrefab, m_afterGameContent.transform);
                m_matchDataDisplay.Add(item.gameObject);
                if (i < winnerActionsCount) item.SetMyRoundAction(matchData.GetAction(myId, i));
                if (i < loserActionsCount) item.SetOpponentRoundAction(matchData.GetAction(opponentId, i));
            }
        }
    }

    public bool IsDisplaying()
    {
        return m_panel.activeSelf;
    }
}
