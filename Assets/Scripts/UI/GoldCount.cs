using DG.Tweening;
using GameActions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoldCount : MonoBehaviour
{
    public static GoldCount Instance { get; private set; }

    [SerializeField] private Image m_goldImage;
    private RectTransform m_goldImageRectTransform;
    private Vector3 m_goldImageRectTransformPosition;

    [SerializeField] private TextMeshProUGUI m_countText;
    private int m_localGoldCount;

    [SerializeField] private TextMeshProUGUI m_goldChangeAmountText;

    [SerializeField] private TextMeshProUGUI m_totalGoldToWinText;
    [SerializeField] private RectTransform m_totalGoldToWinProgressBar;

    private int m_maxGoldProgressAt;
    private float m_maxProgressBarHeight;

    private Vector3 m_punchSideways = new(10, 0, 0);
    private Vector3 m_punchUpwards = new(0, 30, 0);
    private Vector3 m_punchCountText = new(0.5f, 0.5f, 0);
    private Vector3 m_punchRightRotation = new(0, 200, 0);

    private Tween m_punchImageTween;
    private Tween m_countTextColorTween;
    private Tween m_changeTextColorTween;
    private Tween m_almostGoldWinTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        m_goldImageRectTransform = m_goldImage.rectTransform;
        m_goldImageRectTransformPosition = m_goldImageRectTransform.localPosition;
        m_maxProgressBarHeight = m_totalGoldToWinProgressBar.sizeDelta.y;
        m_maxGoldProgressAt = GameManager.k_StartingGoldTotalToWin;
    }

    private void Start()
    {
        GameManager.Instance.OnPlayerGoldChange += GameManager_PlayerGoldChange;
        GameManager.Instance.OnMatchDecided += GameManager_MatchDecided;
        ActionManager.Instance.OnActionEnqueue += ActionManager_ActionEnqueue;
        ActionManager.Instance.OnActionDequeue += ActionManager_ActionDequeue;
        ActionManager.Instance.OnDisallowedActionAttempted += ActionManager_DisallowedActionAttempted;

        m_almostGoldWinTween = m_goldImageRectTransform
            .DOPunchPosition(m_punchUpwards, GameManager.k_TimePerTick, vibrato: 0, elasticity: 0)
            .SetLoops(-1)
            .Pause();
        ResetGoldDisplay();
    }

    private void OnDisable()
    {
        GameManager.Instance.OnPlayerGoldChange -= GameManager_PlayerGoldChange;
        GameManager.Instance.OnMatchDecided -= GameManager_MatchDecided;
        ActionManager.Instance.OnActionEnqueue -= ActionManager_ActionEnqueue;
        ActionManager.Instance.OnActionDequeue -= ActionManager_ActionDequeue;
        ActionManager.Instance.OnDisallowedActionAttempted -= ActionManager_DisallowedActionAttempted;
    }

    public void RegisterNetworkCallbacks()
    {
        GameManager.Instance.m_GoldTotalToWin.OnValueChanged += OnGoldTotalToWinChanged;
        GameManager.Instance.m_AtMatch.OnValueChanged += OnAtMatchChanged;
    }

    public void UnregisterNetworkCallbacks()
    {
        GameManager.Instance.m_GoldTotalToWin.OnValueChanged -= OnGoldTotalToWinChanged;
        GameManager.Instance.m_AtMatch.OnValueChanged -= OnAtMatchChanged;
    }

    private void ResetGoldDisplay()
    {
        m_goldChangeAmountText.enabled = false;
        m_totalGoldToWinText.text = GameManager.Instance.m_GoldTotalToWin.Value.ToString();
        m_totalGoldToWinProgressBar.sizeDelta = new(m_totalGoldToWinProgressBar.sizeDelta.x, 0);
    }

    private void OnGoldTotalToWinChanged(int previousAmount, int currentAmount)
    {
        m_totalGoldToWinText.text = currentAmount.ToString();
    }

    private void OnAtMatchChanged(int previousAmount, int currentAmount)
    {
        ResetGoldDisplay();
    }

    private void GameManager_PlayerGoldChange(int newAmount)
    {
        m_countText.text = newAmount.ToString();
        float progress = Mathf.Min((float)newAmount / m_maxGoldProgressAt, 1);
        m_totalGoldToWinProgressBar.DOSizeDelta(new(m_totalGoldToWinProgressBar.sizeDelta.x, progress * m_maxProgressBarHeight), GameManager.k_TimePerTick);

        if (m_localGoldCount != newAmount)
        {
            m_localGoldCount = newAmount;
            m_countText.rectTransform.DOPunchScale(m_punchCountText, GameManager.k_TimePerTick, vibrato: 0, elasticity: 0);
        }

        if (newAmount + ActionLogic.GetGoldChange(GameAction.Collect) == GameManager.Instance.m_GoldTotalToWin.Value)
        {
            m_almostGoldWinTween.Play();
        }
        else
        {
            StopAlmostGoldWinAnimation();
        }
    }

    private void GameManager_MatchDecided(bool hasPlayerWon, MatchResult resultType)
    {
        Debug.Log($"{hasPlayerWon}, {resultType}");
        StopAlmostGoldWinAnimation();
        if (hasPlayerWon && resultType == MatchResult.GoldReached)
        {
            m_goldImageRectTransform.DOPunchRotation(m_punchRightRotation, GameManager.k_TimePerTick, vibrato: 0, elasticity: 0);
        }
    }

    private void ActionManager_ActionEnqueue(GameAction action)
    {
        int goldChange = ActionLogic.GetGoldChange(action);
        m_goldChangeAmountText.enabled = goldChange != 0;
        if (goldChange > 0)
        {
            m_goldChangeAmountText.text = $"(+{goldChange})";
        }
        else if (goldChange < 0)
        {
            m_goldChangeAmountText.text = $"({goldChange})";
        }
    }

    private void ActionManager_ActionDequeue(GameAction action)
    {
        m_goldChangeAmountText.enabled = false;
    }

    private void ActionManager_DisallowedActionAttempted(GameAction disallowedAction)
    {
        m_punchImageTween.Kill();
        m_countTextColorTween.Kill();
        m_changeTextColorTween.Kill();

        m_punchImageTween = m_goldImageRectTransform
            .DOPunchPosition(m_punchSideways, GameManager.k_TimePerTick)
            .OnComplete(() => m_goldImageRectTransform.localPosition = m_goldImageRectTransformPosition)
            .OnKill(() => m_goldImageRectTransform.localPosition = m_goldImageRectTransformPosition);

        m_countText.color = Color.red;
        m_goldChangeAmountText.color = Color.red;

        m_countTextColorTween = m_countText.DOColor(Color.white, GameManager.k_TimePerTick).OnComplete(() => m_countText.color = Color.white);
        m_changeTextColorTween = m_goldChangeAmountText.DOColor(Color.white, GameManager.k_TimePerTick).OnComplete(() => m_goldChangeAmountText.color = Color.white);
    }

    private void StopAlmostGoldWinAnimation()
    {
        m_almostGoldWinTween.Pause();
        m_goldImageRectTransform.localPosition = m_goldImageRectTransformPosition;
    }
}
