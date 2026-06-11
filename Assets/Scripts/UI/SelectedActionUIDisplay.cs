using DG.Tweening;
using GameActions;
using UnityEngine;
using UnityEngine.UI;

public class SelectedActionUIDisplay : MonoBehaviour
{
    [SerializeField] private RectTransform m_rectTransform;
    [SerializeField] private Image m_selectedActionImage;

    private GameAction m_selectedAction;

    private const float baseDuration = 0.33f;
    private Vector3 m_scaleXY = new(2, 2, 0);
    private Vector3 m_scaleDisappear = Vector3.zero;
    private Vector3 m_inwardsPunch = new(-0.3f, -0.3f, 0);
    private Vector3 m_outwardsPunch = new(0.3f, 0.3f, 0);
    private Vector3 m_upwardsPunch = new(0, 50, 0);

    public void DisableImage()
    {
        m_selectedActionImage.enabled = false;
    }

    public void EnableImage()
    {
        m_selectedActionImage.enabled = true;
    }

    public GameAction GetSelectedAction()
    {
        return m_selectedAction;
    }

    public void SetSpriteFromGameAction(GameAction gameAction)
    {
        m_selectedAction = gameAction;
        Sprite actionSprite = ActionSpriteManager.Instance.GetSpriteForAction(gameAction);
        m_selectedActionImage.sprite = actionSprite;
    }

    public Sprite GetSprite()
    {
        return ActionSpriteManager.Instance.GetSpriteForAction(m_selectedAction);
    }

    public void SetSolidImageColor()
    {
        SetImageAlpha(1);
    }

    public void SetTransparentImageColor()
    {
        SetImageAlpha(0.5f);
    }

    public void SetInvisibleImageColor()
    {
        SetImageAlpha(0);
    }

    private void SetImageAlpha(float alpha)
    {
        Color originalColor = m_selectedActionImage.color;
        m_selectedActionImage.color = new(originalColor.r, originalColor.g, originalColor.b, alpha);
    }

    public Tween FadeImageColor()
    {
        return m_selectedActionImage.DOFade(0, baseDuration).SetEase(Ease.InQuart);
    }

    public Tween ExpandSize(bool isHalfTick = false)
    {
        float duration = baseDuration;
        if (isHalfTick)
        {
            duration /= 2;
        }
        return m_rectTransform.DOScale(m_scaleXY, duration);
    }

    public Tween InwardsPunch(int multiplier = 1, bool isHalfTick = false)
    {
        float duration = baseDuration;
        if (isHalfTick)
        {
            duration /= 2;
        }
        return m_rectTransform.DOPunchScale(m_inwardsPunch * multiplier, duration, vibrato: 0);
    }

    public Tween OutwardsPunch()
    {
        return m_rectTransform.DOPunchScale(m_outwardsPunch, baseDuration, vibrato: 0);
    }

    public Tween UpwardsPunch(bool isHalfTick = false)
    {
        float duration = baseDuration;
        if (isHalfTick)
        {
            duration /= 2;
        }
        return m_rectTransform.DOPunchPosition(m_upwardsPunch, duration, vibrato: 0, elasticity: 0);
    }

    public Tween ShakePosition()
    {
        return m_rectTransform.DOShakeAnchorPos(baseDuration);
    }

    public Tween ScaleDisappear(bool isHalfTick = false)
    {
        float duration = baseDuration;
        if (isHalfTick)
        {
            duration /= 2;
        }
        return m_rectTransform.DOScale(m_scaleDisappear, duration);
    }

    public Tween ShakeScale(bool isHalfTick = false)
    {
        float duration = baseDuration;
        if (isHalfTick)
        {
            duration /= 2;
        }
        return m_rectTransform.DOShakeScale(duration);
    }

    public Vector3 GetAnchoredPosition()
    {
        return m_rectTransform.anchoredPosition;
    }

    public RectTransform GetRectTransform()
    {
        return m_rectTransform;
    }

    public Tween DoAnimation(GameAction playerAction, GameAction opponentAction)
    {
        ActionType playerActionType = ActionLogic.GetActionType(playerAction);
        ActionType opponentActionType = ActionLogic.GetActionType(opponentAction);

        Sequence animation = DOTween.Sequence();

        if (playerActionType == ActionType.Passive && opponentActionType == ActionType.Offensive)
        {
            return animation
                .Append(ShakePosition())
                .Append(ScaleDisappear());
        }
        if (playerActionType == ActionType.Defensive && opponentActionType == ActionType.Offensive)
        {
            return animation
                .Append(ExpandSize(true))
                .Append(InwardsPunch(multiplier: 2, isHalfTick: true))
                .Join(FadeImageColor());
        }
        if (playerActionType == ActionType.Offensive && opponentActionType == ActionType.Passive)
        {
            return animation
                .Append(ExpandSize(true))
                .Join(UpwardsPunch(true))
                .Append(UpwardsPunch(true))
                .Join(FadeImageColor());
        }
        if (playerActionType == ActionType.Offensive && opponentActionType == ActionType.Defensive)
        {
            return animation
                .Append(ShakeScale())
                .Append(ScaleDisappear());
        }

        return playerActionType switch
        {
            ActionType.Passive => animation
                .Append(InwardsPunch())
                .Join(FadeImageColor()),
            ActionType.Defensive => animation
                .Append(OutwardsPunch())
                .Join(FadeImageColor()),
            ActionType.Offensive => animation
                .Append(UpwardsPunch())
                .Join(FadeImageColor()),
            _ => animation
        };
    }
}
