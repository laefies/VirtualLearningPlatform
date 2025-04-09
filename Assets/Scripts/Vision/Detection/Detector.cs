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
    [SerializeField] private string serverUrl = "ws://localhost:8765";
    private WebSocket websocket;
    private bool isConnected = false;
    private Queue<byte[]> messageQueue = new Queue<byte[]>();
    private object queueLock = new object();
    private bool awaitingResponse = false;

    public event Action<DetectionMessage> OnMarkDetected;


    void Start()
    {
        ConnectToServer();
    }

    async void Update()
    {
        if (websocket == null) return;
        
        #if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
        #endif
        
        if (isConnected)
        {
            lock (queueLock)
            {
                while (messageQueue.Count > 0)
                {
                    byte[] message = messageQueue.Dequeue();
                    SendMessageAsync(message);
                }
            }
        }
    }

    public async void ConnectToServer()
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
            var message = System.Text.Encoding.UTF8.GetString(bytes);            
            DetectionMessage detectionData = JsonConvert.DeserializeObject<DetectionMessage>(message);
            OnMarkDetected?.Invoke(detectionData);

            awaitingResponse = false;
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

    public void QueueFrameToSend(byte[] frameData)
    {
        if (!isConnected) return;

        lock (queueLock)
        {
            messageQueue.Clear();
            messageQueue.Enqueue(frameData);
        }
    }

    private async void SendMessageAsync(byte[] message)
    {
        if (websocket.State == WebSocketState.Open && !awaitingResponse)
        {
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