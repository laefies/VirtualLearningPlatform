using UnityEngine;
using Nova;

/// <summary>
/// Controls the visual representation of a single experience card in the grid.
/// </summary>
public class ExperienceListItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button selectExperienceButton;
    [SerializeField] private UIBlock2D thumbnail;
    [SerializeField] private TextBlock experienceNameText;
    [SerializeField] private TextBlock subtitleText;
    
    private ExperienceData experienceData;
    private LobbyManager LobbyManager => LobbyManager.Instance;

    private void Awake() { selectExperienceButton?.AddListener(OnItemClicked); }
    private void OnDestroy() { selectExperienceButton?.RemoveListener(OnItemClicked); }

    public void SetExperience(ExperienceData data)
    {
        experienceData = data;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (experienceData == null) return;

        if (experienceNameText != null)
            experienceNameText.Text = experienceData.experienceName;

        if (subtitleText != null)
            subtitleText.Text = experienceData.experienceSubtitle;

        thumbnail?.SetImage(experienceData.displayIcon);
    }

    private async void OnItemClicked()
    {
        if (experienceData == null || LobbyManager == null) return;

        if (!LobbyManager.IsInLobby) await LobbyManager.RefreshLobbyListAsync(experienceData.sceneName);
        else if (LobbyManager.IsHost) await LobbyManager.ChangeExperienceAsync(experienceData.sceneName);
    }

}