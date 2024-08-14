using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchImage : MonoBehaviour
{
    private Image m_MatchResultImage;
    private RectTransform m_RectTransform;

    private Vector3 m_Punch = new(1, 1, 0);

    private void Awake()
    {
        m_MatchResultImage = GetComponent<Image>();
        m_RectTransform = GetComponent<RectTransform>();
    }

    public void SetImage(Sprite sprite)
    {
        m_MatchResultImage.sprite = sprite;
    }

    public void DoPunchScaleAnimation()
    {
        m_RectTransform.DOPunchScale(m_Punch, GameManager.k_TimePerTick, vibrato: 0, elasticity: 0);
    }
}
