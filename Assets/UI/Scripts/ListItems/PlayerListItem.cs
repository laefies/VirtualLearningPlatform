using UnityEngine;
using Nova;
using Unity.Services.Lobbies.Models;

public class PlayerListItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextBlock playerNameText;
    [SerializeField] private Label hostLabel;
    [SerializeField] private UIBlock lobbyHostOptions;
    [SerializeField] private Button transferHostButton;
    [SerializeField] private Button kickPlayerButton;

    private Player player;
    private LobbyManager LobbyManager => LobbyManager.Instance;

    private void Awake()
    { 
        transferHostButton?.AddListener(OnTransferHostClicked);
        kickPlayerButton?.AddListener(OnKickPlayerClicked);
    }

    private void OnDestroy() 
    {
        transferHostButton?.RemoveListener(OnTransferHostClicked);
        kickPlayerButton?.RemoveListener(OnKickPlayerClicked);
    }

    public void SetPlayer(Player playerData)
    {
        player = playerData;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (player == null || LobbyManager == null) return;

        bool hasPlayerName = player.Data.TryGetValue("PlayerName", out var playerNameData);
        string playerName = hasPlayerName ? playerNameData.Value : "Unknown Player";
        
        bool isHostEntry = LobbyManager.IsPlayerHost(player.Id);
        bool isLocalHost = LobbyManager.IsHost;

        playerNameText.Text = playerName;

        LabelStyle labelStyle = hostLabel.ApplyStyle(isHostEntry ? "Owner" : "Member");
        if (labelStyle != null) playerNameText.Color = labelStyle.mainColor;

        hostLabel.gameObject.SetActive(isHostEntry);
        lobbyHostOptions.gameObject.SetActive(isLocalHost && !isHostEntry);
    }

    private async void OnTransferHostClicked()
    {
        if (player == null) return;

        await LobbyManager?.TransferHostAsync(player.Id);
    }

    private async void OnKickPlayerClicked()
    {
        if (player == null) return;

        await LobbyManager?.KickPlayerAsync(player.Id);
    }
}
