using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;

public class SceneLoader : MonoBehaviour
{
    // Singleton instance for global access
    public static SceneLoader Instance { get; private set; }

    // Reference to the currently loaded scene
    private SceneInfo currentScene;

    // Reference to the main menu scene
    [SerializeField] private SceneInfo menuScene;

    // Event to notify other systems about scene actions
    public event EventHandler<SceneEventArgs> OnSceneLoaded;

    // Custom event arguments classes to pass scene data
    public class SceneEventArgs : EventArgs {
        public SceneInfo sceneInfo;
    }

    [SerializeField] private SceneInfo testScene; // TODO :: DELETE AFTER IMPLEMENTING UI

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void OnEnable()
    {
        // Authenticate into Unity Services
        await LobbyManager.Instance.AuthenticateAsync();

        // Subscribe to Lobby related events
        LobbyManager.Instance.OnExperienceStarted += HandleGameStart;
    }

    private void Start()
    {
        // Start by showing the player the Menu Scene
        StartCoroutine(LoadMenuScene());
    }

    // Handle scene change by saving new scene data and notifying listeners
    private void HandleSceneLoaded(SceneInfo newScene) {
        if (currentScene == null || newScene.sceneName != currentScene.sceneName)
        {
            Debug.Log($"[Scene Loader] Scene successfully changed to '{newScene.displayName}'");

            currentScene = newScene;
            OnSceneLoaded?.Invoke(this, new SceneEventArgs { sceneInfo = newScene });     
        }
    }

    // Transition into the chosen game
    void HandleGameStart(Lobby lobby) {
        StartCoroutine(TransitionToScene(testScene));
    }

    // Unsubscribe from Lobby related events
    void OnDisable() {
        LobbyManager.Instance.OnExperienceStarted -= HandleGameStart;
    }

    /*
     * --- TRANSITION COROUTINES ---
     */

    private IEnumerator LoadMenuScene()
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(menuScene.sceneName, LoadSceneMode.Additive);
        yield return loadOp;
        HandleSceneLoaded(menuScene);
    }

    private IEnumerator TransitionToScene(SceneInfo newScene)
    {
        // Debug.Log($"[Scene Loader] Starting transition to '{newScene.displayName}'");

        // First, the current scene must be unloaded
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(currentScene.sceneName);
        while (!unloadOperation.isDone) { yield return null; }

        // Then, the new game scene is loaded by the server, ensuring synchronization
        if (NetworkManager.Singleton.IsServer) {
            NetworkManager.Singleton.SceneManager.LoadScene(newScene.sceneName, LoadSceneMode.Single);
        }

        // Wait until the scene is loaded and handle new scene data
        while (SceneManager.GetActiveScene().name != newScene.sceneName) { yield return null; }
        HandleSceneLoaded(newScene);
    }
}