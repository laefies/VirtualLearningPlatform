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
    [SerializeField] private GameObject _hostIndicator;

    public void SetPlayer(Player player, bool isHostEntry) {
        this.player = player;

        // Show the player name
        _playerNameTextfield.text = player.Data["PlayerName"].Value;

        // Show a crown icon on the host entry to regular members of the lobby,
        // and lobby management options to the host
        _hostIndicator.SetActive(isHostEntry);
        _lobbyHostOptions.SetActive(LobbyManager.Instance.IsHost && !isHostEntry);
    }

    public void SetColor(Color mainColor, Color secondaryColor) {
        // Main color on the body
        gameObject.GetComponent<Image>().color = mainColor;

        // Accent color for buttons and decorations
        _hostIndicator.GetComponent<Image>().color  = secondaryColor;
        _transferButton.color = secondaryColor;
        _kickButton.color     = secondaryColor;
    }

    public async void HandleTransferOwnershipClick() {
        LobbyManager.Instance.TransferHostAsync(player.Id);
    }

    public async void HandleKickPlayerClick() {
        await LobbyManager.Instance.KickPlayerAsync(player.Id);
    }
}