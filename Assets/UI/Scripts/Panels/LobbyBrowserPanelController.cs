using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Unity.Services.Lobbies.Models;

/// <summary>
/// Controls the lobby browser panel, showcasing available lobbies to join.
/// </summary>
public class LobbyBrowserPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button refreshLobbiesButton;
    [SerializeField] private Transform lobbyListContainer;
    [SerializeField] private GameObject lobbyItemPrefab;

    private LobbyManager LobbyManager => LobbyManager.Instance;
    private readonly List<GameObject> activeLobbyItems = new List<GameObject>();

    private void OnEnable()
    {
        if (LobbyManager != null)
            LobbyManager.OnLobbyListChanged += HandleLobbyListChanged;

        createLobbyButton?.onClick.AddListener(OnCreateLobbyClicked);
        refreshLobbiesButton?.onClick.AddListener(OnRefreshLobbiesClicked);

        LobbyManager?.RefreshLobbyList();
    }

    private void OnDisable()
    {
        if (LobbyManager != null)
            LobbyManager.OnLobbyListChanged -= HandleLobbyListChanged;

        createLobbyButton?.onClick.RemoveListener(OnCreateLobbyClicked);
        refreshLobbiesButton?.onClick.RemoveListener(OnRefreshLobbiesClicked);

        ClearLobbyList();
    }

    private void OnDestroy() { ClearLobbyList(); }

    private void HandleLobbyListChanged(object sender, LobbyManager.LobbyListChangedEventArgs e)
    {
        DisplayLobbies(e.lobbyList);
    }

    private void DisplayLobbies(List<Lobby> lobbies)
    {
        ClearLobbyList();

        foreach (Lobby lobby in lobbies) {
            if (lobby == null) continue;

            GameObject lobbyItemObject = Instantiate(lobbyItemPrefab, lobbyListContainer);
            activeLobbyItems.Add(lobbyItemObject);
            
            if (lobbyItemObject.TryGetComponent<LobbyListItemUI>(out LobbyListItemUI lobbyItem)) 
                lobbyItem.SetLobby(lobby);
        }
    }

    private void ClearLobbyList()
    {
        foreach (GameObject item in activeLobbyItems) 
            if (item) Destroy(item);

        activeLobbyItems.Clear();
    }

    private async void OnCreateLobbyClicked() { await LobbyManager?.CreateLobby(); }
    private async void OnRefreshLobbiesClicked() { LobbyManager?.RefreshLobbyList(); }

}
