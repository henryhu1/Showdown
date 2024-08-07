using System.Collections.Generic;
using UnityEngine;
using GameActions;

public class ActionButtonsGroup : MonoBehaviour
{
    public static ActionButtonsGroup Instance { get; private set; }

    public static Color s_enabledButtonColor = new(1, 1, 1, 1);
    public static Color s_disabledButtonColor = new(1, 1, 1, 0.3f);

    [SerializeField] private ActionButton m_attackButton;
    [SerializeField] private ActionButton m_blockButton;
    [SerializeField] private ActionButton m_collectButton;

    private List<ActionButton> m_actionButtonsList;
    private Dictionary<ActionType, ActionButton> m_actionToButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        Instance = this;

        m_attackButton.SetButtonText(ActionLogic.GetGoldChange(ActionType.Attack).ToString());
        m_collectButton.SetButtonText(ActionLogic.GetGoldChange(ActionType.Collect).ToString());
    }

    private void Start()
    {
        GameManager.Instance.OnEnableActionsToBePlayed += GameManager_EnableActionsToBePlayed;
        GameManager.Instance.OnDisableActionsToBePlayed += GameManager_DisableActionsToBePlayed;
        //ActionManager.Instance.OnAllowedToAttack += ActionManager_AllowedToAttack;
        //ActionManager.Instance.OnNotAllowedToAttack += ActionManager_NotAllowedToAttack;

        m_attackButton.AddOnClickListener(ActionButtonClicked, ActionType.Attack);
        m_blockButton.AddOnClickListener(ActionButtonClicked, ActionType.Block);
        m_collectButton.AddOnClickListener(ActionButtonClicked, ActionType.Collect);

        m_actionButtonsList = new List<ActionButton> { m_attackButton, m_blockButton, m_collectButton };
        m_actionToButton = new Dictionary<ActionType, ActionButton>
        {
            { ActionType.Attack, m_attackButton },
            { ActionType.Block, m_blockButton },
            { ActionType.Collect, m_collectButton },
        };

        DisableActions();
    }

    private void OnDisable()
    {
        GameManager.Instance.OnEnableActionsToBePlayed -= GameManager_EnableActionsToBePlayed;
        GameManager.Instance.OnDisableActionsToBePlayed -= GameManager_DisableActionsToBePlayed;
        //ActionManager.Instance.OnAllowedToAttack -= ActionManager_AllowedToAttack;
        //ActionManager.Instance.OnNotAllowedToAttack -= ActionManager_NotAllowedToAttack;
        m_attackButton.RemoveOnClickListeners();
        m_blockButton.RemoveOnClickListeners();
        m_collectButton.RemoveOnClickListeners();
    }

    private void DisableActions()
    {
        foreach (ActionButton actionButton in m_actionButtonsList)
        {
            actionButton.SetEnabled(false);
            actionButton.SetColor(s_disabledButtonColor);
        }
    }

    //private void ActionManager_AllowedToAttack()
    //{
    //    //if (m_disallowedActions.Contains(ActionType.Attack))
    //    //{
    //    //    m_disallowedActions.Remove(ActionType.Attack);
    //    //}
    //}

    //private void ActionManager_NotAllowedToAttack()
    //{
    //    //m_disallowedActions.Add(ActionType.Attack);
    //    m_attackButton.SetColor(s_disabledButtonColor);
    //}

    private void GameManager_EnableActionsToBePlayed()
    {
        foreach (KeyValuePair<ActionType, ActionButton> actionToButton in m_actionToButton)
        {
            bool isActionAllowed = ActionManager.Instance.IsActionAllowed(actionToButton.Key);
            actionToButton.Value.SetEnabled(isActionAllowed);
            Color buttonColor = ActionManager.Instance.IsActionAllowed(actionToButton.Key) ? s_enabledButtonColor : s_disabledButtonColor;
            actionToButton.Value.SetColor(buttonColor);
        }
    }

    private void GameManager_DisableActionsToBePlayed()
    {
        DisableActions();
    }

    private void ActionButtonClicked(ActionType action)
    {
        ActionManager.Instance.EnqueueAction(action);
    }

    public RectTransform GetActionButtonTransform(ActionType action)
    {
        return m_actionToButton[action].getButtonRectTransform();
    }
}
