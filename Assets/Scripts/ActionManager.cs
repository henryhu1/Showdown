using GameActions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActionManager : MonoBehaviour
{
    public static ActionManager Instance { get; private set; }

    private Dictionary<GameAction, bool> m_localAllowedActions;

    private Queue<GameAction> m_ActionQueue;

    [HideInInspector]
    public delegate void AddToQueueDelegateHandler(GameAction gameAction);
    [HideInInspector]
    public event AddToQueueDelegateHandler OnActionEnqueue;

    [HideInInspector]
    public delegate void ActionDequeueDelegateHandler(GameAction gameAction);
    [HideInInspector]
    public event ActionDequeueDelegateHandler OnActionDequeue;

    [HideInInspector]
    public delegate void DisallowedActionAttemptedDelegateHandler(GameAction disallowedAction);
    [HideInInspector]
    public event DisallowedActionAttemptedDelegateHandler OnDisallowedActionAttempted;

    [HideInInspector]
    public delegate void SubmitActionDelegateHandler();
    [HideInInspector]
    public event SubmitActionDelegateHandler OnSubmitAction;

    //[HideInInspector]
    //public delegate void AllowedToAttackDelegateHandler();
    //[HideInInspector]
    //public event AllowedToAttackDelegateHandler OnAllowedToAttack;

    //[HideInInspector]
    //public delegate void NotAllowedToAttackDelegateHandler();
    //[HideInInspector]
    //public event NotAllowedToAttackDelegateHandler OnNotAllowedToAttack;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
    }

    private void Start()
    {
        m_ActionQueue = new Queue<GameAction>();

        m_localAllowedActions = new();
        foreach (GameAction action in Enum.GetValues(typeof(GameAction)).Cast<GameAction>())
        {
            m_localAllowedActions.Add(action, false);
        }

        GameManager.Instance.OnPlayerGoldChange += GameManager_PlayerGoldChange;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnPlayerGoldChange -= GameManager_PlayerGoldChange;
    }

    private void GameManager_PlayerGoldChange(int newAmount)
    {
        SetLocalAllowedActions(newAmount);
    }

    public void SetLocalAllowedActions(int goldTotal)
    {
        Dictionary<GameAction, bool> playable = new();
        foreach (GameAction action in Enum.GetValues(typeof(GameAction)).Cast<GameAction>())
        {
            bool isActionAllowed = goldTotal + ActionLogic.GetGoldChange(action) >= 0;
            playable.Add(action, isActionAllowed);
            // TODO: if more objects or classes need to know when actions become playable/unplayable, make this an event
            ActionButtonsGroup.Instance.SetActionPlayable(action, isActionAllowed);
        }
        m_localAllowedActions = playable;

        //int attackCost = ActionLogic.GetGoldChange(ActionType.Attack);
        //bool attackEnabled = IsActionAllowed(ActionType.Attack);
        //if (m_localAllowedActions[ActionType.Attack] && !attackEnabled)
        //{
        //    OnAllowedToAttack?.Invoke();
        //}
        //else if (!m_localAllowedActions[ActionType.Attack] && attackEnabled)
        //{
        //    OnNotAllowedToAttack?.Invoke();
        //}
    }

    public bool IsActionAllowed(GameAction gameAction)
    {
        return m_localAllowedActions[gameAction];
    }

    public void EnqueueAction(GameAction gameAction, bool force = true)
    {
        Debug.LogFormat("Trying to enqueue action {0}", gameAction);
        // if (m_TickCoroutine == null) return; // TODO: make sure this won't break things

        if (m_ActionQueue.Count > 0 && !force)
        {
            Debug.LogFormat("Could not enqueue action {0} as there was already an action queued", gameAction);
            return;
        }

        if (IsActionAllowed(gameAction))
        {
            Debug.LogFormat("Enqueued action {0}", gameAction);
            m_ActionQueue.Clear();
            m_ActionQueue.Enqueue(gameAction);
            OnActionEnqueue?.Invoke(gameAction);
        }
        else
        {
            Debug.LogFormat("Action {0} is not allowed", gameAction);
            OnDisallowedActionAttempted?.Invoke(gameAction);
        }
    }

    public void SubmitAction()
    {
        GameAction selectedAction = GameAction.Block;
        if (m_ActionQueue.Count > 0)
        {
            selectedAction = m_ActionQueue.Dequeue();
            OnActionDequeue?.Invoke(selectedAction);
        }
        OnSubmitAction?.Invoke();
        Debug.LogFormat("action to submit: {0}", selectedAction);
        GameManager.Instance.SendActionServerRpc(selectedAction);
    }
}
