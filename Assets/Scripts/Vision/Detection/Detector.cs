using UnityEngine;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using NativeWebSocket;
using Newtonsoft.Json; 

public class Detection
{
    public string class_id;
    public List<List<int>> corners;
}

public class DetectionMessage
{
    public List<Detection> detections;
}

public class Detector : MonoBehaviour
{
    [SerializeField] private string serverUrl = "wss://supreme-quail-united.ngrok-free.app";
    private WebSocket websocket;

    private bool isConnected = false;
    private bool awaitingResponse = false;
    public event Action<DetectionMessage> OnMarkDetected;


    private DateTime lastSentTime;
    private TimeSpan averageRTT = TimeSpan.FromMilliseconds(1000);
    private readonly float alpha = 0.2f;

    public static Detector Instance{ get; private set;}

    private void Awake() {
        Instance = this;
    }

    void Start()
    {
        ConnectToServer();
    }

    public bool IsAvailable() {
        if (!isConnected) return false;

        return (DateTime.UtcNow - lastSentTime).TotalSeconds >= .25;
    }

    async void Update()
    {
        if (websocket == null) return;
        
        #if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
        #endif        
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

            // TimeSpan rtt = DateTime.UtcNow - lastSentTime;
            // averageRTT = TimeSpan.FromMilliseconds((1 - alpha) * averageRTT.TotalMilliseconds + alpha * rtt.TotalMilliseconds);
            // Debug.Log("RTT1 " + averageRTT);

            var message = System.Text.Encoding.UTF8.GetString(bytes);            
            DetectionMessage detectionData = JsonConvert.DeserializeObject<DetectionMessage>(message);
            OnMarkDetected?.Invoke(detectionData);
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

    public async void SendMessageAsync(byte[] message)
    {
        if (websocket.State == WebSocketState.Open && IsAvailable())
        {
            // float deltaTime = Time.time - testingCallTime;
            // Debug.Log("[SEND] Time since last frame sent: " + deltaTime + " seconds");
            // testingCallTime = Time.time;

            lastSentTime     = DateTime.UtcNow;
            await websocket.Send(message);
            awaitingResponse = true;
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