using System;
using UnityEngine;

/// <summary>
/// Orchestrates the main menu flow and manages panel visibility.
/// </summary>
public class MainMenuManager : FollowPlayerUI
{
    [Header("Panel Objects")]
    [SerializeField] private GameObject lobbyBrowserPanel;
    [SerializeField] private GameObject lobbyDetailsPanel;

    private LobbyManager LobbyManager => LobbyManager.Instance;

    protected override void Start()
    {
        base.Start();        
        UpdatePanelVisibility();
    }

    private void OnEnable() { SubscribeToEvents(); }
    private void OnDisable() { UnsubscribeFromEvents(); }

    private void SubscribeToEvents()
    {
        if (LobbyManager == null) return;

        LobbyManager.OnJoinedLobby += HandleJoinedLobby;
        LobbyManager.OnLeftLobby   += HandleLeftLobby;
    }

    private void UnsubscribeFromEvents()
    {
        if (LobbyManager == null) return;
        
        LobbyManager.OnJoinedLobby -= HandleJoinedLobby;
        LobbyManager.OnLeftLobby   -= HandleLeftLobby;
    }


    private void HandleJoinedLobby(object sender, LobbyManager.LobbyEventArgs e) { UpdatePanelVisibility(); }
    private void HandleLeftLobby(object sender, EventArgs e) { UpdatePanelVisibility(); }

    private void UpdatePanelVisibility()
    {
        if (LobbyManager == null) return;

        bool inLobby = LobbyManager.IsPlayerInLobby();

        lobbyBrowserPanel?.SetActive(!inLobby);
        lobbyDetailsPanel?.SetActive( inLobby);
    }
}