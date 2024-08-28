using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance { get; private set; }

    [SerializeField] private GameObject m_panel;
    private Button m_panelButton;
    [SerializeField] private Button m_settingsButton;
    [SerializeField] private VolumeControl m_timerVolume;
    [SerializeField] private VolumeControl m_effectVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        m_panel.SetActive(false);

        m_panelButton = m_panel.GetComponent<Button>();
    }

    private void Start()
    {
        m_panelButton.onClick.AddListener(() => { m_panel.SetActive(false); });
        m_settingsButton.onClick.AddListener(() => { m_panel.SetActive(!m_panel.activeSelf); });
    }

    private void OnDisable()
    {
        m_panelButton.onClick.RemoveAllListeners();
        m_settingsButton.onClick.RemoveAllListeners();
    }

    public bool GetIsSettingsOpen()
    {
        return m_panel.activeInHierarchy;
    }
}
