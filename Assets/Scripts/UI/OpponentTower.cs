using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class OpponentTower : MonoBehaviour
{
    [SerializeField] private GameObject m_opponentAction;
    [SerializeField] private Image m_opponentTowerImage;
    [SerializeField] private GameObject m_selectedActionPrefab;
    [SerializeField] private SelectedActionBox m_playerSelectedAction;
    [SerializeField] private Sprite m_lowGoldOpponentTower;
    [SerializeField] private Sprite m_midGoldOpponentTower;
    [SerializeField] private Sprite m_highGoldOpponentTower;

    private Tween m_opponentAnimation;

    private void OnEnable()
    {
        GameManager.Instance.OnActionsDone += GameManager_ActionsDone;
        GameManager.Instance.OnOpponentGoldState += GameManager_OpponentGoldState;
        GameManager.Instance.OnMatchDecided += GameManager_MatchDecided;
        m_playerSelectedAction.OnTriggerActionInteraction += SelectedAction_TriggerActionInteraction;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnActionsDone -= GameManager_ActionsDone;
        GameManager.Instance.OnOpponentGoldState -= GameManager_OpponentGoldState;
        GameManager.Instance.OnMatchDecided -= GameManager_MatchDecided;
        m_playerSelectedAction.OnTriggerActionInteraction -= SelectedAction_TriggerActionInteraction;
    }

    private void GameManager_ActionsDone(GameAction playerAction, GameAction opponentAction)
    {
        GameObject animating = Instantiate(m_selectedActionPrefab, m_opponentAction.transform);
        SelectedActionUIDisplay animatingDisplay = animating.GetComponent<SelectedActionUIDisplay>();

        animatingDisplay.SetSpriteFromGameAction(opponentAction);
        m_opponentAnimation = animatingDisplay.DoAnimation(opponentAction, playerAction).OnComplete(() => Destroy(animating));
    }

    private void GameManager_OpponentGoldState(GoldState opponentGoldState)
    {
        Sprite opponentTowerSprite = opponentGoldState switch
        {
            GoldState.Low => m_lowGoldOpponentTower,
            GoldState.Mid => m_midGoldOpponentTower,
            GoldState.High => m_highGoldOpponentTower,
            _ => m_lowGoldOpponentTower,
        };
        m_opponentTowerImage.sprite = opponentTowerSprite;
    }

    private void GameManager_MatchDecided(bool hasPlayerWon, MatchResult resultType)
    {
        //throw new System.NotImplementedException();
    }

    private void SelectedAction_TriggerActionInteraction()
    {
        m_opponentAnimation.Play();
    }
}
