using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SelectedActionBaseBehaviour : MonoBehaviour
{
    public virtual void Start()
    {
        GameManager.Instance.OnEnableActionsToBePlayed += GameManager_EnableActionsToBePlayed;
        GameManager.Instance.OnDisableActionsToBePlayed += GameManager_DisableActionsToBePlayed;
        GameManager.Instance.OnTickBeforeActionSubmit += GameManager_TickBeforeActionSubmit;
        ActionManager.Instance.OnAddToQueue += ActionManager_AddToQueue;
        ActionManager.Instance.OnSubmitAction += ActionManager_SubmitAction;
    }

    public virtual void OnDisable()
    {
        GameManager.Instance.OnEnableActionsToBePlayed -= GameManager_EnableActionsToBePlayed;
        GameManager.Instance.OnDisableActionsToBePlayed -= GameManager_DisableActionsToBePlayed;
        GameManager.Instance.OnTickBeforeActionSubmit -= GameManager_TickBeforeActionSubmit;
        ActionManager.Instance.OnAddToQueue -= ActionManager_AddToQueue;
        ActionManager.Instance.OnSubmitAction -= ActionManager_SubmitAction;
    }

    public abstract void GameManager_EnableActionsToBePlayed();

    public abstract void GameManager_DisableActionsToBePlayed();

    public abstract void GameManager_TickBeforeActionSubmit();

    public abstract void ActionManager_AddToQueue(GameAction gameAction);

    public abstract void ActionManager_SubmitAction();
}
