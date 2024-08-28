using UnityEngine;

public class LoadingSpinner : MonoBehaviour
{
    [SerializeField] private ParticleSystem m_loadingParticles;

    private void OnEnable()
    {
        LobbyManager.Instance.OnSearchForGame += LobbyManager_SearchForGame;
        LobbyManager.Instance.OnCancelSearchForGame += LobbyManager_CancelSearchForGame;
    }

    private void OnDisable()
    {
        LobbyManager.Instance.OnSearchForGame -= LobbyManager_SearchForGame;
        LobbyManager.Instance.OnCancelSearchForGame -= LobbyManager_CancelSearchForGame;
    }

    private void LobbyManager_SearchForGame()
    {
        m_loadingParticles.Play();
    }

    private void LobbyManager_CancelSearchForGame()
    {
        m_loadingParticles.Stop();
        m_loadingParticles.Clear();
    }
}
