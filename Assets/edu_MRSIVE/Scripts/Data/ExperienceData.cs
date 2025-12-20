using UnityEngine;

/// <summary>
/// Defines an experience (educational module/lesson) that can be loaded and played.
/// </summary>
[CreateAssetMenu(fileName = "New Experience", menuName = "edu_MRSIVE/Data/Experience Data")]
public class ExperienceData : ScriptableObject
{
    [Header("Display Information")]
    [Tooltip("Title shown in main menu")]
    public string experienceName;

    [Tooltip("Subtitle shown in main menu")]
    public string experienceSubtitle;
    
    [Tooltip("Thumbnail displayed")]
    public Sprite displayIcon;
    
    [Header("Scene Configuration")]
    [Tooltip("Scene name as it appears in Build Settings")]
    public string sceneName;
    
    [Tooltip("Environment prefab to spawn for VR users")]
    public GameObject vrEnvironment;
}