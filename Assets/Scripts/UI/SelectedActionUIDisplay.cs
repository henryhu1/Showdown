using UnityEngine;
using UnityEngine.UI;

public class SelectedActionUIDisplay : MonoBehaviour
{
    [SerializeField] private RectTransform m_rectTransform;
    [SerializeField] private Image m_selectedActionImage;

    public void DisableImage()
    {
        m_selectedActionImage.enabled = false;
    }

    public void EnableImage()
    {
        m_selectedActionImage.enabled = true;
    }

    public void SetSprite(Sprite actionSprite)
    {
        m_selectedActionImage.sprite = actionSprite;
    }

    public Sprite GetSprite()
    {
        return m_selectedActionImage.sprite;
    }

    public void SolidifyImageColor()
    {
        Color originalColor = m_selectedActionImage.color;
        m_selectedActionImage.color = new(originalColor.r, originalColor.g, originalColor.b, 1);
    }

    public void FadeImageColor()
    {
        Color originalColor = m_selectedActionImage.color;
        m_selectedActionImage.color = new(originalColor.r, originalColor.g, originalColor.b, 0.5f);
    }

    public Vector3 GetAnchoredPosition()
    {
        return m_rectTransform.anchoredPosition;
    }

    public RectTransform GetRectTransform()
    {
        return m_rectTransform;
    }
}
