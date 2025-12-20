using UnityEngine;
using Nova;
using System;

/// <summary>
/// Represents a single experience card in the grid.
/// This is a "dumb" view component - it doesn't manage state, just displays it.
/// </summary>
public class ExperienceListItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Toggle selectExperienceToggle;
    [SerializeField] private UIBlock2D thumbnail;
    [SerializeField] private TextBlock experienceNameText;
    [SerializeField] private TextBlock subtitleText;
    
    private ExperienceData experienceData;
    private Action<ExperienceData> onClickCallback;

    public ExperienceData ExperienceData => experienceData;

    private void Awake() { selectExperienceToggle?.AddListener(OnToggleClicked); }
    private void OnDestroy() { selectExperienceToggle?.RemoveListener(OnToggleClicked); }

    public void SetExperience(ExperienceData data, Action<ExperienceData> onClick)
    {
        experienceData  = data;
        onClickCallback = onClick;
        UpdateDisplay();
    }

    public void SetState(bool isSelected, bool isInteractable)
    {
        if (selectExperienceToggle != null)
        {
            selectExperienceToggle.ToggledOn      = isSelected;
            selectExperienceToggle.IsInteractable = isInteractable;
        }
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

    private void OnToggleClicked() { onClickCallback?.Invoke(experienceData); }
}