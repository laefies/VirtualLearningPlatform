using UnityEngine;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using NativeWebSocket;
using Newtonsoft.Json; 
using UnityEngine.XR.OpenXR;
using Unity.XR.CoreUtils;

[Serializable]
public class DetectionRequest
{
    public string frameData;
}

[Serializable]
public class DetectionResponse
{
    public List<Detection> detections;
}

public class Detection
{
    public string class_id;
    public List<List<float>> corners;
}

public class Detector : MonoBehaviour
{
    [SerializeField] private string serverUrl  = "wss://supreme-quail-united.ngrok-free.app";
    [SerializeField] private float requestRate = .5f;

    private WebSocket websocket;

    private bool isConnected      = false;
    private bool awaitingResponse = false;

    private DateTime lastSentTime;
    private DetectionResponse lastResponse;

    public static Detector Instance{ get; private set;}

    public event EventHandler<DetectionEventArgs> OnDetectionReceived;

    public class DetectionEventArgs : EventArgs {
        public DetectionResponse response;
    }

    private void Awake() {
        Instance = this;
    }

    void Start()
    {
        ConnectToServer();
    }

    public bool IsAvailable() {
        if (!isConnected) return false;

        return !awaitingResponse;
    }

    async void Update()
    {
        if (websocket == null) return;
        
        #if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
        #endif

        if (lastResponse != null)
        {
            DetectionResponse toHandle = lastResponse;
            lastResponse = null;

            if (toHandle?.detections == null || toHandle.detections.Count == 0)
                return;

            OnDetectionReceived?.Invoke(this, new DetectionEventArgs { response = toHandle });
        }

    }

    async void ConnectToServer()
    {
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () => 
        {
            Debug.Log("Connection open!");
            isConnected = true;
        };

        websocket.OnClose += (e) => 
        {
            Debug.Log($"Connection closed! Code: {e}");
            isConnected = false;
        };

        websocket.OnMessage += (bytes) =>
        {
            awaitingResponse = false;
            lastResponse     = JsonConvert.DeserializeObject<DetectionResponse>(Encoding.UTF8.GetString(bytes));
        };

        websocket.OnError += (e) =>
        { 
            Debug.LogError($"WebSocket Error: {e}");
        };

        try
        {
            await websocket.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to connect to WebSocket server: {ex.Message}");
        }
    }

    public async void SendMessageAsync(DetectionRequest request)
    {
        if (websocket.State == WebSocketState.Open && IsAvailable())
        {            
            string jsonMessage = JsonConvert.SerializeObject(request);
            awaitingResponse = true;

            await websocket.Send(Encoding.UTF8.GetBytes(jsonMessage));
            lastSentTime = DateTime.UtcNow;
        }
    }

    private async void CloseConnection()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            await websocket.Close();
        }
    }

    private void OnApplicationQuit()
    {
        CloseConnection();
    }
}