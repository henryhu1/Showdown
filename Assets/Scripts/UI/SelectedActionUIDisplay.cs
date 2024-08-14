using DG.Tweening;
using GameActions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectedActionUIDisplay : MonoBehaviour
{
    [SerializeField] private RectTransform m_rectTransform;
    [SerializeField] private Image m_selectedActionImage;

    private GameAction m_selectedAction;

    private Vector3 m_scaleXY = new(2, 2, 0);
    private Vector3 m_scaleDisappear = Vector3.zero;
    private Vector3 m_inwardsPunch = new(-0.3f, -0.3f, 0);
    private Vector3 m_outwardsPunch = new(0.3f, 0.3f, 0);
    private Vector3 m_upwardsPunch = new(0, 50, 0);

    private Dictionary<ActionType, Sequence> m_actionDefaultAnimations;

    private Dictionary<ActionType, Dictionary<ActionType, Tween>> m_actionInteractionAnimations;

    private void Awake()
    {
        m_actionDefaultAnimations = new()
        {
            {
                ActionType.Passive,
                DOTween.Sequence()
                    .Append(InwardsPunch())
                    .Join(FadeImageColor()).Pause()
            },
            {
                ActionType.Defensive,
                DOTween.Sequence()
                    .Append(OutwardsPunch())
                    .Join(FadeImageColor()).Pause()
            },
            {
                ActionType.Offensive,
                DOTween.Sequence()
                    .Append(UpwardsPunch())
                    .Join(FadeImageColor()).Pause()
            },
        };

        m_actionInteractionAnimations = new()
        {
            {
                ActionType.Passive,
                new()
                {
                    {
                        ActionType.Offensive,
                        DOTween.Sequence()
                            .Append(ShakePosition())
                            .Append(ScaleDisappear()).Pause()
                    },
                }
            },
            {
                ActionType.Defensive,
                new()
                {
                    {
                        ActionType.Offensive,
                        DOTween.Sequence()
                            .Append(ExpandSize(true))
                            .Append(InwardsPunch(multiplier: 2, isHalfTick: true))
                            .Join(FadeImageColor()).Pause()
                    },
                }
            },
            {
                ActionType.Offensive,
                new()
                {
                    {
                        ActionType.Passive,
                        DOTween.Sequence()
                            .Append(ExpandSize(true))
                            .Join(UpwardsPunch(true))
                            .Append(UpwardsPunch(true))
                            .Join(FadeImageColor()).Pause()
                    },
                    {
                        ActionType.Defensive,
                        DOTween.Sequence()
                            .Append(ShakeScale())
                            .Append(ScaleDisappear()).Pause()
                    },
                }
            },
        };
    }

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

    public void SetSprite(GameAction gameAction)
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
        return m_selectedActionImage.DOFade(0, GameManager.k_TimePerTick).SetEase(Ease.InQuart);
    }

    public Tween ExpandSize(bool isHalfTick = false)
    {
        float duration = GameManager.k_TimePerTick;
        if (isHalfTick)
        {
            duration /= 2;
        }
        return m_rectTransform.DOScale(m_scaleXY, duration);
    }

    public Tween InwardsPunch(int multiplier = 1, bool isHalfTick = false)
    {
        float duration = GameManager.k_TimePerTick;
        if (isHalfTick)
        {
            duration /= 2;
        }
        return m_rectTransform.DOPunchScale(m_inwardsPunch * multiplier, duration, vibrato: 0);
    }

    public Tween OutwardsPunch()
    {
        return m_rectTransform.DOPunchScale(m_outwardsPunch, GameManager.k_TimePerTick, vibrato: 0);
    }

    public Tween UpwardsPunch(bool isHalfTick = false)
    {
        float duration = GameManager.k_TimePerTick;
        if (isHalfTick)
        {
            duration /= 2;
        }
        return m_rectTransform.DOPunchPosition(m_upwardsPunch, duration, vibrato: 0, elasticity: 0);
    }

    public Tween ShakePosition()
    {
        return m_rectTransform.DOShakeAnchorPos(GameManager.k_TimePerTick);
    }

    public Tween ScaleDisappear(bool isHalfTick = false)
    {
        float duration = GameManager.k_TimePerTick;
        if (isHalfTick)
        {
            duration /= 2;
        }
        return m_rectTransform.DOScale(m_scaleDisappear, duration);
    }

    public Tween ShakeScale(bool isHalfTick = false)
    {
        float duration = GameManager.k_TimePerTick;
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

        if (m_actionInteractionAnimations[playerActionType].TryGetValue(opponentActionType, out Tween anim))
        {
            return anim;
        }
        else
        {
            return m_actionDefaultAnimations[playerActionType];
        }
    }
}
