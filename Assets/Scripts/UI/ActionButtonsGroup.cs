using System.Collections.Generic;
using UnityEngine;
using GameActions;
using UnityEngine.UI;
using DG.Tweening;

public class ActionButtonsGroup : MonoBehaviour
{
    public static ActionButtonsGroup Instance { get; private set; }

    public static Color s_enabledButtonColor = new(1, 1, 1, 1);
    public static Color s_disabledButtonColor = new(1, 1, 1, 0.3f);

    [SerializeField] private ActionButton m_attackButton;
    [SerializeField] private ActionButton m_blockButton;
    [SerializeField] private ActionButton m_collectButton;
    [SerializeField] private Image m_panel;
    [SerializeField] private RectTransform m_buttonGroup;

    private Vector3 m_buttonGroupOriginalPos;

    private List<ActionButton> m_actionButtonsList;
    private Dictionary<GameAction, ActionButton> m_actionToButton;

    private Sequence m_highlightActions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        m_attackButton.SetButtonText(ActionLogic.GetGoldChange(GameAction.Attack).ToString());
        m_collectButton.SetButtonText(ActionLogic.GetGoldChange(GameAction.Collect).ToString());
        m_buttonGroupOriginalPos = m_buttonGroup.localPosition;
    }

    private void Start()
    {
        GameManager.Instance.OnPlayerGoldChange += GameManager_PlayerGoldChange;
        GameManager.Instance.OnEnableActionsToBePlayed += GameManager_EnableActionsToBePlayed;
        GameManager.Instance.OnDisableActionsToBePlayed += GameManager_DisableActionsToBePlayed;
        GameManager.Instance.OnGameFinished += GameManager_GameFinished;
        //ActionManager.Instance.OnAllowedToAttack += ActionManager_AllowedToAttack;
        //ActionManager.Instance.OnNotAllowedToAttack += ActionManager_NotAllowedToAttack;

        m_attackButton.AddOnClickListener(ActionButtonClicked, GameAction.Attack);
        m_blockButton.AddOnClickListener(ActionButtonClicked, GameAction.Block);
        m_collectButton.AddOnClickListener(ActionButtonClicked, GameAction.Collect);

        m_actionButtonsList = new List<ActionButton> { m_attackButton, m_blockButton, m_collectButton };
        m_actionToButton = new Dictionary<GameAction, ActionButton>
        {
            { GameAction.Attack, m_attackButton },
            { GameAction.Block, m_blockButton },
            { GameAction.Collect, m_collectButton },
        };

        Tween appear = m_panel.DOFade(0.5f, GameManager.k_TimePerTick / 2);
        Tween fade = m_panel.DOFade(0, GameManager.k_TimePerTick / 2);
        m_highlightActions = DOTween.Sequence().Append(appear).Append(fade).SetLoops(-1).Pause();

        DisableActions();
    }

    private void OnDisable()
    {
        GameManager.Instance.OnPlayerGoldChange -= GameManager_PlayerGoldChange;
        GameManager.Instance.OnEnableActionsToBePlayed -= GameManager_EnableActionsToBePlayed;
        GameManager.Instance.OnDisableActionsToBePlayed -= GameManager_DisableActionsToBePlayed;
        GameManager.Instance.OnGameFinished -= GameManager_GameFinished;
        //ActionManager.Instance.OnAllowedToAttack -= ActionManager_AllowedToAttack;
        //ActionManager.Instance.OnNotAllowedToAttack -= ActionManager_NotAllowedToAttack;
        m_attackButton.RemoveOnClickListeners();
        m_blockButton.RemoveOnClickListeners();
        m_collectButton.RemoveOnClickListeners();
    }

    public void RegisterNetworkCallbacks()
    {
        GameManager.Instance.m_IsSuddenDeath.OnValueChanged += OnIsSuddenDeathChanged;
    }

    public void UnregisterNetworkCallbacks()
    {
        GameManager.Instance.m_IsSuddenDeath.OnValueChanged -= OnIsSuddenDeathChanged;
    }

    public void OnIsSuddenDeathChanged(bool previousValue, bool currentValue)
    {
        if (currentValue)
        {
            m_highlightActions.Restart();
        }
        else
        {
            m_highlightActions.Pause();
        }
    }

    private void DisableActions()
    {
        foreach (ActionButton actionButton in m_actionButtonsList)
        {
            actionButton.SetEnabled(false);
            actionButton.SetColor(s_disabledButtonColor);
        }
    }

    private void GameManager_PlayerGoldChange(int newAmount)
    {
        //foreach (KeyValuePair<GameAction, ActionButton> actionToButton in m_actionToButton)
        //{
        //    bool isActionAllowed = ActionManager.Instance.IsActionAllowed(actionToButton.Key);
        //    actionToButton.Value.SetEnabled(isActionAllowed);
        //    Color buttonColor = isActionAllowed ? s_enabledButtonColor : s_disabledButtonColor;
        //    actionToButton.Value.SetColor(buttonColor);
        //}
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
        foreach (KeyValuePair<GameAction, ActionButton> actionToButton in m_actionToButton)
        {
            bool isActionAllowed = ActionManager.Instance.IsActionAllowed(actionToButton.Key);
            SetButtonAllowed(actionToButton.Value, isActionAllowed);
        }
    }

    private void GameManager_DisableActionsToBePlayed()
    {
        DisableActions();
    }

    private void GameManager_GameFinished()
    {
        m_buttonGroup.DOMoveY(-1000, 1f).SetEase(Ease.OutSine);
    }

    public void SetActionPlayable(GameAction gameAction, bool isActionAllowed)
    {
        if (m_actionToButton.TryGetValue(gameAction, out ActionButton button))
        {
            SetButtonAllowed(button, isActionAllowed);
        }
    }

    private void SetButtonAllowed(ActionButton button, bool isActionAllowed)
    {
        button.SetEnabled(isActionAllowed);
        Color buttonColor = isActionAllowed ? s_enabledButtonColor : s_disabledButtonColor;
        button.SetColor(buttonColor);
    }

    private void ActionButtonClicked(GameAction action)
    {
        ActionManager.Instance.EnqueueAction(action);
    }

    public RectTransform GetActionButtonTransform(GameAction action)
    {
        return m_actionToButton[action].getButtonRectTransform();
    }
}
