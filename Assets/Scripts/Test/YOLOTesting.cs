using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YOLOTesting : MonoBehaviour
{
    [SerializeField]
    private Detector detector;

    [SerializeField]
    private Texture2D testImage;

    void Start()
    {
        if (detector == null || testImage == null)
        {
            Debug.LogError("Please assign the Detector and Test Image in the Inspector!");
            return;
        }

        detector.Detect(testImage);
    }
}