using UnityEngine;

[CreateAssetMenu(fileName = "ActionSpriteData", menuName = "Game/Action Sprite Data")]
public class ActionSpriteData : ScriptableObject
{
    public Sprite collectSprite;
    public Sprite blockSprite;
    public Sprite attackSprite;
    public Sprite fireSprite;
    public Sprite waterSprite;
    public Sprite eggSprite;
    public Sprite reflectSprite;

    public Sprite GetSprite(ActionType action)
    {
        return action switch
        {
            ActionType.Collect => collectSprite,
            ActionType.Block => blockSprite,
            ActionType.Attack => attackSprite,
            ActionType.Fire => fireSprite,
            ActionType.Water => waterSprite,
            ActionType.Egg => eggSprite,
            ActionType.Reflect => reflectSprite,
            _ => null,
        };
    }
}
