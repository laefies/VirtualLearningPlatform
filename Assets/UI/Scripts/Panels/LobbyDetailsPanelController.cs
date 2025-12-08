using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using NovaSamples.UIControls;
using UnityEngine.Events;
using Unity.Services.Lobbies.Models;

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
            LobbyManager.OnLobbyUpdated += HandleLobbyUpdated;

        leaveLobbyButton?.AddListener(OnLeaveLobbyClicked);
    }

    private void OnDisable()
    {
        if (LobbyManager != null)
            LobbyManager.OnLobbyUpdated -= HandleLobbyUpdated;

        leaveLobbyButton?.RemoveListener(OnLeaveLobbyClicked);

        ClearPlayerList();
    }

    private void OnDestroy() { ClearPlayerList(); }

    private void HandleLobbyUpdated(Lobby lobby)
    {
        DisplayPlayers(lobby.Players);
    }

    private void DisplayPlayers(List<Player> playerList)
    {
        ClearPlayerList();

        foreach (Player player in playerList) {
            if (player == null) continue;

            GameObject playerItemObject = Instantiate(playerItemPrefab, playerListContainer);
            activePlayerItems.Add(playerItemObject);
            
            if (playerItemObject.TryGetComponent<PlayerListItem>(out PlayerListItem playerItem))
                playerItem.SetPlayer(player);
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