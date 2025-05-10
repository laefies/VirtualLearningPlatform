using System.Collections.Generic;
using System.Collections;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : BodyLockedUI
{
    [SerializeField] private Transform lobbyListContent; // Content panel where the lobbies are listed
    [SerializeField] private GameObject lobbyItemPrefab; // Prefab regarding an item of the lobby list

    private Color[] listColors = new Color[] {
        new Color(0.639f, 0.741f, 0.922f), // Light Blue
        new Color(0.945f, 0.808f, 0.620f), // Light Orange
        new Color(0.933f, 0.667f, 0.384f)  // Dark Orange
    };

    private List<GameObject> currentLobbyItems = new List<GameObject>();

    void Start() {
        // Call base initialization
        base.Start();

        // Subscribe to Lobby related events
        LobbyManager.Instance.OnLobbyListChanged += UpdateLobbyList;
        LobbyManager.Instance.OnJoinedLobby      += ShowJoinedLobby;

        // Authenticate into Unity Services
        LobbyManager.Instance.Authenticate();
    }

    void OnDestroy() {
        // Unsubscribe from Lobby related events
        LobbyManager.Instance.OnLobbyListChanged -= UpdateLobbyList;
        LobbyManager.Instance.OnJoinedLobby      -= ShowJoinedLobby;

        // Clean up any remaining lobby items
        ClearLobbyItems();
    }

    // TODO REMOVE AFTER TESTING
    void Update() {
        base.Update();
        if (Input.GetKeyDown(KeyCode.N)) CreateLobby();
        if (Input.GetKeyDown(KeyCode.R)) RefreshLobbies();
    }

    /*
     * --- HANDLING LOBBY RELATED EVENTS ---
     */

    // Visually updates the list of lobbies the player can join
    void UpdateLobbyList(object sender, LobbyManager.LobbyListChangedEventArgs e) {
        // Clear existing lobby items first
        ClearLobbyItems();
        
        // Create new lobby items for each lobby in the list
        for (int i = 0; i < e.lobbyList.Count; i++)
        {
            Lobby lobby = e.lobbyList[i];

            // Instantiate the lobby item prefab as a child of the content panel
            GameObject lobbyItemGO = Instantiate(lobbyItemPrefab, lobbyListContent);
            
            // Assign the lobby data to the list item
            LobbyListItemUI lobbyItem = lobbyItemGO.GetComponent<LobbyListItemUI>();
            lobbyItem.SetLobby(lobby);
            
            // Add to tracking list
            currentLobbyItems.Add(lobbyItemGO);

            // Cycle through the possible colors meaning: 0,1,2,0,1,2,...
            lobbyItemGO.GetComponent<Image>().color = listColors[i % listColors.Length];
        }
    }

    // Clear all lobby items from view
    private void ClearLobbyItems() {
        foreach (GameObject item in currentLobbyItems) { Destroy(item); }
        currentLobbyItems.Clear();
    }

    // Enables visualization of information regarding joined lobby
    void ShowJoinedLobby(object sender, LobbyManager.LobbyEventArgs e) {
        Debug.Log("[LOBBY TEST] Joined a new Lobby");

        // TODO Show another menu
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