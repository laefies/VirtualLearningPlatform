using System.Collections.Generic;
using UnityEngine;
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
            LobbyManager.OnExperienceChanged += HandleExperienceChanged;
        
        startExperienceButton?.AddListener(OnStartExperienceClicked);

        LoadExperiences();
        DisplayExperiences();
        UpdateStartButtonState();
    }

    private void OnDisable()
    {
        if (LobbyManager != null)
            LobbyManager.OnExperienceChanged -= HandleExperienceChanged;

        startExperienceButton?.RemoveListener(OnStartExperienceClicked);
    }

    private void OnDestroy() { ClearExperienceGrid(); }

    private void LoadExperiences()
    {
        if (availableExperiences != null && availableExperiences.Count > 0)
            return;

        if (availableExperiences.Count == 0) {
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
            
            if (itemObject.TryGetComponent(out ExperienceListItem listItem)) {
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

    private void UpdateSelectedExperience(ExperienceData newExperience)
    {
        selectedExperience = newExperience;
        
        foreach (ExperienceListItem item in experienceItems)
        {
            bool isSelected     = item.ExperienceData == selectedExperience;
            bool isInteractable = !LobbyManager.IsInLobby || (LobbyManager.IsHost && !isSelected);
            item.SetState(isSelected, isInteractable);  
        }   

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

    private void UpdateStartButtonState()
    {
        if (startExperienceButton == null) return;

        bool canStart = selectedExperience != null && 
                       (LobbyManager == null || !LobbyManager.IsInLobby || LobbyManager.IsHost);
        
        startExperienceButton.interactable = canStart;
    }


    private async void OnExperienceCardClicked(ExperienceData clickedExperience)
    {
        if (clickedExperience == null || LobbyManager == null)
            return;

        bool inLobby = LobbyManager.IsInLobby;
        bool isHost  = LobbyManager.IsHost;

        // Case 1 :: Not in lobby + Clicking the already selected experience → Unselect
        if (!inLobby && selectedExperience == clickedExperience) {
            await LobbyManager.RefreshLobbyListAsync(null, true);
            UpdateSelectedExperience(null);
            return;
        }

        // Case 2 :: Host of a lobby → Change lobby experience
        if (inLobby && isHost) {
            bool changed = await LobbyManager.ChangeExperienceAsync(clickedExperience.experienceName);
            UpdateSelectedExperience(changed ? clickedExperience : selectedExperience);
            return;
        }

        // Case 3 : Not in a lobby → Update filtering
        if (!inLobby) {
            await LobbyManager.RefreshLobbyListAsync(clickedExperience.experienceName);
            UpdateSelectedExperience(clickedExperience);
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