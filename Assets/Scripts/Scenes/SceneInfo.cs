using UnityEngine;

[System.Flags]
public enum SubsystemRequirements
{
    None = 0,
    MarkerDetection   = 1 << 0,
    VoiceInput        = 1 << 1, // 0010
    HandTracking      = 1 << 2, // 0100
    SpatialMapping    = 1 << 3  // 1000
}

[CreateAssetMenu(menuName = "Scene Info")]
public class SceneInfo : ScriptableObject
{

    [Header("Display Information")]
    // Name of the Game/Lesson shown to the player in menus
    public string displayName;

    // Description of the simulation, shown to the player in menus
    //public string displayDescription;

    // Icon of the experience, shown to the player in menus
    public Sprite displayIcon;

    [Header("Setup Details and Requirements")]
    // Actual name of the scene, as saved in the Unity project
    public string sceneName;

    // Prefab environment to spawn for VR players (background/backdrop)
    public GameObject vrEnvironmentPrefab;

    /* METHODS TO HANDLE REQUIRED SUBSYSTEMS MORE EFFICIENTLY - TODO Might be reworked? */
    public SubsystemRequirements requiredSubsystems = SubsystemRequirements.None;
    public bool RequiresSubsystem(SubsystemType type) { return (requiredSubsystems & ToRequirement(type)) != 0; }
    private SubsystemRequirements ToRequirement(SubsystemType type)
    {
        return type switch
        {
            SubsystemType.MarkerDetection => SubsystemRequirements.MarkerDetection,
            _ => SubsystemRequirements.None
        };
    }

}