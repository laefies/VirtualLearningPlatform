using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;

public class Detector : MonoBehaviour
{
    [SerializeField]
    private ModelAsset modelAsset;

    private Worker _worker;

    private Tensor<float> _lastOutput;

    void OnEnable()
    {
        _worker = new Worker(ModelLoader.Load(modelAsset), BackendType.GPUCompute);
    }

    public void TreatResults()
    {
        if (_lastOutput == null)
        {
            Debug.LogError("No output tensor to process.");
            return;
        }

        Debug.Log(_lastOutput[0]);

        for (int i = 0; i < _lastOutput.shape[1]; i++)
        {
            Debug.Log(_lastOutput[0, i]);

            // float x      = _lastOutput[0, i, 0]; // X-coordinate
            // float y          = _lastOutput[0, i, 1]; // Y-coordinate

            // float width      = _lastOutput[0, i, 2];
            // float height     = _lastOutput[0, i, 3];

            // float confidence = _lastOutput[0, i, 4];
            // int classId = (int)_lastOutput[0, i, 5]; // Class ID (assuming last index)

            // if (confidence < 0.5f) continue;

            // Debug.Log($"Detected object: Class {classId}, Confidence {confidence}, Box: ({x}, {y}, {width}, {height})");
        }
    }


    public void Detect(Texture2D imageTexture)
    {

        if (_worker == null)
        {
            Debug.LogError("Worker is not initialized!");
            return;
        }

        _worker.Schedule(TextureConverter.ToTensor(imageTexture, width: 640, height: 640));

        var results = _worker.PeekOutput() as Tensor<float>;
        _lastOutput = results.ReadbackAndClone();

        TreatResults();
    }

    void OnDisable()
    {
        _worker?.Dispose();
    }
}
