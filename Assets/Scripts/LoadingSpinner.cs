using UnityEngine;

public class LoadingSpinner : MonoBehaviour
{
    [SerializeField] private ParticleSystem m_loadingParticles;

    private void OnEnable()
    {
        LobbyManager.Instance.OnSearchForGame += LobbyManager_SearchForGame;
        LobbyManager.Instance.OnSearchForGameFailed += LobbyManager_SearchForGameFailed;
        LobbyManager.Instance.OnCancelSearchForGame += LobbyManager_CancelSearchForGame;
        LobbyManager.Instance.OnPlayerLeftLobby += LobbyManager_PlayerLeftLobby;
    }

    private void OnDisable()
    {
        LobbyManager.Instance.OnSearchForGame -= LobbyManager_SearchForGame;
        LobbyManager.Instance.OnSearchForGameFailed -= LobbyManager_SearchForGameFailed;
        LobbyManager.Instance.OnCancelSearchForGame -= LobbyManager_CancelSearchForGame;
        LobbyManager.Instance.OnPlayerLeftLobby -= LobbyManager_PlayerLeftLobby;
    }

    private void LobbyManager_SearchForGame()
    {
        m_loadingParticles.Play();
    }

    private void LobbyManager_SearchForGameFailed()
    {
        m_loadingParticles.Stop();
        m_loadingParticles.Clear();
    }

    private void LobbyManager_CancelSearchForGame()
    {
        m_loadingParticles.Stop();
        m_loadingParticles.Clear();
    }

    private void LobbyManager_PlayerLeftLobby()
    {
        m_loadingParticles.Stop();
        m_loadingParticles.Clear();
    }
}
