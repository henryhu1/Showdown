using System.Collections;
using UnityEngine;

public class SelectedActionArrow : SelectedActionBaseBehaviour
{
    public static SelectedActionArrow Instance { get; private set; }

    [SerializeField] private float m_verticalMovement = 5f;
    [SerializeField] private float m_cycleDuration = 5f;
    [SerializeField] private AnimationCurve m_movementCurve;
    [SerializeField] private RectTransform m_panel;

    private Camera m_mainCamera;

    private Coroutine m_animationCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        Instance = this;

        m_mainCamera = Camera.main;
    }

    public override void Start()
    {
        m_panel.gameObject.SetActive(false);
        base.Start();
    }

    private IEnumerator VerticalTransitionAnimation()
    {
        Vector3 finalPosition = transform.position;
        int direction = 1;
        if (m_verticalMovement < 0)
        {
            direction = -1;
        }
        finalPosition.y += direction * m_verticalMovement;
        Debug.LogFormat("<color=blue>{0} {1}</color>",transform.position, finalPosition);
        float time = 0;
        while (true)
        {
            time += Time.deltaTime;
            float step = time / m_cycleDuration;
            float curveStep = m_movementCurve.Evaluate(step);
            if (Mathf.Abs(transform.position.y - finalPosition.y) > 0.1f)
            {
                Vector3 newPos = Vector3.Lerp(transform.position, finalPosition, curveStep);
                Debug.LogFormat("<color=green>{0}</color>", newPos);
                transform.position = Utils.ScreenToWorld(m_mainCamera, newPos);
            }
            else
            {
                time = 0;
                direction *= -1;
                finalPosition.y += direction * m_verticalMovement;
            }
            yield return null;
        }
    }

    public override void GameManager_EnableActionsToBePlayed()
    {
        m_panel.gameObject.SetActive(true);
    }

    public override void GameManager_DisableActionsToBePlayed()
    {
        if (m_animationCoroutine != null)
        {
            StopCoroutine(m_animationCoroutine);
            m_animationCoroutine = null;
        }
        m_panel.gameObject.SetActive(false);
    }

    public override void ActionManager_AddToQueue(ActionType gameAction)
    {
        m_panel.gameObject.SetActive(true);
        RectTransform actionButtonTransform = ActionButtonsGroup.Instance.GetActionButtonTransform(gameAction);
        Vector3 setPosition = actionButtonTransform.position;
        setPosition.y += actionButtonTransform.sizeDelta.y;
        Debug.LogFormat("<color=red>{0}</color>", actionButtonTransform.sizeDelta.y);
        transform.position = Utils.ScreenToWorld(m_mainCamera, setPosition);
        if (m_animationCoroutine != null)
        {
            StopCoroutine(m_animationCoroutine);
        }
        m_animationCoroutine = StartCoroutine(VerticalTransitionAnimation());
    }

    public override void ActionManager_SubmitAction()
    {
        if (m_animationCoroutine != null)
        {
            StopCoroutine(m_animationCoroutine);
        }
        m_animationCoroutine = null;
        m_panel.gameObject.SetActive(false);
    }
}
