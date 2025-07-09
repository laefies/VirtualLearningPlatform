using UnityEngine;

[System.Flags]
public enum SubsystemRequirements
{
    None = 0,
    MarkerDetection   = 1 << 0,
}

[CreateAssetMenu(menuName = "Scene Info")]
public class SceneInfo : ScriptableObject
{

    [Header("Display Information")]
    // Name of the Game/Lesson shown to the player in menus
    public string displayName;

    // Description of the simulation, shown to the player in menus
    public string sceneDescription;

    [Header("Setup Details and Requirements")]
    // Actual name of the scene, as saved in the Unity project
    public string sceneName;

    // Device subsystems needed for the game/lesson to run
    public SubsystemRequirements requiredSubsystems = SubsystemRequirements.None;

    // Prefab environment to spawn for VR players (background/backdrop)
    public GameObject vrEnvironmentPrefab;

    /* METHODS TO HANDLE REQUIRED SUBSYSTEMS MORE EFFICIENTLY */
    public bool RequiresSubsystem(SubsystemType type)
    {
        return (requiredSubsystems & ToRequirement(type)) != 0;
    }

    private SubsystemRequirements ToRequirement(SubsystemType type)
    {
        return type switch
        {
            SubsystemType.MarkerDetection => SubsystemRequirements.MarkerDetection,
            _ => SubsystemRequirements.None
        };
    }

}