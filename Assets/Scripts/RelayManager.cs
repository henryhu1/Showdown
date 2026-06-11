using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != this && Instance != null)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
    }

    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(GameManager.k_PlayersPerGame);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            RelayServerData relayServerData = new(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            if (!NetworkManager.Singleton.StartHost())
            {
                throw new Exception("Failed to start host");
            }

            return joinCode;
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to create relay: {e.Message}");
        }
    }

    public async void JoinRelay(string joinCode)
    {
        if (joinCode == null) { return; }
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            // NetworkManager.Singleton.NetworkConfig.ConnectionData = StreamUtils.WritePlayerNameId(GameManager.Instance.LocalPlayerName, GameManager.Instance.LocalPlayerId);

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void LeaveRelay()
    {
        NetworkManager.Singleton.Shutdown();
    }
}
