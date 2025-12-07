using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;

/// <summary>
/// Controls the visual representation of a single lobby to be displayed in a list.
/// </summary>
public class LobbyListItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Label statusLabel;

    private Lobby lobby;
    private LobbyManager LobbyManager => LobbyManager.Instance;

    private void Awake()  { joinLobbyButton?.onClick.AddListener(OnItemClicked); }
    private void OnDestroy() { joinLobbyButton?.onClick.RemoveListener(OnItemClicked); }

    public void SetLobby(Lobby lobbyData)
    {
        lobby = lobbyData;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (lobby == null) return;

        int currentPlayers = lobby.Players?.Count ?? 0;
        int maxPlayers     = lobby.MaxPlayers;
        string lobbyStatus = LobbyManager.GetLobbyStatus(lobby);

        statusLabel?.ApplyStyle(lobbyStatus);

        if (lobbyNameText != null) 
            lobbyNameText.text = lobby.Name;

        if (playerCountText != null) 
            playerCountText.text = $"Players {currentPlayers}/{maxPlayers}";
        
        if (joinLobbyButton != null)
            joinLobbyButton.interactable = currentPlayers < maxPlayers;
    }

    private async void OnItemClicked()
    {
        if (joinLobbyButton == null || lobby == null) return;

        joinLobbyButton.interactable = false;

        bool joined = await LobbyManager?.JoinLobbyAsync(lobby, PlayerManager.Instance.PlayerName);
        if (joined) await LobbyManager?.RefreshLobbyListAsync();
    }
}