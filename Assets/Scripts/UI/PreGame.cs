using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreGame : MonoBehaviour
{
    [SerializeField] private GameObject m_panel;
    private Image m_panelImage;

    private const float m_fadeTime = 0.3f;
    private const float k_panelSolidAlpha = 0.9f;

    private void Awake()
    {
        m_panelImage = m_panel.GetComponent<Image>();
        m_panelImage.color = new(m_panelImage.color.r, m_panelImage.color.g, m_panelImage.color.b, k_panelSolidAlpha);
        m_panel.SetActive(true);
    }

    private void Start()
    {
        m_panelImage.DOFade(0, m_fadeTime).OnComplete(() => m_panel.SetActive(false));
    }
}
