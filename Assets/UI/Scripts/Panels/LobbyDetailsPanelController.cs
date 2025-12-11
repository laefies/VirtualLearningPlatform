using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using UnityEngine.Events;
using Unity.Services.Lobbies.Models;
using System.Linq;

/// <summary>
/// Controls the lobby details panel showing current lobby members and data.
/// </summary>
public class LobbyDetailsPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerItemPrefab;

    private LobbyManager LobbyManager => LobbyManager.Instance;
    private readonly List<GameObject> activePlayerItems = new List<GameObject>();

    private async void OnEnable()
    {
        if (LobbyManager != null)
        {  
            DisplayPlayers(LobbyManager.CurrentLobby?.Players);
            LobbyManager.OnLobbyPlayersChanged += HandlePlayersChanged;
        }

        leaveLobbyButton?.AddListener(OnLeaveLobbyClicked);

    }

    private void OnDisable()
    {
        if (LobbyManager != null)
            LobbyManager.OnLobbyPlayersChanged -= HandlePlayersChanged;

        leaveLobbyButton?.RemoveListener(OnLeaveLobbyClicked);

        ClearPlayerList();
    }

    private void OnDestroy() { ClearPlayerList(); }

    private void HandlePlayersChanged(List<Player> players)
    {
        DisplayPlayers(players);
    }

    private void DisplayPlayers(List<Player> players)
    {
        if (players == null) return;

        ClearPlayerList();

        players = players.OrderByDescending(p => p.Id == LobbyManager.CurrentLobby.HostId).ToList();
        foreach (Player player in players) {
            if (player == null) continue;

            GameObject playerItemObject = Instantiate(playerItemPrefab, playerListContainer);
            
            if (playerItemObject.TryGetComponent<PlayerListItem>(out PlayerListItem playerItem)) {
                playerItem.SetPlayer(player);
                activePlayerItems.Add(playerItemObject);
            }
        }
    }

    private void ClearPlayerList()
    {
        foreach (GameObject item in activePlayerItems) 
            if (item) Destroy(item);

        activePlayerItems.Clear();
    }

    private async void OnLeaveLobbyClicked() { LobbyManager?.LeaveLobbyAsync(); }
}