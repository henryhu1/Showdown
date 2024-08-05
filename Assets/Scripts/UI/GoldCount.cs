using GameActions;
using TMPro;
using UnityEngine;

public class GoldCount : MonoBehaviour
{
    public static GoldCount Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI m_countText;
    [SerializeField] private TextMeshProUGUI m_changeText;

    private float m_notEnoughGoldAnimationTime = 0.5f;
    //private float m_notEnoughGoldAnimationTimeQuarter;
    //private float m_notEnoughGoldAnimationTimeHalf;
    //private float m_notEnoughGoldAnimationTimeThreeQuarters;
    //[SerializeField] private AnimationCurve m_movementCurve;
    //private float m_horizontalOffset = 50;

    //private Coroutine m_notEnoughGoldCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        Instance = this;
        m_changeText.enabled = false;
        //m_notEnoughGoldAnimationTimeHalf = m_notEnoughGoldAnimationTime / 2;
        //m_notEnoughGoldAnimationTimeQuarter = m_notEnoughGoldAnimationTimeHalf / 4;
        //m_notEnoughGoldAnimationTimeThreeQuarters = 3 * m_notEnoughGoldAnimationTimeHalf / 4;
    }

    private void Start()
    {
        GameManager.Instance.OnPlayerGoldChange += GameManager_PlayerGoldChange;
        GameManager.Instance.OnAddToQueue += GameManager_AddToQueue;
        GameManager.Instance.OnActionDequeue += GameManager_Dequeue;
        //ActionButtonsGroup.Instance.OnDisallowedActionAttempted += ActionButtonsGroup_DisallowedActionAttempted;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnPlayerGoldChange -= GameManager_PlayerGoldChange;
        GameManager.Instance.OnAddToQueue -= GameManager_AddToQueue;
        GameManager.Instance.OnActionDequeue -= GameManager_Dequeue;
        //ActionButtonsGroup.Instance.OnDisallowedActionAttempted -= ActionButtonsGroup_DisallowedActionAttempted;
    }

    private void GameManager_PlayerGoldChange(int newAmount)
    {
        m_countText.text = newAmount.ToString();
    }

    private void GameManager_AddToQueue(ActionType action)
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

    private void GameManager_Dequeue()
    {
        m_changeText.enabled = false;
    }

    //private void ActionButtonsGroup_DisallowedActionAttempted(Type disallowedAction)
    //{
    //    if (ActionCost.GetCost(disallowedAction) < 0)
    //    {
    //        if (m_notEnoughGoldCoroutine != null)
    //        {
    //            StopCoroutine(m_notEnoughGoldCoroutine);
    //            m_notEnoughGoldCoroutine = null;
    //        }
    //        m_notEnoughGoldCoroutine = StartCoroutine(NotEnoughGold());
    //    }
    //}

    //private IEnumerator NotEnoughGold()
    //{
    //    float time = 0;
    //    m_countText.color = Color.red;
    //    m_changeText.color = Color.red;
    //    //Vector3 originalPos = transform.position;
    //    //Vector3 movement = new Vector3(m_horizontalOffset, 0, 0);
    //    //Vector3 leftPos = originalPos - movement;
    //    //Vector3 rightPos = originalPos + movement;
    //    //Vector3 atPos = originalPos;
    //    //Vector3 targetPos = leftPos;
    //    //Debug.LogFormat("original: {0}\nmovement: {1}\nleft: {2}\nright:{3}\nat:{4}\ntarget:{5}", originalPos, movement, leftPos, rightPos, atPos, targetPos);
    //    while (time < m_notEnoughGoldAnimationTime)
    //    {
    //        time += Time.deltaTime;
    //        float step = time / m_notEnoughGoldAnimationTime;

    //        float colorGreenYellow = Mathf.Lerp(0, 1, step);
    //        Color color = new Color(1, colorGreenYellow, colorGreenYellow);
    //        m_countText.color = color;
    //        m_changeText.color = color;

    //        //if (time > m_notEnoughGoldAnimationTimeQuarter)
    //        //{
    //        //    atPos = targetPos;
    //        //    targetPos = rightPos;
    //        //    Debug.LogFormat("original: {0}\nmovement: {1}\nleft: {2}\nright:{3}\nat:{4}\ntarget:{5}\ntime: {6}", originalPos, movement, leftPos, rightPos, atPos, targetPos, time);
    //        //}
    //        //else if (time > m_notEnoughGoldAnimationTimeThreeQuarters)
    //        //{
    //        //    atPos = targetPos;
    //        //    targetPos = originalPos;
    //        //    Debug.LogFormat("original: {0}\nmovement: {1}\nleft: {2}\nright:{3}\nat:{4}\ntarget:{5}\ntime: {6}", originalPos, movement, leftPos, rightPos, atPos, targetPos, time);
    //        //}
    //        //float movementStep = m_movementCurve.Evaluate(step / 4);
    //        //transform.position = Vector3.Lerp(atPos, targetPos, movementStep);

    //        yield return null;
    //    }
    //    m_countText.color = Color.white;
    //    m_changeText.color = Color.white;
    //    //transform.position = originalPos;
    //    m_notEnoughGoldCoroutine = null;
    //}
}
