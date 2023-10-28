using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance {  get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public async Task<string> CreateRelay()
    {
        try
        {
            // Set max player (guest) connection to 1, so host + guest = 2
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);

            // Get join code to pass to clients and let them join the server we allocated
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);

            // Setting connection option for the server
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

            // Use ip/port from Unity Transport so we don't have to set everything manually 
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            // Join the game as host
            NetworkManager.Singleton.StartHost();

            // Return join code to LobbyManager.cs StartGame()
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }

    public async void JoinRelay(string joinCode_in)
    {
        try
        {
            Debug.Log($"Joining relay with code: {joinCode_in}");
            // Join the same allocation with host by using join code
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode_in);

            // Setting connection type to server
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            // Use ip/port from Unity Transport so we don't have to set everything manually 
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            // Join as client
            NetworkManager.Singleton.StartClient();
        }
        catch(RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
