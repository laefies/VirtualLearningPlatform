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
    public IntrinsicParameters intrinsicParams;
    public ExtrinsicParameters extrinsicParams;
}

[Serializable]
public class DetectionResponse
{
    public List<Detection> detections;
    public IntrinsicParameters intrinsicParams;
    public ExtrinsicParameters extrinsicParams;
}

public class Detection
{
    public string class_id;
    public List<List<int>> corners;
}


public class Detector : MonoBehaviour
{
    [SerializeField] private string serverUrl  = "wss://supreme-quail-united.ngrok-free.app";
    [SerializeField] private float requestRate = .5f;
    private Transform _origin;

    private WebSocket websocket;

    private bool isConnected      = false;
    private bool awaitingResponse = false;

    private DateTime lastSentTime;
    private DetectionResponse lastResponse;

    public static Detector Instance{ get; private set;}

    private void Awake() {
        Instance = this;

    }

    void Start()
    {
        ConnectToServer();
        XROrigin xrOrigin = FindAnyObjectByType<XROrigin>();
        _origin = xrOrigin.CameraFloorOffsetObject.transform;
    }

    public bool IsAvailable() {
        if (!isConnected) return false;

        return (DateTime.UtcNow - lastSentTime).TotalSeconds >= requestRate;
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
                foreach (var detection in toHandle.detections)
                {
                    HandleDetection(detection, toHandle.intrinsicParams, toHandle.extrinsicParams);
                }
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
            lastResponse = JsonConvert.DeserializeObject<DetectionResponse>(Encoding.UTF8.GetString(bytes));            
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

    private void HandleDetection(Detection detection, IntrinsicParameters intrinsicParams, ExtrinsicParameters extrinsicParams) {
        // TODO Python
        if (detection.corners.Count != 4) return;

        Vector3 sum = Vector3.zero;
        List<Vector3> cornerPositions = new List<Vector3>();
                        
        foreach (var point in detection.corners)
        {
            Vector3 cornerPos = CameraUtilities.CastRayFromScreenToWorldPoint(
                 intrinsicParams, extrinsicParams, new Vector2(point[0], point[1])
            );
            cornerPositions.Add(cornerPos);
            sum += cornerPos;
        }

        Vector3 center = sum / cornerPositions.Count;
        Debug.Log("Center Sem Transform: " + center);
        Debug.Log("Center Com Transform: " + _origin.TransformPoint(center));
        Vector3 centerTest = _origin.TransformPoint(center);

        Vector2 topLeftPixel    = new Vector2(0, 0);
        Vector2 bottomLeftPixel = new Vector2(0, 360);

        Debug.Log("Top Pixel   \n" + CameraUtilities.CastRayFromScreenToWorldPoint(intrinsicParams, extrinsicParams, topLeftPixel));
        Debug.Log("Bottom Left \n" + CameraUtilities.CastRayFromScreenToWorldPoint(intrinsicParams, extrinsicParams, bottomLeftPixel));

        NetworkObjectManager.Instance.ProcessMarkerServerRpc(
             new MarkerInfo {
                 Id   = detection.class_id,
                 Pose = new Pose(new Vector3(center.x, 1.06f, center.z), Quaternion.identity),
                 Size = 0.05f
             }
        );
    }


    public async void SendMessageAsync(DetectionRequest request)
    {
        if (websocket.State == WebSocketState.Open && IsAvailable())
        {
            string jsonMessage = JsonConvert.SerializeObject(request);
            await websocket.Send(Encoding.UTF8.GetBytes(jsonMessage));
            awaitingResponse = true;
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