using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SelectedActionBox : SelectedActionBaseBehaviour
{
    [SerializeField] private GameObject m_panel;
    private Image m_panelImage;
    [SerializeField] private SelectedActionUIDisplay m_selectedAction;
    [SerializeField] private GameObject m_selectedActionPrefab;

    private SelectedActionUIDisplay m_animatingDisplay;

    private Vector3 m_selectedActionImagePosition;
    private float m_movementDistance;
    private float m_submitAnimationTime = 0.5f;

    private Sequence m_tweenSequence;
    private Tween m_submitActionTween;

    [HideInInspector]
    public delegate void TriggerActionInteractionDelegateHandler();
    [HideInInspector]
    public event TriggerActionInteractionDelegateHandler OnTriggerActionInteraction;

    public override void Start()
    {
        m_panelImage = m_panel.GetComponent<Image>();
        m_movementDistance = m_panelImage.rectTransform.rect.height;

        m_selectedAction.DisableImage();
        m_selectedActionImagePosition = m_selectedAction.GetAnchoredPosition();

        GameManager.Instance.OnActionsDone += GameManager_ActionsDone;

        base.Start();
    }

    public override void OnDisable()
    {
        GameManager.Instance.OnActionsDone -= GameManager_ActionsDone;

        base.OnDisable();
    }

    public override void ActionManager_AddToQueue(GameAction gameAction)
    {
        m_selectedAction.SetSprite(gameAction);
    }

    public override void GameManager_EnableActionsToBePlayed()
    {
        m_selectedAction.EnableImage();
        m_panelImage.color = Colors.Turquoise;
        m_selectedAction.SetSolidImageColor();
    }

    public override void GameManager_DisableActionsToBePlayed()
    {
        Color grey = Colors.Grey;
        m_panelImage.color = new(grey.r, grey.g, grey.b, 0.5f);
        m_selectedAction.SetTransparentImageColor();
    }

    public override void GameManager_TickBeforeActionSubmit()
    {
        m_selectedAction.EnableImage();
        m_panelImage.color = Colors.Turquoise;
        m_selectedAction.SetSolidImageColor();
    }

    public override void ActionManager_SubmitAction()
    {
        Color grey = Colors.Grey;
        m_panelImage.color = new(grey.r, grey.g, grey.b, 0.5f);
        m_selectedAction.SetTransparentImageColor();
        SubmitActionAnimation();
    }

    private void GameManager_ActionsDone(GameAction playerAction, GameAction opponentAction)
    {
        if (m_tweenSequence != null)
        {
            m_tweenSequence.OnComplete(() =>
            {
                Tween selectedActionAnimation = m_animatingDisplay.DoAnimation(playerAction, opponentAction);
                selectedActionAnimation.OnComplete(() =>
                {
                    Destroy(m_animatingDisplay.gameObject, GameManager.RoundCycle);
                }).Play();

                m_tweenSequence = null;
            });
        }
    }

    private void SubmitActionAnimation()
    {
        GameObject animating = Instantiate(m_selectedActionPrefab, transform);

        m_animatingDisplay = animating.GetComponent<SelectedActionUIDisplay>();
        m_animatingDisplay.SetSprite(m_selectedAction.GetSelectedAction());
        m_animatingDisplay.SetSolidImageColor();

        m_submitActionTween = m_animatingDisplay.GetRectTransform()
            .DOAnchorPosY(m_selectedActionImagePosition.y + m_movementDistance, m_submitAnimationTime)
            .SetEase(Ease.OutSine)
            .OnComplete(() =>
            {
                OnTriggerActionInteraction?.Invoke();
            });
        m_tweenSequence = DOTween.Sequence().Append(m_submitActionTween);
    }
}
