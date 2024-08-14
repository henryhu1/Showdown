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

    public Sprite GetSprite(GameAction action)
    {
        return action switch
        {
            GameAction.Collect => collectSprite,
            GameAction.Block => blockSprite,
            GameAction.Attack => attackSprite,
            GameAction.Fire => fireSprite,
            GameAction.Water => waterSprite,
            GameAction.Egg => eggSprite,
            GameAction.Reflect => reflectSprite,
            _ => null,
        };
    }
}
