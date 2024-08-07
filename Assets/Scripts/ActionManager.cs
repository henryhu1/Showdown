using GameActions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActionManager : MonoBehaviour
{
    public static ActionManager Instance { get; private set; }

    private Dictionary<ActionType, bool> m_localAllowedActions;

    private Queue<ActionType> m_ActionQueue;

    [HideInInspector]
    public delegate void AddToQueueDelegateHandler(ActionType gameAction);
    [HideInInspector]
    public event AddToQueueDelegateHandler OnAddToQueue;

    [HideInInspector]
    public delegate void DisallowedActionAttemptedDelegateHandler(ActionType disallowedAction);
    [HideInInspector]
    public event DisallowedActionAttemptedDelegateHandler OnDisallowedActionAttempted;

    [HideInInspector]
    public delegate void SubmitActionDelegateHandler();
    [HideInInspector]
    public event SubmitActionDelegateHandler OnSubmitAction;

    [HideInInspector]
    public delegate void ActionDequeueDelegateHandler();
    [HideInInspector]
    public event ActionDequeueDelegateHandler OnActionDequeue;

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
            Destroy(Instance);
        }
        Instance = this;
    }

    private void Start()
    {
        m_ActionQueue = new Queue<ActionType>();

        m_localAllowedActions = new();
        foreach (ActionType action in Enum.GetValues(typeof(ActionType)).Cast<ActionType>())
        {
            m_localAllowedActions.Add(action, false);
        }
    }

    public void SetLocalAllowedActions(int goldTotal)
    {
        Dictionary<ActionType, bool> playable = new();
        foreach (ActionType action in Enum.GetValues(typeof(ActionType)).Cast<ActionType>())
        {
            playable.Add(action, goldTotal + ActionLogic.GetGoldChange(action) >= 0);
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

    public bool IsActionAllowed(ActionType gameAction)
    {
        return m_localAllowedActions[gameAction];
    }

    public void EnqueueAction(ActionType gameAction, bool force = true)
    {
        Debug.LogFormat("Trying to enqueue action {0}", gameAction);
        // if (m_TickCoroutine == null) return; // TODO: make sure this won't break things

        if (m_ActionQueue.Count > 0 && !force)
        {
            Debug.LogFormat("Could not enqueue action {0} as ther was already an action queued", gameAction);
            return;
        }

        if (IsActionAllowed(gameAction))
        {
            Debug.LogFormat("Enqueued action {0}", gameAction);
            m_ActionQueue.Clear();
            m_ActionQueue.Enqueue(gameAction);
            OnAddToQueue?.Invoke(gameAction);
        }
        else
        {
            Debug.LogFormat("Action {0} is not allowed", gameAction);
            OnDisallowedActionAttempted?.Invoke(gameAction);
        }
    }

    public void SubmitAction()
    {
        ActionType selectedAction = ActionType.Block;
        if (m_ActionQueue.Count > 0)
        {
            selectedAction = m_ActionQueue.Dequeue();
            OnActionDequeue?.Invoke();
        }
        OnSubmitAction?.Invoke();
        Debug.LogFormat("action to submit: {0}", selectedAction);
        GameManager.Instance.SendActionServerRpc(selectedAction);
    }
}
