using System.Collections;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyListItemUI : MonoBehaviour
{
    private Lobby lobby;

    [SerializeField] private TMPro.TextMeshProUGUI _lobbyNameTextfield;
    [SerializeField] private TMPro.TextMeshProUGUI _playerInfoTextfield;
    [SerializeField] private TMPro.TextMeshProUGUI _lobbyStatusTextfield;

    [SerializeField] private Image _bodyBackground;
    [SerializeField] private Image _statusBackground;

    public void SetLobby(Lobby lobby) {        
        this.lobby = lobby;

        _lobbyNameTextfield.text   = lobby.Name;
        _playerInfoTextfield.text  = $"{lobby.Players.Count}/{lobby.MaxPlayers} Players";
        _lobbyStatusTextfield.text = lobby.Data[LobbyManager.KEY_LOBBY_STATE].Value;
    }

    public void SetColor(Color mainColor, Color secondaryColor) {
        // Main color for background
        _bodyBackground.color   = mainColor;

        // Accent color for status
        _statusBackground.color = secondaryColor;
    }

    public async void HandleLobbyClick() {
        if (lobby == null) return;
        await LobbyManager.Instance.JoinLobby(lobby);
    }

    async void Update() {
        if (Input.GetKeyDown(KeyCode.J)) HandleLobbyClick();
    }
}