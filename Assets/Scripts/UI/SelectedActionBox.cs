using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SelectedActionBox : SelectedActionBaseBehaviour
{
    [SerializeField] private GameObject m_panel;
    private Image m_panelImage;
    [SerializeField] private SelectedActionUIDisplay m_selectedAction;
    [SerializeField] private GameObject m_selectedActionPrefab;

    private Vector3 m_selectedActionImagePosition;
    private float m_movementDistance;
    private float m_submitAnimationTime = 0.5f;

    public override void Start()
    {
        m_panelImage = m_panel.GetComponent<Image>();
        m_movementDistance = m_panelImage.rectTransform.rect.height;

        m_selectedAction.DisableImage();
        m_selectedActionImagePosition = m_selectedAction.GetAnchoredPosition();

        base.Start();
    }

    public override void GameManager_EnableActionsToBePlayed()
    {
        m_selectedAction.EnableImage();
        m_panelImage.color = Colors.Turquoise;
        m_selectedAction.SolidifyImageColor();
    }

    public override void ActionManager_AddToQueue(ActionType gameAction)
    {
        Sprite actionSprite = ActionSpriteManager.Instance.GetSpriteForAction(gameAction);
        m_selectedAction.SetSprite(actionSprite);
    }

    public override void GameManager_DisableActionsToBePlayed()
    {
        Color grey = Colors.Grey;
        m_panelImage.color = new(grey.r, grey.g, grey.b, 0.5f);
        m_selectedAction.FadeImageColor();
    }

    public override void ActionManager_SubmitAction()
    {
        Color grey = Colors.Grey;
        m_panelImage.color = new(grey.r, grey.g, grey.b, 0.5f);
        m_selectedAction.FadeImageColor();
        SubmitActionAnimation();
    }

    private void SubmitActionAnimation()
    {
        GameObject animating = Instantiate(m_selectedActionPrefab, transform);
        SelectedActionUIDisplay animatingDisplay = animating.GetComponent<SelectedActionUIDisplay>();
        animatingDisplay.SetSprite(m_selectedAction.GetSprite());
        animatingDisplay.SolidifyImageColor();
        animatingDisplay.GetRectTransform()
            .DOAnchorPosY(m_selectedActionImagePosition.y + m_movementDistance, m_submitAnimationTime)
            .SetEase(Ease.OutSine);

        Destroy(animating, m_submitAnimationTime);
    }
}
