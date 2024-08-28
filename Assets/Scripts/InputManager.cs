using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    [SerializeField] private float m_ShakeThreshold = 2.0f;

    private PlayerControls m_playerControls;

    private Camera m_mainCamera;

    [HideInInspector]
    public delegate void StartTouchDelegateHandler(Vector2 position);
    [HideInInspector]
    public event StartTouchDelegateHandler OnStartTouch;

    [HideInInspector]
    public delegate void EndTouchDelegateHandler(Vector2 position);
    [HideInInspector]
    public event EndTouchDelegateHandler OnEndTouch;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        m_playerControls = new PlayerControls();
        m_mainCamera = Camera.main;
    }

    private void Start()
    {
        m_playerControls.Touch.PrimaryContact.started += PrimaryContact_started;
        m_playerControls.Touch.PrimaryContact.canceled += PrimaryContact_canceled;
    }

    private void OnEnable()
    {
        m_playerControls.Enable();
    }

    private void OnDisable()
    {
        m_playerControls.Disable();

        m_playerControls.Touch.PrimaryContact.started -= PrimaryContact_started;
        m_playerControls.Touch.PrimaryContact.canceled -= PrimaryContact_canceled;
    }

    private void PrimaryContact_started(InputAction.CallbackContext context)
    {
        if (!SettingsUI.Instance.GetIsSettingsOpen() && !GameManager.Instance.m_IsSuddenDeath.Value)
        {
            OnStartTouch?.Invoke(GetPrimaryPosition());
        }
    }

    private void PrimaryContact_canceled(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.m_IsSuddenDeath.Value)
        {
            OnEndTouch?.Invoke(GetPrimaryPosition());
        }
    }

    private void Update()
    {
        if (Input.acceleration.sqrMagnitude > Mathf.Pow(m_ShakeThreshold, 2))
        {
            Debug.Log("shake");
        }
    }

    public Vector2 GetPrimaryPosition()
    {
        return Utils.ScreenToWorld(m_mainCamera, m_playerControls.Touch.PrimaryPosition.ReadValue<Vector2>());
    }
}
