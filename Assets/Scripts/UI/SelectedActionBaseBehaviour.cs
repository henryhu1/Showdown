using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SelectedActionBaseBehaviour : MonoBehaviour
{
    public virtual void Start()
    {
        GameManager.Instance.OnEnableActionsToBePlayed += GameManager_EnableActionsToBePlayed;
        GameManager.Instance.OnDisableActionsToBePlayed += GameManager_DisableActionsToBePlayed;
        GameManager.Instance.OnAddToQueue += GameManager_AddToQueue;
        GameManager.Instance.OnSubmitAction += GameManager_SubmitAction;
    }

    public virtual void OnDisable()
    {
        GameManager.Instance.OnEnableActionsToBePlayed -= GameManager_EnableActionsToBePlayed;
        GameManager.Instance.OnDisableActionsToBePlayed -= GameManager_DisableActionsToBePlayed;
        GameManager.Instance.OnAddToQueue -= GameManager_AddToQueue;
        GameManager.Instance.OnSubmitAction -= GameManager_SubmitAction;
    }

    public abstract void GameManager_EnableActionsToBePlayed();

    public abstract void GameManager_DisableActionsToBePlayed();

    public abstract void GameManager_AddToQueue(ActionType gameAction);

    public abstract void GameManager_SubmitAction();
}
