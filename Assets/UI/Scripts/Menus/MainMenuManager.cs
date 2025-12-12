using System;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

/// <summary>
/// Orchestrates the main menu flow and manages panel visibility.
/// </summary>
public class MainMenuManager : FollowPlayerUI
{
    [Header("Panel Objects")]
    [SerializeField] private GameObject lobbyBrowserPanel;
    [SerializeField] private GameObject lobbyDetailsPanel;

    private LobbyManager LobbyManager => LobbyManager.Instance;

    protected override void Start() {
        base.Start();        
        UpdatePanelVisibility();
    }

    protected override void OnEnable()  { 
        base.OnEnable();        
        SubscribeToEvents(); 
    }

    protected override void OnDisable() { 
        base.OnDisable();        
        UnsubscribeFromEvents(); 
    }

    private void SubscribeToEvents()
    {
        if (LobbyManager == null) return;

        LobbyManager.OnLobbyJoined += HandleLobbyJoined;
        LobbyManager.OnLobbyLeft   += HandleLobbyLeft;
    }

    private void UnsubscribeFromEvents()
    {
        if (LobbyManager == null) return;
        
        LobbyManager.OnLobbyJoined -= HandleLobbyJoined;
        LobbyManager.OnLobbyLeft   -= HandleLobbyLeft;
    }

    private void HandleLobbyJoined(Lobby lobby) { UpdatePanelVisibility(); }
    private void HandleLobbyLeft() { UpdatePanelVisibility(); }

    private void UpdatePanelVisibility()
    {
        if (LobbyManager == null) return;

        bool inLobby = LobbyManager.IsInLobby;

        lobbyBrowserPanel?.SetActive(!inLobby);
        lobbyDetailsPanel?.SetActive( inLobby);
    }
}