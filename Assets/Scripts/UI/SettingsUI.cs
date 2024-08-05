using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject m_panel;
    private Button m_panelButton;
    [SerializeField] private Button m_settingsButton;
    [SerializeField] private VolumeControl m_timerVolume;
    [SerializeField] private VolumeControl m_effectVolume;

    private void Awake()
    {
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
}
