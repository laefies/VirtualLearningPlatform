using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

/// <summary>
/// Centralized manager for scene transitions and experience data.
/// Handles both solo and multiplayer scene loading.
/// </summary>
public class SceneManager : MonoBehaviour
{
    #region Singleton
    public static SceneManager Instance { get; private set; }
    #endregion

    #region Configuration
    [Header("Scene Configuration")]
    [SerializeField] private GameObject defaultVREnvironment;
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private List<ExperienceData> availableExperiences;
    #endregion

    #region State
    private ExperienceData currentExperience;
    private bool isTransitioning;
    private bool isInMenu;
    #endregion

    #region Events
    public event Action OnMenuLoaded;
    public event Action<ExperienceData> OnExperienceLoaded;
    #endregion

    #region Properties
    public ExperienceData CurrentExperience => currentExperience;
    public IReadOnlyList<ExperienceData> AvailableExperiences => availableExperiences;
    public bool IsTransitioning => isTransitioning;
    public bool IsInMenu => isInMenu;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ValidateConfiguration();
    }

    private void OnEnable()
    {
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.OnRelayReady += HandleRelayReady;
    }

    private void OnDisable()
    {
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.OnRelayReady -= HandleRelayReady;
    }

    private async void Start()
    {
        MockBootstraping();
    }

    [SerializeField] private GameObject loadingScreen;
    private async void MockBootstraping()
    {
        // know what type of device and spawn VR environment if needed
        if (PlayerManager.Instance.GetDeviceInfo()?.deviceType != DeviceType.AR)
            SpawnVREnvironmentForCurrentScene();

        // spawn device
        PlayerManager.Instance.SpawnDevice();

        FollowPlayerUI aa = Instantiate(loadingScreen).GetComponent<FollowPlayerUI>();
        aa.ForceReposition();
        DontDestroyOnLoad(aa.gameObject);
        await LobbyManager.Instance?.AuthenticateAsync();

        StartCoroutine(LoadMenuScene());
        Destroy(aa.gameObject);
    }
    #endregion

    #region Public API
    /// <summary>
    /// Loads an experience in solo mode (no networking).
    /// </summary>
    public void LoadSoloExperience(ExperienceData experience)
    {
        if (!ValidateExperienceLoad(experience)) return;
        
        Debug.Log($"[SceneManager] Loading solo experience: {experience.experienceName}");
        StartCoroutine(TransitionToExperience(experience, false));
    }

    /// <summary>
    /// Loads an experience in multiplayer mode (with networking).
    /// </summary>
    public void LoadMultiplayerExperience(ExperienceData experience)
    {
        if (!ValidateExperienceLoad(experience)) return;

        var netManager = NetworkManager.Singleton;
        if (netManager == null || (!netManager.IsServer && !netManager.IsClient))
        {
            Debug.LogError("[SceneManager] Cannot load multiplayer - not connected to network");
            return;
        }

        string role = LobbyManager.Instance.IsHost ? "host" : "client";
        Debug.Log($"[SceneManager] Loading multiplayer experience as {role}: {experience.experienceName}");
        
        StartCoroutine(TransitionToExperience(experience, true));
    }

    /// <summary>
    /// Returns to the main menu.
    /// </summary>
    public void ReturnToMenu()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[SceneManager] Already transitioning to another scene");
            return;
        }

        Debug.Log("[SceneManager] Returning to menu");
        
        var netManager = NetworkManager.Singleton;
        if (netManager != null && (netManager.IsServer || netManager.IsClient))
        {
            netManager.Shutdown();
        }

        StartCoroutine(TransitionToMenu());
    }

    /// <summary>
    /// Gets an experience by name.
    /// </summary>
    public ExperienceData GetExperienceByName(string experienceName)
    {
        if (string.IsNullOrEmpty(experienceName)) return null;
        return availableExperiences.FirstOrDefault(e => e != null && e.experienceName == experienceName);
    }

    /// <summary>
    /// Spawns the VR environment for the current scene.
    /// </summary>
    public GameObject SpawnVREnvironmentForCurrentScene()
    {
        GameObject prefabToSpawn = currentExperience != null && currentExperience.vrEnvironment != null
            ? currentExperience.vrEnvironment
            : defaultVREnvironment;

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[SceneManager] No VR environment prefab available");
            return null;
        }

        GameObject spawnedEnv = Instantiate(prefabToSpawn);
        Debug.Log("[SceneManager] Spawned VR environment");
        return spawnedEnv;
    }
    #endregion

    #region Event Handlers
    private void HandleRelayReady()
    {
        // Only handle for non-host clients
        if (LobbyManager.Instance.IsHost || isTransitioning) return;

        string experienceName = LobbyManager.Instance.GetCurrentExperience();
        ExperienceData experience = GetExperienceByName(experienceName);
        
        if (experience == null)
        {
            Debug.LogError($"[SceneManager] Could not find experience data for: {experienceName}");
            return;
        }

        Debug.Log($"[SceneManager] Relay ready - loading multiplayer experience as client: {experienceName}");
        LoadMultiplayerExperience(experience);
    }
    #endregion

    #region Scene Transitions
    private IEnumerator LoadMenuScene()
    {
        isTransitioning = true;

        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(menuSceneName, LoadSceneMode.Additive);

        isInMenu = true;
        currentExperience = null;

        Debug.Log("[SceneManager] Menu loaded");
        OnMenuLoaded?.Invoke();
        
        isTransitioning = false;
    }

    private IEnumerator TransitionToMenu()
    {
        isTransitioning = true;

        if (currentExperience != null)
        {
            yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentExperience.sceneName);
        }

        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(menuSceneName, LoadSceneMode.Single);

        isInMenu = true;
        currentExperience = null;

        Debug.Log("[SceneManager] Returned to menu");
        OnMenuLoaded?.Invoke();

        isTransitioning = false;
    }

    private IEnumerator TransitionToExperience(ExperienceData experience, bool isNetworked)
    {
        isTransitioning = true;

        // Unload menu scene
        if (isInMenu)
        {
            yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(menuSceneName);
        }

        // Load experience scene
        if (isNetworked)
        {
            yield return LoadNetworkedScene(experience.sceneName);
        }
        else
        {
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(experience.sceneName, LoadSceneMode.Single);
        }

        // Update state
        isInMenu = false;
        currentExperience = experience;

        Debug.Log($"[SceneManager] Successfully loaded experience: {experience.experienceName}");
        OnExperienceLoaded?.Invoke(experience);

        isTransitioning = false;
    }

    private IEnumerator LoadNetworkedScene(string sceneName)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        // Wait for scene to load (both server and client)
        while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != sceneName)
        {
            yield return null;
        }
    }
    #endregion

    #region Validation
    private bool ValidateExperienceLoad(ExperienceData experience)
    {
        if (experience == null)
        {
            Debug.LogError("[SceneManager] Cannot load null experience");
            return false;
        }

        if (isTransitioning)
        {
            Debug.LogWarning("[SceneManager] Already transitioning to another scene");
            return false;
        }

        return true;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogError("[SceneManager] Menu scene name not configured!");
        }

        if (availableExperiences == null || availableExperiences.Count == 0)
        {
            Debug.LogWarning("[SceneManager] No experiences configured!");
            return;
        }

        // Check for duplicate experiences
        var duplicates = availableExperiences
            .Where(e => e != null)
            .GroupBy(e => e.experienceName)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var dup in duplicates)
        {
            Debug.LogWarning($"[SceneManager] Duplicate experience name found: {dup}");
        }
    }
    #endregion
}