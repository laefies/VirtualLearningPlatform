using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using Nova;

/// <summary>
/// Controls the experience selection panel, displaying available experiences in a grid.
/// Coordinates user actions to start solo or multiplayer experiences.
/// </summary>
public class ExperienceSelectionPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button startExperienceButton;
    [SerializeField] private Transform experienceGridContainer;
    [SerializeField] private GameObject experienceItemPrefab;

    private LobbyManager LobbyManager => LobbyManager.Instance;
    private SceneManager SceneManager => SceneManager.Instance;
    
    private readonly List<ExperienceListItem> experienceItems = new List<ExperienceListItem>();
    private ExperienceData selectedExperience;

    private void OnEnable()
    {
        if (LobbyManager != null)
        {
            LobbyManager.OnExperienceChanged += HandleExperienceChanged;
            LobbyManager.OnLobbyPlayersChanged += HandleLobbyPlayersChanged;
        }
        
        startExperienceButton?.AddListener(OnStartExperienceClicked);
    }

    private void Start()
    {
        DisplayExperiences();
        UpdateStartButtonState();
    }

    private void OnDisable()
    {
        if (LobbyManager != null)
        {
            LobbyManager.OnExperienceChanged -= HandleExperienceChanged;
            LobbyManager.OnLobbyPlayersChanged -= HandleLobbyPlayersChanged;
        }

        startExperienceButton?.RemoveListener(OnStartExperienceClicked);
    }

    private void OnDestroy() { ClearExperienceGrid(); }

    private void DisplayExperiences()
    {
        ClearExperienceGrid();

        if (SceneManager == null || SceneManager.AvailableExperiences == null) {
            Debug.LogWarning("[Experience Panel] SceneManager or experiences not available");
            return;
        }

        foreach (ExperienceData experience in SceneManager.AvailableExperiences)
        {
            if (experience == null) continue;

            GameObject itemObject = Instantiate(experienceItemPrefab, experienceGridContainer);
            
            if (itemObject.TryGetComponent(out ExperienceListItem listItem))
            {
                listItem.SetExperience(experience, OnExperienceCardClicked);
                experienceItems.Add(listItem);
            }
        }
    }

    private void HandleExperienceChanged(string experienceName)
    {
        ExperienceData experience = string.IsNullOrEmpty(experienceName)
            ? null
            : SceneManager.GetExperienceByName(experienceName);

        UpdateSelectedExperience(experience);
    }

    private void HandleLobbyPlayersChanged(List<Player> _)
    {
        UpdateExperienceListState();
        UpdateStartButtonState();
    }

    private void UpdateSelectedExperience(ExperienceData newExperience)
    {
        selectedExperience = newExperience;
        
        UpdateExperienceListState();
        UpdateStartButtonState();
    }

    private void ClearExperienceGrid()
    {
        foreach (ExperienceListItem item in experienceItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        experienceItems.Clear();
    }

    private void UpdateExperienceListState()
    {
        foreach (ExperienceListItem item in experienceItems)
        {
            bool isSelected = item.ExperienceData == selectedExperience;
            bool isInteractable = !LobbyManager.IsInLobby || (LobbyManager.IsHost && !isSelected);
            item.SetState(isSelected, isInteractable);
        }
    }

    private void UpdateStartButtonState()
    {
        if (startExperienceButton == null) return;

        bool canStart = selectedExperience != null && 
                       (LobbyManager == null || !LobbyManager.IsInLobby || LobbyManager.IsHost);
        
        startExperienceButton.IsInteractable = canStart;
    }

    private async void OnExperienceCardClicked(ExperienceData clickedExperience)
    {
        if (clickedExperience == null || LobbyManager == null)
            return;

        // Case 1: Not in a lobby - update filtering
        if (!LobbyManager.IsInLobby)
        {
            bool isDeselecting = selectedExperience == clickedExperience;
            await LobbyManager.RefreshLobbyListAsync(
                isDeselecting ? null : clickedExperience.experienceName,
                isDeselecting
            );
            UpdateSelectedExperience(isDeselecting ? null : clickedExperience);
            return;
        }

        // Case 2: In lobby as host - change lobby experience
        if (LobbyManager.IsHost)
        {
            await LobbyManager.ChangeExperienceAsync(clickedExperience.experienceName);
        }
    }

    private async void OnStartExperienceClicked()
    {
        if (selectedExperience == null)
        {
            Debug.LogWarning("[Experience Panel] No experience selected");
            return;
        }

        if (SceneManager.IsTransitioning)
        {
            Debug.LogWarning("[Experience Panel] Scene transition already in progress");
            return;
        }

        // Multiplayer: Prepare relay, then load scene
        if (LobbyManager != null && LobbyManager.IsInLobby)
        {
            if (!LobbyManager.IsHost) {
                Debug.LogWarning("[Experience Panel] Only host can start experience");
                return;
            }

            Debug.Log($"[Experience Panel] Starting multiplayer experience: {selectedExperience.experienceName}");
            
            bool success = await LobbyManager.PrepareMultiplayerExperienceAsync();
            
            if (success)
                SceneManager.LoadMultiplayerExperience(selectedExperience);
            else
                Debug.LogError("[Experience Panel] Failed to prepare multiplayer experience");
        }
        // Solo: Directly load the scene
        else
        {
            Debug.Log($"[Experience Panel] Starting solo experience: {selectedExperience.experienceName}");
            SceneManager.LoadSoloExperience(selectedExperience);
        }
    }
}