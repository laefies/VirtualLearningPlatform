using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Netcode;

public class SceneLoader : MonoBehaviour
{
    // Singleton instance for global access
    public static SceneLoader Instance { get; private set; }

    // Uitlity variables
    [SerializeField] private string menuSceneName = "MainMenu";
    private string currentScene;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Subscribe to Lobby related events
        LobbyManager.Instance.OnGameStarted += HandleGameStart;

        // Start with menu scene
        currentScene = menuSceneName;
        SceneManager.LoadScene(menuSceneName, LoadSceneMode.Additive);
    }

    void OnDestroy() {
        // Unsubscribe from Lobby related events
        LobbyManager.Instance.OnGameStarted -= HandleGameStart;
    }

    // Transition into the chosen game
    void HandleGameStart(object sender, LobbyManager.LobbyEventArgs e) {
        StartCoroutine(TransitionToScene("SolarPanelTest"));
    }

    /*
     * --- TRANSITION COROUTINES ---
     */

    private IEnumerator TransitionToScene(string sceneName)
    {
        Debug.Log($"Starting transition to {sceneName}");
        
        // First, the current scene must be unloaded
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(currentScene);
        while (!unloadOperation.isDone) { yield return null; }
        
        // Then, the new game scene is loaded by the server, ensuring synchronization
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        currentScene = sceneName;
    }
}