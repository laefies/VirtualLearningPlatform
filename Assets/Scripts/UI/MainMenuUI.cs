using System.Collections.Generic;
using System.Collections;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class MainMenuUI : BodyLockedUI
{
    void Start() {
        // Call base initialization
        base.Start();

        // Subscribe to Lobby related events
        LobbyManager.Instance.OnLobbyListChanged += UpdateLobbyList;
        LobbyManager.Instance.OnJoinedLobby      += ShowJoinedLobby;

        // Obtain starting list of existing lobbies
        LobbyManager.Instance.RefreshLobbyList();
    }

    void OnDestroy() {
        // Unsubscribe from Lobby related events
        LobbyManager.Instance.OnLobbyListChanged -= UpdateLobbyList;
        LobbyManager.Instance.OnJoinedLobby      -= ShowJoinedLobby;
    }
    
    /*
     * --- HANDLING LOBBY RELATED EVENTS ---
     */

    // Visually updates the list of lobbies the player can join
    void UpdateLobbyList(object sender, LobbyManager.LobbyListChangedEventArgs e) {
        Debug.Log("Updated Lobby List:");
        foreach (Lobby lobby in e.lobbyList) {
            Debug.Log($"Lobby Name: {lobby.Name}, Players: {lobby.Players.Count}/{lobby.MaxPlayers}, ID: {lobby.Id}");
        }
    }

    // Enables visualization of information regarding joined lobby
    void ShowJoinedLobby(object sender, LobbyManager.LobbyEventArgs e) {
        LobbyManager.Instance.RefreshLobbyList();
    } 

    /*
     * --- METHODS CALLED BY UI COMPONENTS ---
     */

    // Called by clicking the "refresh" button, to update lobby list
    public void RefreshLobbies() {
        LobbyManager.Instance.RefreshLobbyList();
    }

    // Called by clicking the "+" button, to create a new lobby
    public async void CreateLobby() {
        await LobbyManager.Instance.CreateLobby("Lobby" + UnityEngine.Random.Range(10, 99));
    }

}