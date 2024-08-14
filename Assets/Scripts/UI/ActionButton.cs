using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameActions;

public class ActionButton : MonoBehaviour
{
    [SerializeField] private Image m_image;
    [SerializeField] private Button m_button;
    [SerializeField] private TextMeshProUGUI m_text;
    [SerializeField] private RectTransform m_rectTransform;

    public delegate void ActionButtonOnClickListener(GameAction gameAction);

    public RectTransform getButtonRectTransform()
    {
        return m_rectTransform;
    }

    public void SetButtonText(string text)
    {
        m_text.text = text;
    }

    public void AddOnClickListener(ActionButtonOnClickListener onClick, GameAction action)
    {
        if (onClick != null)
        {
            m_button.onClick.AddListener(() => onClick.Invoke(action));
        }
    }

    public void RemoveOnClickListeners()
    {
        m_button.onClick.RemoveAllListeners();
    }

    public bool IsButtonEnabled()
    {
        return m_button.enabled;
    }

    public void SetEnabled(bool enabled)
    {
        m_button.enabled = enabled;
    }

    public void SetColor(Color color)
    {
        m_image.color = color;
    }
}
