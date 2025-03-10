using System.Collections;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using static UnityEngine.XR.MagicLeap.MLCameraBase.Metadata;

public class ML2CameraManager : CameraManager
{
    private MLCamera colorCamera;
    private bool isCameraConnected;
    private bool cameraDeviceAvailable;
    private bool isCapturingImage;
    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

    protected override bool SetUpCamera()
    {
        MLResult result = MLPermissions.RequestPermission(MLPermission.Camera, permissionCallbacks);
        if (!result.IsOk)
        {
            enabled = false;
        }

        return true;
    }

    protected override async void CaptureFrame()
    {
        if (MLCamera.IsCaptureTypeSupported(colorCamera, MLCamera.CaptureType.Image))
        {
            isCapturingImage = true;

            var aeawbResult = await colorCamera.PreCaptureAEAWBAsync();
            if (aeawbResult.IsOk)
            {
                var result = await colorCamera.CaptureImageAsync(1);
                if (!result.IsOk)
                {
                    Debug.LogError("Image capture failed!");
                }
            }

            isCapturingImage = false;
        }
    }


    private void Awake()
    {
        texture = new Texture2D(8, 8, TextureFormat.RGB24, false);
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
    }

    void OnDisable()
    {
        permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;

        if (colorCamera != null && isCameraConnected)
        {
            colorCamera.Disconnect();
            isCameraConnected = false;
        }
    }

    private void OnPermissionGranted(string permission)
    {
        StartCoroutine(EnableMLCamera());
        StartCoroutine(CaptureFrameLoop());
    }

    private IEnumerator EnableMLCamera()
    {
        while (!cameraDeviceAvailable)
        {
            MLResult result = MLCamera.GetDeviceAvailabilityStatus(MLCamera.Identifier.Main, out cameraDeviceAvailable);
            if (!(result.IsOk && cameraDeviceAvailable))
            {
                yield return new WaitForSeconds(1.0f);
            }
        }

        ConnectCamera();
        while (!isCameraConnected)
        {
            yield return null;
        }

        ConfigureAndPrepareCapture();
    }

    private async void ConnectCamera()
    {
        MLCamera.ConnectContext context = MLCamera.ConnectContext.Create();
        context.EnableVideoStabilization = false;
        context.Flags = MLCameraBase.ConnectFlag.CamOnly;

        colorCamera = await MLCamera.CreateAndConnectAsync(context);

        if (colorCamera != null)
        {
            colorCamera.OnRawImageAvailable += OnCaptureRawImageComplete;
            isCameraConnected = true;
        }
    }

    private IEnumerator CaptureFrameLoop()
    {
        while (true)
        {
            if (isCameraConnected && !isCapturingImage)
            {
                if (MLCamera.IsCaptureTypeSupported(colorCamera, MLCamera.CaptureType.Image))
                {
                    CaptureFrame();
                }
            }
            yield return new WaitForSeconds(3.0f);
        }
    }


    private async void ConfigureAndPrepareCapture()
    {
        MLCamera.CaptureStreamConfig[] imageConfig = new MLCamera.CaptureStreamConfig[1]
        {
            new MLCamera.CaptureStreamConfig()
            {
                OutputFormat = MLCamera.OutputFormat.JPEG,
                CaptureType  = MLCamera.CaptureType.Image,
                Width = 1920, Height = 1080
            }
        };

        MLCamera.CaptureConfig captureConfig = new MLCamera.CaptureConfig()
        {
            StreamConfigs = imageConfig,
            CaptureFrameRate = MLCamera.CaptureFrameRate._30FPS
        };

        MLResult prepareCaptureResult = colorCamera.PrepareCapture(captureConfig, out MLCamera.Metadata _);
        if (!prepareCaptureResult.IsOk)
        {
            return;
        }
    }


    private void OnCaptureRawImageComplete(MLCamera.CameraOutput capturedImage, MLCamera.ResultExtras resultExtras, MLCamera.Metadata metadataHandle)
    {
        MLResult aeStateResult = metadataHandle.GetControlAEStateResultMetadata(out ControlAEState controlAEState);
        MLResult awbStateResult = metadataHandle.GetControlAWBStateResultMetadata(out ControlAWBState controlAWBState);

        if (aeStateResult.IsOk && awbStateResult.IsOk)
        {
            bool autoExposureComplete = controlAEState == MLCameraBase.Metadata.ControlAEState.Converged || controlAEState == MLCameraBase.Metadata.ControlAEState.Locked;
            bool autoWhiteBalanceComplete = controlAWBState == MLCameraBase.Metadata.ControlAWBState.Converged || controlAWBState == MLCameraBase.Metadata.ControlAWBState.Locked;

            if (autoExposureComplete && autoWhiteBalanceComplete)
            {
                if (capturedImage.Format == MLCameraBase.OutputFormat.JPEG)
                {
                    SendFrameToServer(capturedImage.Planes[0].Data);
                }
            }
        }

    }
}