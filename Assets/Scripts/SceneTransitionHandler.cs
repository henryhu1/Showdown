using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionHandler : MonoBehaviour
{
    public static SceneTransitionHandler Instance { get; private set; }

    [HideInInspector]
    public delegate void ClientLoadedSceneDelegateHandler(ulong clientId);
    [HideInInspector]
    public event ClientLoadedSceneDelegateHandler OnClientLoadedScene;
    [HideInInspector]
    public delegate void AllClientsLoadedSceneDelegateHandler(SceneState inScene);
    [HideInInspector]
    public event AllClientsLoadedSceneDelegateHandler OnAllClientsLoadedScene;
    [HideInInspector]
    public delegate void SceneStateChangedDelegateHandler(SceneState newState);
    [HideInInspector]
    public event SceneStateChangedDelegateHandler OnSceneStateChanged;

    private int m_numberOfClientLoaded;

    public const string k_MainMenuScene = "StartScene";
    public const string k_InGameSceneName = "GameScene";


    private void Awake()
    {
        if (Instance != this && Instance != null)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
        DontDestroyOnLoad(this);
    }

    public void RegisterCallbacks()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
    }

    public void UnregisterCallbacks()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
    }

    private void SwitchScene(string sceneName)
    {
        if (NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.ShutdownInProgress)
        {
            m_numberOfClientLoaded = 0;
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadSceneAsync(sceneName);
        }
    }

    public void SwitchToMainMenuScene() { SwitchScene(k_MainMenuScene); }

    public void SwitchToGameScene() { SwitchScene(k_InGameSceneName); }

    private void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Debug.Log($"client #{clientId} has loaded scene {sceneName}");
        OnClientLoadedScene?.Invoke(clientId);
        m_numberOfClientLoaded += 1;
        if (m_numberOfClientLoaded == NetworkManager.Singleton.ConnectedClients.Count)
        {
            SceneState inScene = sceneName == k_InGameSceneName ? SceneState.GameScene : SceneState.StartScene;
            OnAllClientsLoadedScene?.Invoke(inScene);
        }
    }

    public bool AreAllClientsLoaded()
    {
        return m_numberOfClientLoaded == NetworkManager.Singleton.ConnectedClients.Count;
    }

    public void ExitAndLoadStartMenu()
    {
        OnClientLoadedScene = null;
        SceneManager.LoadScene(k_MainMenuScene);
    }
}
