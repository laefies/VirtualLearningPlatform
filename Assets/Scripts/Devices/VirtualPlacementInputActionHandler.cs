using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

public class VirtualPlacementInputActionHandler : MonoBehaviour {
    private static float ROT_FACTOR = -2f;
    private static float RAY_DIST = 100f;

    [SerializeField] private InputActionReference selectAction;
    [SerializeField] private InputActionReference confirmAction;
    [SerializeField] private InputActionReference cancelAction;
    [SerializeField] private InputActionReference rotateAction;
    [SerializeField] private InputActionReference aimPositionAction;
    [SerializeField] private InputActionReference aimRotationAction;

    private XROrigin xrOrigin;
    private bool placing = false;
    private ActionBasedSnapTurnProvider snapTurn;
    private ActionBasedContinuousTurnProvider continuousTurn;


    void OnEnable() {
        selectAction.action.Enable();
        confirmAction.action.Enable();
        cancelAction.action.Enable();
        rotateAction.action.Enable();

        selectAction.action.performed  += OnSelect;
        confirmAction.action.performed += OnConfirm;    
        cancelAction.action.performed  += OnCancel;

        xrOrigin = GetComponentInChildren<XROrigin>();
        snapTurn = GetComponentInChildren<ActionBasedSnapTurnProvider>();
        continuousTurn = GetComponentInChildren<ActionBasedContinuousTurnProvider>();
    }

    private void OnDisable() {
        selectAction.action.Disable();
        confirmAction.action.Disable();
        cancelAction.action.Disable();
        rotateAction.action.Disable();

        selectAction.action.performed  -= OnSelect;
        confirmAction.action.performed -= OnConfirm;
        cancelAction.action.performed  -= OnCancel;
    }

    private void HandleDuplicateInputs()
    {
        snapTurn.enabled = !placing;
        continuousTurn.enabled = !placing;
    }

    private void OnSelect(InputAction.CallbackContext context) {
        placing = VirtualPlacementSystem.Instance.InitPlacement();
        // TODO - chamar novo método "Select" que chama Init ou só Next 
        HandleDuplicateInputs();
    }

    private void OnConfirm(InputAction.CallbackContext context) {
        placing = !VirtualPlacementSystem.Instance.ConfirmPlacement();
        HandleDuplicateInputs();
    }

    private void OnCancel(InputAction.CallbackContext context) {
        VirtualPlacementSystem.Instance.StopPlacement();
        placing = false;
        HandleDuplicateInputs();
    }

    void Update() {   
        if (placing) {
            Vector3 aimPos = aimPositionAction.action.ReadValue<Vector3>();
            Quaternion aimRot = aimRotationAction.action.ReadValue<Quaternion>();

            Transform originTransform = xrOrigin.CameraFloorOffsetObject.transform;
            aimPos = originTransform.TransformPoint(aimPos);
            aimRot = originTransform.rotation * aimRot;

            RaycastHit physicsHit;
            bool hitPhysics = Physics.Raycast(new Ray(aimPos, aimRot * Vector3.forward), out physicsHit, RAY_DIST);    
            if (hitPhysics) VirtualPlacementSystem.Instance.UpdatePreview(physicsHit.point);

            Vector2 rotateValue = rotateAction.action.ReadValue<Vector2>();
            if (rotateValue.x != 0) VirtualPlacementSystem.Instance?.RotateBy(rotateValue.x * ROT_FACTOR);
        }     
    }
}