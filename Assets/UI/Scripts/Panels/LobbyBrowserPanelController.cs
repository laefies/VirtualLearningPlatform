using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using UnityEngine.Events;
using Unity.Services.Lobbies.Models;

/// <summary>
/// Controls the lobby browser panel, showcasing available lobbies to join.
/// </summary>
public class LobbyBrowserPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button refreshLobbyListButton;
    [SerializeField] private Transform lobbyListContainer;
    [SerializeField] private GameObject lobbyItemPrefab;

    private LobbyManager LobbyManager => LobbyManager.Instance;
    private readonly List<GameObject> activeLobbyItems = new List<GameObject>();

    private async void OnEnable()
    {
        if (LobbyManager != null)
            LobbyManager.OnLobbyListRefreshed += HandleLobbyListRefreshed;

        createLobbyButton?.AddListener(OnCreateLobbyClicked);
        refreshLobbyListButton?.AddListener(OnRefreshLobbyListClicked);
    }

    private async void Start()
    {
        await LobbyManager?.RefreshLobbyListAsync();
    }

    private void OnDisable()
    {
        if (LobbyManager != null)
            LobbyManager.OnLobbyListRefreshed -= HandleLobbyListRefreshed;

        createLobbyButton?.RemoveListener(OnCreateLobbyClicked);
        refreshLobbyListButton?.RemoveListener(OnRefreshLobbyListClicked);

        ClearLobbyList();
    }

    private void OnDestroy() { ClearLobbyList(); }

    private void HandleLobbyListRefreshed(List<Lobby> lobbies)
    {
        DisplayLobbies(lobbies);
    }

    private void DisplayLobbies(List<Lobby> lobbies)
    {
        if (lobbies == null) return;
        
        ClearLobbyList();

        foreach (Lobby lobby in lobbies) {
            if (lobby == null) continue;

            GameObject lobbyItemObject = Instantiate(lobbyItemPrefab, lobbyListContainer);
            
            if (lobbyItemObject.TryGetComponent<LobbyListItem>(out LobbyListItem lobbyItem)) {
                lobbyItem.SetLobby(lobby);
                activeLobbyItems.Add(lobbyItemObject);
            }
        }
    }

    private void ClearLobbyList()
    {
        foreach (GameObject item in activeLobbyItems) 
            if (item) Destroy(item);

        activeLobbyItems.Clear();
    }

    private async void OnCreateLobbyClicked() { 
        await LobbyManager?.CreateLobbyAsync(PlayerManager.Instance.PlayerName); 
    }

    private async void OnRefreshLobbyListClicked() { LobbyManager?.RefreshLobbyListAsync(); }
}