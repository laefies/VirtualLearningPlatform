using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using System.Text;

public class CameraImageTest : MonoBehaviour
{
    [SerializeField, Tooltip("The renderer to show the camera capture on JPEG format")]
    private Renderer _screenRendererRGB = null;

    public bool IsCameraConnected => _captureCamera != null && _captureCamera.ConnectionEstablished;
    
    [SerializeField][Tooltip("If true, the camera capture will start immediately.")]
    private bool _startCameraCaptureOnStart = true;

    #region Capture Config

    private int _targetImageWidth = 640; //1920;
    private int _targetImageHeight = 360; //1080;
    private MLCameraBase.Identifier _cameraIdentifier = MLCameraBase.Identifier.CV;
    private MLCameraBase.CaptureFrameRate _targetFrameRate = MLCameraBase.CaptureFrameRate._30FPS;
    private MLCameraBase.OutputFormat _outputFormat = MLCameraBase.OutputFormat.RGBA_8888;

    #endregion

    #region Magic Leap Camera Info
    private MLCamera _captureCamera;
    private bool _isCapturingVideo = false;
    #endregion

    private bool? _cameraPermissionGranted;
    private bool _isCameraInitializationInProgress;

    private readonly MLPermissions.Callbacks _permissionCallbacks = new MLPermissions.Callbacks();

    private Texture2D _videoTextureRgb;

    private Detector detector;

    private void Awake()
    {
        _permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        _permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        _permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
        _isCapturingVideo = false;

        detector = FindObjectOfType<Detector>();
    }

    void Start()
    {
        if (_startCameraCaptureOnStart)
        {
            StartCameraCapture(_cameraIdentifier,_targetImageWidth,_targetImageHeight);
        }
    }

    public void StartCameraCapture(MLCameraBase.Identifier cameraIdentifier = MLCameraBase.Identifier.CV, int width = 1920, int height = 1080, Action<bool> onCameraCaptureStarted = null)
    {
        if (_isCameraInitializationInProgress)
        {
            Debug.LogError("Camera Initialization is already in progress.");
            onCameraCaptureStarted?.Invoke(false);
            return;
        }

        this._cameraIdentifier = cameraIdentifier;
        _targetImageWidth = width;
        _targetImageHeight = height;
        TryEnableMLCamera(onCameraCaptureStarted);
    }

    private void OnDisable()
    {
        _ = DisconnectCameraAsync();
    }

    private void OnPermissionGranted(string permission)
    {
        if (permission == MLPermission.Camera)
        {
            _cameraPermissionGranted = true;
            Debug.Log($"Granted {permission}.");
        }
    }

    private void OnPermissionDenied(string permission)
    {
        if (permission == MLPermission.Camera)
        {
            _cameraPermissionGranted = false;
            Debug.LogError($"{permission} denied, camera capture won't function.");
        }
    }

    private async void TryEnableMLCamera(Action<bool> onCameraCaptureStarted = null)
    {
        if (_isCameraInitializationInProgress)
        {
            onCameraCaptureStarted?.Invoke(false);
            return;
        }

        _isCameraInitializationInProgress = true;

        _cameraPermissionGranted = null;
        Debug.Log("Requesting Camera permission.");
        MLPermissions.RequestPermission(MLPermission.Camera, _permissionCallbacks);

        while (!_cameraPermissionGranted.HasValue)
        {
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
        }

        if (MLPermissions.CheckPermission(MLPermission.Camera).IsOk || _cameraPermissionGranted.GetValueOrDefault(false))
        {
            Debug.Log("Initializing camera.");
            bool isCameraAvailable = await WaitForCameraAvailabilityAsync();

            if (isCameraAvailable)
            {
                await ConnectAndConfigureCameraAsync();
            }
        }

        _isCameraInitializationInProgress = false;
        onCameraCaptureStarted?.Invoke(_isCapturingVideo);
    }

    private async Task<bool> WaitForCameraAvailabilityAsync()
    {
        bool cameraDeviceAvailable = false;
        int maxAttempts = 10;
        int attempts = 0;
   
        while (!cameraDeviceAvailable && attempts < maxAttempts)
        {
            MLResult result =
                MLCameraBase.GetDeviceAvailabilityStatus(_cameraIdentifier, out cameraDeviceAvailable);

            if (result.IsOk == false && cameraDeviceAvailable == false)
            {
                await Task.Delay(TimeSpan.FromSeconds(1.0f));
            }
            attempts++;
        }

        return cameraDeviceAvailable;
    }

    private async Task<bool> ConnectAndConfigureCameraAsync()
    {
        Debug.Log("Starting Camera Capture.");

        MLCameraBase.ConnectContext context = CreateCameraContext();

        _captureCamera = await MLCamera.CreateAndConnectAsync(context);
        if (_captureCamera == null)
        {
            Debug.LogError("Could not create or connect to a valid camera. Stopping Capture.");
            return false;
        }

        Debug.Log("Camera Connected.");

        bool hasImageStreamCapabilities = GetStreamCapabilityWBestFit(out MLCameraBase.StreamCapability streamCapability);
        if (!hasImageStreamCapabilities)
        {
            Debug.LogError("Could not start capture. No valid Image Streams available. Disconnecting Camera.");
            await DisconnectCameraAsync();
            return false;
        }

        Debug.Log("Preparing camera configuration.");

        MLCameraBase.CaptureConfig captureConfig = CreateCaptureConfig(streamCapability);
        var prepareResult = _captureCamera.PrepareCapture(captureConfig, out MLCameraBase.Metadata _);
        if (!MLResult.DidNativeCallSucceed(prepareResult.Result, nameof(_captureCamera.PrepareCapture)))
        {
            Debug.LogError($"Could not prepare capture. Result: {prepareResult.Result} .  Disconnecting Camera.");
            await DisconnectCameraAsync();
            return false;
        }

        Debug.Log("Starting Video Capture.");

        bool captureStarted = await StartVideoCaptureAsync();
        if (!captureStarted)
        {
            Debug.LogError("Could not start capture. Disconnecting Camera.");
            await DisconnectCameraAsync();
            return false;
        }

        return _isCapturingVideo;
    }

    private MLCameraBase.ConnectContext CreateCameraContext()
    {
        var context = MLCameraBase.ConnectContext.Create();
        context.CamId = _cameraIdentifier;
        context.Flags = MLCameraBase.ConnectFlag.CamOnly;
        return context;
    }

    private MLCameraBase.CaptureConfig CreateCaptureConfig(MLCameraBase.StreamCapability streamCapability)
    {
        var captureConfig = new MLCameraBase.CaptureConfig();
        captureConfig.CaptureFrameRate = _targetFrameRate;
        captureConfig.StreamConfigs = new MLCameraBase.CaptureStreamConfig[1];
        captureConfig.StreamConfigs[0] = MLCameraBase.CaptureStreamConfig.Create(streamCapability, _outputFormat);
        return captureConfig;
    }

    private async Task<bool> StartVideoCaptureAsync()
    {
        await _captureCamera.PreCaptureAEAWBAsync();

        var startCapture = await _captureCamera.CaptureVideoStartAsync();
        _isCapturingVideo = MLResult.DidNativeCallSucceed(startCapture.Result, nameof(_captureCamera.CaptureVideoStart));

        if (!_isCapturingVideo)
        {
            Debug.LogError($"Could not start camera capture. Result : {startCapture.Result}");
            return false;
        }

        _captureCamera.OnRawVideoFrameAvailable += OnCaptureRawVideoFrameAvailable;
        return true;
    }

    private async Task DisconnectCameraAsync()
    {
        if (_captureCamera != null)
        {
            if (_isCapturingVideo)
            {
                await _captureCamera.CaptureVideoStopAsync();
                _captureCamera.OnRawVideoFrameAvailable -= OnCaptureRawVideoFrameAvailable;
            }

            await _captureCamera.DisconnectAsync();
            _captureCamera = null;
        }

        _isCapturingVideo = false;
    }

    private bool GetStreamCapabilityWBestFit(out MLCameraBase.StreamCapability streamCapability)
    {
        streamCapability = default;

        if (_captureCamera == null)
        {
            Debug.Log("Could not get Stream capabilities Info. No Camera Connected");
            return false;
        }

        MLCameraBase.StreamCapability[] streamCapabilities =
            MLCameraBase.GetImageStreamCapabilitiesForCamera(_captureCamera, MLCameraBase.CaptureType.Video);

        if (streamCapabilities.Length <= 0) 
            return false;

        if (MLCameraBase.TryGetBestFitStreamCapabilityFromCollection(streamCapabilities, _targetImageWidth,
                _targetImageHeight, MLCameraBase.CaptureType.Video,
                out streamCapability))
        {
            Debug.Log($"Stream: {streamCapability} selected with best fit.");
            return true;
        }

        Debug.Log($"No best fit found. Stream: {streamCapabilities[0]} selected by default.");
        streamCapability = streamCapabilities[0];
        return true;
    }

    private void OnCaptureRawVideoFrameAvailable(MLCameraBase.CameraOutput cameraOutput,
        MLCameraBase.ResultExtras resultExtras,
        MLCameraBase.Metadata metadata)
    {
        if (cameraOutput.Format == MLCamera.OutputFormat.RGBA_8888)
        {
            MLCamera.FlipFrameVertically(ref cameraOutput);
            UpdateRGBTexture(ref _videoTextureRgb, cameraOutput.Planes[0], _screenRendererRGB);
        }
    }

    private void UpdateRGBTexture(ref Texture2D videoTextureRGB, MLCamera.PlaneInfo imagePlane, Renderer renderer)
    {

        if (videoTextureRGB != null &&
            (videoTextureRGB.width != imagePlane.Width || videoTextureRGB.height != imagePlane.Height))
        {
            Destroy(videoTextureRGB);
            videoTextureRGB = null;
        }

        if (videoTextureRGB == null)
        {
            videoTextureRGB = new Texture2D((int)imagePlane.Width, (int)imagePlane.Height, TextureFormat.RGBA32, false);
            videoTextureRGB.filterMode = FilterMode.Bilinear;

            Material material = renderer.material;
            material.mainTexture = videoTextureRGB;
        }

        int actualWidth = (int)(imagePlane.Width * imagePlane.PixelStride);

        if (imagePlane.Stride != actualWidth)
        {
            var newTextureChannel = new byte[actualWidth * imagePlane.Height];
            for (int i = 0; i < imagePlane.Height; i++)
            {
                Buffer.BlockCopy(imagePlane.Data, (int)(i * imagePlane.Stride), newTextureChannel, i * actualWidth, actualWidth);
            }
            videoTextureRGB.LoadRawTextureData(newTextureChannel);
        }
        else
        {
            videoTextureRGB.LoadRawTextureData(imagePlane.Data);
        }
        videoTextureRGB.Apply();

        detector.QueueFrameToSend(videoTextureRGB.EncodeToJPG());
    }
}