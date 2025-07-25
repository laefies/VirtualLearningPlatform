using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using System.Collections.Generic;

using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features;

[Serializable]
public class IntrinsicParameters
{
    public int width;
    public int height;
    public float fov;
    public float[] focalLength;
    public float[] principalPoint;
    public double[] distortion;
}

[Serializable]
public class ExtrinsicParameters
{
    public float[] position;
    public float[] rotation;
}

[RequireComponent(typeof(Detector))]
public class ML2CameraManager : MonoBehaviour
{
    public bool IsCameraConnected => _captureCamera != null && _captureCamera.ConnectionEstablished;
    
    [SerializeField][Tooltip("If true, the camera capture will start immediately.")]
    private bool _startCameraCaptureOnStart = true;

    private Texture2D _frameTexture;

    #region Capture Config
    private int _targetImageWidth = 640;
    private int _targetImageHeight = 360;
    private MLCameraBase.Identifier _cameraIdentifier = MLCameraBase.Identifier.CV;
    private MLCameraBase.CaptureFrameRate _targetFrameRate = MLCameraBase.CaptureFrameRate._30FPS;
    private MLCameraBase.OutputFormat _outputFormat = MLCameraBase.OutputFormat.RGBA_8888;
    #endregion

    #region Initialization Config
    private bool? _cameraPermissionGranted;
    private bool _isCameraInitializationInProgress;
    private readonly MLPermissions.Callbacks _permissionCallbacks = new MLPermissions.Callbacks();
    #endregion

    #region Camera Info
    private MLCamera _captureCamera;
    private bool _isCapturingVideo = false;
    #endregion

    private XRInputSubsystem inputSubsystem;


    private void Awake()
    {
        _permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        _permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        _permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
        _isCapturingVideo = false;
    }

    void Start()
    {
        inputSubsystem = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRInputSubsystem>();
        SetSpace(TrackingOriginModeFlags.Unbounded);

        if (_startCameraCaptureOnStart)
        {
            StartCameraCapture(_cameraIdentifier, _targetImageWidth, _targetImageHeight);
        }
    }

    private void SetSpace(TrackingOriginModeFlags flag)
    {
        if (inputSubsystem.TrySetTrackingOriginMode(flag))
        {
            Debug.Log($"Current Space: {inputSubsystem.GetTrackingOriginMode()}");
            inputSubsystem.TryRecenter();
        }
        else
        {
            Debug.LogError($"SetSpace failed to set Tracking Mode Origin to {flag}");
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
        MLPermissions.RequestPermission(MLPermission.Camera, _permissionCallbacks);

        while (!_cameraPermissionGranted.HasValue)
        {
            await Task.Delay(TimeSpan.FromSeconds(1.0f));
        }

        if (MLPermissions.CheckPermission(MLPermission.Camera).IsOk || _cameraPermissionGranted.GetValueOrDefault(false))
        {
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
            MLResult result = MLCameraBase.GetDeviceAvailabilityStatus(_cameraIdentifier, out cameraDeviceAvailable);
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
        MLCameraBase.ConnectContext context = MLCameraBase.ConnectContext.Create();
        context.CamId = _cameraIdentifier;
        context.Flags = MLCameraBase.ConnectFlag.CamOnly;

        _captureCamera = await MLCamera.CreateAndConnectAsync(context);
        if (_captureCamera == null)
        {
            Debug.LogError("Could not create or connect to a valid camera.");
            return false;
        }

        if (!GetStreamCapabilityWBestFit(out MLCameraBase.StreamCapability streamCapability))
        {
            Debug.LogError("No valid Image Streams available. Disconnecting Camera.");
            await DisconnectCameraAsync();
            return false;
        }

        var captureConfig = new MLCameraBase.CaptureConfig();
        captureConfig.CaptureFrameRate = _targetFrameRate;
        captureConfig.StreamConfigs = new MLCameraBase.CaptureStreamConfig[1];
        captureConfig.StreamConfigs[0] = MLCameraBase.CaptureStreamConfig.Create(streamCapability, _outputFormat);

        var prepareResult = _captureCamera.PrepareCapture(captureConfig, out MLCameraBase.Metadata _);
        if (!MLResult.DidNativeCallSucceed(prepareResult.Result, nameof(_captureCamera.PrepareCapture)))
        {
            Debug.LogError($"Could not prepare capture. Result: {prepareResult.Result}");
            await DisconnectCameraAsync();
            return false;
        }

        return await StartVideoCaptureAsync();
    }

    private async Task<bool> StartVideoCaptureAsync()
    {
        await _captureCamera.PreCaptureAEAWBAsync();

        var startCapture = await _captureCamera.CaptureVideoStartAsync();
        _isCapturingVideo = MLResult.DidNativeCallSucceed(startCapture.Result, nameof(_captureCamera.CaptureVideoStart));

        if (!_isCapturingVideo)
        {
            Debug.LogError($"Could not start camera capture. Result: {startCapture.Result}");
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
            return true;
        }

        streamCapability = streamCapabilities[0];
        return true;
    }

    private void OnCaptureRawVideoFrameAvailable(MLCameraBase.CameraOutput cameraOutput,
        MLCameraBase.ResultExtras resultExtras,
        MLCameraBase.Metadata metadata)
    {

        if (cameraOutput.Format == MLCamera.OutputFormat.RGBA_8888)
        {
            if (Detector.Instance.IsAvailable()) {

                // Prepare frame
                MLCamera.FlipFrameVertically(ref cameraOutput);
                ProcessFrame(cameraOutput.Planes[0]);

                // Intrinsic Parameters
                IntrinsicParameters intrinsic = null;
                if (resultExtras.Intrinsics != null) {
                    intrinsic = new IntrinsicParameters
                    {
                        width          = (int)resultExtras.Intrinsics.Value.Width,
                        height         = (int)resultExtras.Intrinsics.Value.Height,
                        fov            = resultExtras.Intrinsics.Value.FOV,
                        focalLength    = new float[] {
                            resultExtras.Intrinsics.Value.FocalLength.x,
                            resultExtras.Intrinsics.Value.FocalLength.y
                        },
                        principalPoint = new float[] {
                            resultExtras.Intrinsics.Value.PrincipalPoint.x,
                            resultExtras.Intrinsics.Value.PrincipalPoint.y
                        },
                        distortion = resultExtras.Intrinsics.Value.Distortion
                    };
                }

                // Extrinsic Parameters
                MLResult result = MLCVCamera.GetFramePose(resultExtras.VCamTimestamp, out Matrix4x4 poseMatrix);
                ExtrinsicParameters extrinsic = null;
                if (result.IsOk)
                {
                    extrinsic = new ExtrinsicParameters
                    {
                        position = new float[] {
                            poseMatrix.GetPosition().x,
                            poseMatrix.GetPosition().y, 
                            poseMatrix.GetPosition().z
                        },
                        rotation = new float[] {
                            poseMatrix.rotation.x, poseMatrix.rotation.y,
                            poseMatrix.rotation.z, poseMatrix.rotation.w
                        }
                    };
                }

                // Prepare to send
                Detector.Instance.SendMessageAsync(
                    new DetectionRequest
                    {
                        frameData       = Convert.ToBase64String(_frameTexture?.EncodeToJPG(20)),
                        // intrinsicParams = intrinsic,
                        // extrinsicParams = extrinsic
                    }
                );
            }
        }
    }

    private void ProcessFrame(MLCamera.PlaneInfo imagePlane)
    {
        if (_frameTexture == null || 
            _frameTexture.width != imagePlane.Width || 
            _frameTexture.height != imagePlane.Height)
        {
            if (_frameTexture != null)
                Destroy(_frameTexture);
                
            _frameTexture = new Texture2D((int)imagePlane.Width, (int)imagePlane.Height, TextureFormat.RGBA32, false);
        }

        int actualWidth = (int)(imagePlane.Width * imagePlane.PixelStride);
        if (imagePlane.Stride != actualWidth)
        {
            var newTextureData = new byte[actualWidth * imagePlane.Height];
            for (int i = 0; i < imagePlane.Height; i++)
            {
                Buffer.BlockCopy(imagePlane.Data, (int)(i * imagePlane.Stride), newTextureData, i * actualWidth, actualWidth);
            }
            _frameTexture.LoadRawTextureData(newTextureData);
        }
        else
        {
            _frameTexture.LoadRawTextureData(imagePlane.Data);
        }
        
        _frameTexture.Apply();
    }

}