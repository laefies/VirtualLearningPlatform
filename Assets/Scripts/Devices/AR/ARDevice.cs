using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ARDevice : MonoBehaviour
{
    public static ARDevice Instance { get; private set; }

    // TODO Abstract? Make Vision Manager?
    [SerializeField] public ML2DetectionManager Detection;
    [SerializeField] public AlignmentManager Alignment;
    [SerializeField] public XRInteractionManager Interaction;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}