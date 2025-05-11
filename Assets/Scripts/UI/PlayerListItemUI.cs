using System.Collections;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerListItemUI : MonoBehaviour
{
    private Player player;

    [SerializeField] private TMPro.TextMeshProUGUI _playerNameTextfield;

    [SerializeField] private Image _transferButton;
    [SerializeField] private Image _kickButton;

    [SerializeField] private GameObject _lobbyHostOptions;

    public void SetPlayer(Player player, bool isHostEntry) {
        this.player = player;

        // Show the player name
        _playerNameTextfield.text = player.Data["PlayerName"].Value;

        // If the player the item is presented to is the host, but the entry
        // doesn't refer to them, show host options 
        _lobbyHostOptions.SetActive(LobbyManager.Instance.IsLobbyHost() && !isHostEntry);
    }

    public void SetColor(Color mainColor, Color secondaryColor) {
        // Main color on the body
        gameObject.GetComponent<Image>().color = mainColor;

        // Accent color for buttons
        _transferButton.color = secondaryColor;
        _kickButton.color     = secondaryColor;
    }

    public async void HandleTransferOwnershipClick() {
        LobbyManager.Instance.MigrateLobbyHost(player.Id);
    }

    public async void HandleKickPlayerClick() {
        LobbyManager.Instance.RemoveFromLobby(player.Id);
    }
}