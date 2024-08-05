using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkButtons : MonoBehaviour
{
    [SerializeField] private Button m_ClientButton;
    [SerializeField] private Button m_HostButton;

    private void Start()
    {
        m_ClientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });

        m_HostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });
    }
}
