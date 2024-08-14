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
    [SerializeField] private TextMeshProUGUI m_changeText;

    private Vector3 m_punchSideways = new(10, 0, 0);
    private Vector3 m_punchUpwards = new(0, 30, 0);
    private Vector3 m_punchCountText = new(0.5f, 0.5f, 0);

    private Tween m_punchImageTween;
    private Tween m_countTextColorTween;
    private Tween m_changeTextColorTween;
    private Tween m_almostGoldWinTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        Instance = this;
        m_changeText.enabled = false;
        m_goldImageRectTransform = m_goldImage.rectTransform;
        m_goldImageRectTransformPosition = m_goldImageRectTransform.localPosition;
    }

    private void Start()
    {
        GameManager.Instance.OnPlayerGoldChange += GameManager_PlayerGoldChange;
        ActionManager.Instance.OnAddToQueue += ActionManager_AddToQueue;
        ActionManager.Instance.OnActionDequeue += ActionManager_ActionDequeue;
        ActionManager.Instance.OnDisallowedActionAttempted += ActionManager_DisallowedActionAttempted;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnPlayerGoldChange -= GameManager_PlayerGoldChange;
        ActionManager.Instance.OnAddToQueue -= ActionManager_AddToQueue;
        ActionManager.Instance.OnActionDequeue -= ActionManager_ActionDequeue;
        ActionManager.Instance.OnDisallowedActionAttempted -= ActionManager_DisallowedActionAttempted;
    }

    private void GameManager_PlayerGoldChange(int newAmount)
    {
        m_countText.text = newAmount.ToString();
        m_countText.rectTransform.DOPunchScale(m_punchCountText, GameManager.k_TimePerTick, vibrato: 0, elasticity: 0);

        if (newAmount + ActionLogic.GetGoldChange(GameAction.Collect) == GameManager.k_GoldTotalToWin)
        {
            m_almostGoldWinTween = m_goldImageRectTransform.DOPunchPosition(m_punchUpwards, GameManager.k_TimePerTick, vibrato: 0, elasticity: 0).SetLoops(-1);
        }
        else if (m_almostGoldWinTween != null)
        {
            m_almostGoldWinTween.Kill();
            m_almostGoldWinTween = null;
            m_goldImageRectTransform.localPosition = m_goldImageRectTransformPosition;
        }
    }

    private void ActionManager_AddToQueue(GameAction action)
    {
        int goldChange = ActionLogic.GetGoldChange(action);
        m_changeText.enabled = goldChange != 0;
        if (goldChange > 0)
        {
            m_changeText.text = $"(+{goldChange})";
        }
        else if (goldChange < 0)
        {
            m_changeText.text = $"({goldChange})";
        }
    }

    private void ActionManager_ActionDequeue()
    {
        m_changeText.enabled = false;
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
        m_changeText.color = Color.red;

        m_countTextColorTween = m_countText.DOColor(Color.white, GameManager.k_TimePerTick).OnComplete(() => m_countText.color = Color.white);
        m_changeTextColorTween = m_changeText.DOColor(Color.white, GameManager.k_TimePerTick).OnComplete(() => m_changeText.color = Color.white);
    }
}
