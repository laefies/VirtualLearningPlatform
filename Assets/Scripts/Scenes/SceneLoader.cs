using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Singleton instance for global access
    public static SceneLoader Instance { get; private set; }

    // Uitlity variables regarding current scene and the Main Menu scene
    [SerializeField] private SceneInfo menuScene;
    private SceneInfo currentScene;
    // TODO :: DELETE AFTER IMPLEMENTING UI
    [SerializeField] private SceneInfo testScene;

    // Event to notify other systems about scene actions
    public event EventHandler<SceneEventArgs> OnSceneLoaded;

    // Custom event args classes to pass scene data
    public class SceneEventArgs : EventArgs {
        public SceneInfo sceneInfo;
    }

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Subscribe to Lobby related events
        LobbyManager.Instance.OnGameStarted += HandleGameStart;

        // Start by showing the player the Menu Scene
        StartCoroutine(LoadMenuScene());
    }

    public void NotifySceneLoad() {
        OnSceneLoaded?.Invoke(this, new SceneEventArgs { sceneInfo = currentScene });      
    }

    // Transition into the chosen game
    void HandleGameStart(object sender, LobbyManager.LobbyEventArgs e) {
        StartCoroutine(TransitionToScene(testScene));
    }

    void OnDestroy() {
        // Unsubscribe from Lobby related events
        LobbyManager.Instance.OnGameStarted -= HandleGameStart;
    }

    /*
     * --- TRANSITION COROUTINES ---
     */

    private IEnumerator LoadMenuScene()
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(menuScene.sceneName, LoadSceneMode.Additive);
        yield return loadOp;
        this.currentScene = menuScene;
        NotifySceneLoad();
    }

    private IEnumerator TransitionToScene(SceneInfo newScene)
    {
        Debug.Log($"[Scene Loader] Starting transition to {newScene.displayName}");

        // First, the current scene must be unloaded
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(currentScene.sceneName);
        while (!unloadOperation.isDone) { yield return null; }

        // Then, the new game scene is loaded by the server, ensuring synchronization
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(newScene.sceneName, LoadSceneMode.Single);
        }

        // Save reference to the new scene
        this.currentScene = newScene;

        while (SceneManager.GetActiveScene().name != newScene.sceneName) { yield return null; }

        Debug.Log($"[Scene Loader] Scene {newScene.sceneName} is now active.");
        NotifySceneLoad();
    }
}