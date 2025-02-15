using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;

public class Detector : MonoBehaviour
{
    [SerializeField] 
    ModelAsset modelAsset;
    private Worker _worker;
    private Tensor _input;

    void OnEnable()
    {
        _worker = new Worker(ModelLoader.Load(modelAsset), BackendType.GPUCompute);
        _input  = new Tensor<float>(new TensorShape(1024));
    }

    void Update()
    {
        _worker.Schedule(_input);
        var outputTensor = _worker.PeekOutput() as Tensor<float>;
        var cpuTensor = outputTensor.ReadbackAndClone();
        cpuTensor.Dispose();
    }

    void OnDisable()
    {
        _worker.Dispose();
        _input.Dispose();
    }
}
