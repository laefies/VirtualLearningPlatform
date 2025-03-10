using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;

public class Detector : MonoBehaviour
{
    [SerializeField]
    private ModelAsset modelAsset;

    private Worker _worker;

    private Tensor<float> _output0, _output1;

    void OnEnable()
    {
        // Prepare model
        _worker = new Worker(ModelLoader.Load(modelAsset), BackendType.GPUCompute);
    }

    public static float[,] Reshape(float[] flattenedArray, int rows, int cols, bool transpose = false)
    {
        float[,] result = new float[transpose ? cols : rows, transpose ? rows : cols];
        
        if (transpose)
        {
            for (int i = 0; i < flattenedArray.Length; i++)
                result[i % cols, i / cols] = flattenedArray[i];
        }
        else
        {
            for (int i = 0; i < flattenedArray.Length; i++)
                result[i / cols, i % cols] = flattenedArray[i];
        }

        return result;
    }

    public void TreatResults()
    {        
        if (_output0 == null || _output1 == null) return;

        float[] output1 = _output1.DownloadToArray();

        float[,] output0 = Reshape(_output0.DownloadToArray(), _output0.shape[1], _output0.shape[2], true);        


    }

    // TODO 
    // Save original size? 
    // Make async
    // Obter número classes e etc em runtime
    public void Detect(Texture2D imageTexture)
    {
        if (_worker == null) return;

        _worker.Schedule(TextureConverter.ToTensor(imageTexture, width: 640, height: 640));

        var outputTensor0 = _worker.PeekOutput("output0") as Tensor<float>;
        var outputTensor1 = _worker.PeekOutput("output1") as Tensor<float>;

        _output0 = outputTensor0.ReadbackAndClone();
        _output1 = outputTensor1.ReadbackAndClone();

        TreatResults();
    }

    void OnDisable()
    {
        _worker?.Dispose();
        _output0?.Dispose();
        _output1?.Dispose();
    }
}
