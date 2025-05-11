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

    public void SetLobby(Lobby lobby) {
        this.lobby = lobby;

        _lobbyNameTextfield.text  = lobby.Name;
        _playerInfoTextfield.text = $"{lobby.Players.Count}/{lobby.MaxPlayers} Players";
    }

    public void SetColor(Color mainColor) {
        gameObject.GetComponent<Image>().color = mainColor;
    }

    public async void HandleLobbyClick() {
        await LobbyManager.Instance.JoinLobby(lobby);
    }

    async void Update() {
        if (Input.GetKeyDown(KeyCode.J)) HandleLobbyClick();
    }
}
