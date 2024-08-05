using UnityEngine;
using UnityEngine.UI;

public class SelectedActionBox : SelectedActionBaseBehaviour
{
    [SerializeField] private Image m_panelImage;
    [SerializeField] private Image m_selectedActionImage;

    public override void Start()
    {
        m_selectedActionImage.enabled = false;
        base.Start();
    }

    public override void GameManager_EnableActionsToBePlayed()
    {
        m_selectedActionImage.enabled = true;
        m_panelImage.color = Colors.Turquoise;
        Color originalColor = m_selectedActionImage.color;
        m_selectedActionImage.color = new(originalColor.r, originalColor.g, originalColor.b, 1);
    }

    public override void GameManager_AddToQueue(ActionType gameAction)
    {
        Sprite actionSprite = ActionSpriteManager.Instance.GetSpriteForAction(gameAction);
        m_selectedActionImage.sprite = actionSprite;
    }

    public override void GameManager_DisableActionsToBePlayed()
    {
        Color grey = Colors.Grey;
        m_panelImage.color = new(grey.r, grey.g, grey.b, 0.5f);
        Color originalColor = m_selectedActionImage.color;
        m_selectedActionImage.color = new(originalColor.r, originalColor.g, originalColor.b, 0.5f);
    }

    public override void GameManager_SubmitAction()
    {
        Color grey = Colors.Grey;
        m_panelImage.color = new(grey.r, grey.g, grey.b, 0.5f);
        Color originalColor = m_selectedActionImage.color;
        m_selectedActionImage.color = new(originalColor.r, originalColor.g, originalColor.b, 0.5f);
    }
}
