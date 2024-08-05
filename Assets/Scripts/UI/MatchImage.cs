using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchImage : MonoBehaviour
{
    private Image m_MatchResultImage;

    private void Awake()
    {
        m_MatchResultImage = GetComponent<Image>();
    }

    public void SetImage(Sprite sprite)
    {
        m_MatchResultImage.sprite = sprite;
    }
}
