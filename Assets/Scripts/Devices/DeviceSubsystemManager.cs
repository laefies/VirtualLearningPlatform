using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SubsystemType { MarkerDetection }

public abstract class DeviceSubsystemManager : MonoBehaviour
{
    protected SubsystemType _managedSubsystemType;
    private bool isEnabled;

    void Start()     { SceneLoader.Instance.OnSceneLoaded += handleSubsystemStatus; }
    void OnDestroy() { SceneLoader.Instance.OnSceneLoaded -= handleSubsystemStatus; }

    void Update()
    {
        /* TODO if (isEnabled) */ HandleSubsystem();
    }
    protected abstract void HandleSubsystem();

    void handleSubsystemStatus(object sender, SceneLoader.SceneEventArgs e) {
        isEnabled = e.sceneInfo.RequiresSubsystem(_managedSubsystemType);
    }
}