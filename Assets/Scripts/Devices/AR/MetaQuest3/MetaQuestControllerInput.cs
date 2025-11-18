using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Input;

public class MetaQuestControllerInput : MonoBehaviour
{
    private InputDevice rightController;
    private bool aButtonPressed;    
    private Transform cameraTransform;

    void Start() {
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        cameraTransform = Camera.main.transform;
    }
    
    void Update() {
        // Reacquire devices if they're not valid
        if (!rightController.isValid)
            rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        CheckButton(rightController, CommonUsages.primaryButton, ref aButtonPressed, "A Button");
        CheckThumbstick(rightController, "Right");
    }

    void CheckButton(InputDevice device, InputFeatureUsage<bool> button, ref bool wasPressed, string buttonName)
    {
        bool isPressed = false;

        if (device.TryGetFeatureValue(button, out isPressed) && isPressed)
        {
            TestSpawn();
        }
    }
    

    void CheckThumbstick(InputDevice device, string side)
    {
        bool thumbstickClick;
        if (device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out thumbstickClick))
        {
            if (thumbstickClick)
            {
                TestSpawn();
            }
        }
    }
    
        
    void TestSpawn() {
        NetworkObjectManager.Instance.ProcessMarkerServerRpc(
            new MarkerInfo {
                Id   = "Solar Panel",
                Pose = new UnityEngine.Pose(
                    cameraTransform.position + cameraTransform.forward * 0.4f, 
                    Quaternion.identity            
                ),
                Size = 0.035f
            }
        );
    }
}