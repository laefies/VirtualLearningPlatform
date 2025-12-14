using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using Nova;

/// <summary>
/// Controls the experience selection panel, displaying available experiences in a grid.
/// </summary>
public class ExperienceSelectionPanelController : MonoBehaviour
{
    [Header("Experience Configuration")]
    [SerializeField] private List<ExperienceData> availableExperiences;

    [Header("UI References")]
    [SerializeField] private Button startExperienceButton;
    [SerializeField] private Transform experienceGridContainer;
    [SerializeField] private GameObject experienceItemPrefab;

    private LobbyManager LobbyManager => LobbyManager.Instance;
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
        LoadExperiences();
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

    private void LoadExperiences()
    {
        if (availableExperiences != null && availableExperiences.Count > 0)
            return;

        if (availableExperiences.Count == 0)
        {
            Debug.LogWarning("No ExperienceData assets found.");
        }
    }

    private void DisplayExperiences()
    {
        ClearExperienceGrid();

        if (availableExperiences == null || availableExperiences.Count == 0) return;

        foreach (ExperienceData experience in availableExperiences)
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
            : availableExperiences.Find(e => e.experienceName == experienceName);

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

        // Case 1 :: Not in a lobby 
        //              + Clicking an unselected experience         →  Update filtering
        //              + Clicking the already selected experience  →  Unselect / Remove filtering
        if (!LobbyManager.IsInLobby)
        {
            bool isDeselecting = selectedExperience == clickedExperience;
            await LobbyManager.RefreshLobbyListAsync(
                isDeselecting ? null : clickedExperience.experienceName, // New filter
                isDeselecting                                            // Ensures previous filter is cleared
            );
            UpdateSelectedExperience(isDeselecting ? null : clickedExperience);
            return;
        }

        // Case 2 :: In lobby as host → Change lobby experience
        if (LobbyManager.IsHost)
        {
            await LobbyManager.ChangeExperienceAsync(clickedExperience.experienceName);
            return;
        }
    }

    private async void OnStartExperienceClicked()
    {
        if (selectedExperience == null) return;

        // If in a lobby, use lobby system to start
        if (LobbyManager != null && LobbyManager.IsInLobby)
        {
            if (LobbyManager.IsHost) await LobbyManager.StartExperienceAsync();
        }
        else
        {
            // Debug.Log($"[Experience Panel] Starting solo experience: {selectedExperience.experienceName}");
            // TODO: Implement solo experience start
            // SceneManager.LoadScene(selectedExperience.sceneName);
        }
    }
}