using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchHeading : MonoBehaviour
{
    [SerializeField] private Image m_matchWinLoss;
    [SerializeField] private TextMeshProUGUI m_headingText;

    [SerializeField] private Sprite m_win;
    [SerializeField] private Sprite m_loss;

    public void SetMatchNumber(int match)
    {
        m_headingText.text = $"Match {match}:";
    }

    public void SetMatchResult(bool hasWon)
    {
        Sprite matchDisplay = hasWon ? m_win : m_loss;
        m_matchWinLoss.sprite = matchDisplay;
    }
}
