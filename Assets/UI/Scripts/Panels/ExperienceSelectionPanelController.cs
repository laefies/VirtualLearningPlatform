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
    private readonly List<GameObject> activeExperienceItems = new List<GameObject>();
    private ExperienceData selectedExperience;

    private void OnEnable()
    {
        startExperienceButton?.AddListener(OnStartExperienceClicked);
        
        LoadExperiences();
        DisplayExperiences();
    }

    private void OnDisable()
    {
        startExperienceButton?.RemoveListener(OnStartExperienceClicked);
    }

    private void OnDestroy() { ClearExperienceGrid();  }

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
            activeExperienceItems.Add(itemObject);

            if (itemObject.TryGetComponent(out ExperienceListItem listItem))
                listItem.SetExperience(experience);
        }
    }

    private void ClearExperienceGrid()
    {
        foreach (GameObject item in activeExperienceItems)
        {
            if (item != null)
                Destroy(item);
        }
        activeExperienceItems.Clear();
    }

    private async void OnStartExperienceClicked()
    {
        if (LobbyManager == null || !LobbyManager.IsInLobby || !LobbyManager.IsHost)
            return;

        await LobbyManager.StartExperienceAsync();
    }
}