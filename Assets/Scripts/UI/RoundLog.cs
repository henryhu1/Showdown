using UnityEngine;
using UnityEngine.UI;

public class RoundLog : MonoBehaviour
{
    [SerializeField] private Image actionImage1;
    [SerializeField] private Image actionImage2;

    public void SetMyRoundAction(GameAction action)
    {
        actionImage1.sprite = ActionSpriteManager.Instance.GetSpriteForAction(action);
    }

    public void SetOpponentRoundAction(GameAction action)
    {
        actionImage2.sprite = ActionSpriteManager.Instance.GetSpriteForAction(action);
    }
}
