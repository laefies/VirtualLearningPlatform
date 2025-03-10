using UnityEngine;
using NativeWebSocket;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

public abstract class CameraManager : MonoBehaviour
{
    protected Texture2D texture;
    protected WebSocket ws;
    private Coroutine captureRoutine;
    public static float FRAME_INTERVAL = 0.5f;

    protected virtual async void Start()
    {
        if (SetUpCamera()) {
            ws = new WebSocket("ws://192.168.1.142:8765");
            ws.OnMessage += (bytes) => 
            {
                Debug.Log("DEBUG: Message received!!!");                
            };

            await ws.Connect();
        }
    }

    protected abstract bool SetUpCamera();
    protected abstract void CaptureFrame();

    protected async void SendFrameToServer(byte[] imageData)
    {
        if (ws.State == WebSocketState.Open)
        {
            await Task.Run(async () =>
            {
                await ws.Send(imageData);
            });
        }
    }

    private async void OnApplicationQuit()
    {
        await ws.Close();
    }

    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
            ws.DispatchMessageQueue();
        #endif
    }

}