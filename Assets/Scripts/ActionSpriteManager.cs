using UnityEngine;

public class ActionSpriteManager : MonoBehaviour
{
    public static ActionSpriteManager Instance { get; private set; }

    [SerializeField] private ActionSpriteData actionSpriteData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }

        Instance = this;
    }

    public Sprite GetSpriteForAction(ActionType action)
    {
        return actionSpriteData.GetSprite(action);
    }
}
