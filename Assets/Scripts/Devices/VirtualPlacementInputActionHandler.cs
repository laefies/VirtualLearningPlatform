using UnityEngine;
using UnityEngine.InputSystem;

public class VirtualPlacementInputActionHandler : MonoBehaviour {
    private static float ROT_FACTOR = -100f;
    [SerializeField] private InputActionReference selectAction;
    [SerializeField] private InputActionReference confirmAction;
    [SerializeField] private InputActionReference cancelAction;
    [SerializeField] private InputActionReference rotateAction;
    [SerializeField] private InputActionReference positionAction;

    private bool placing = false;

    void OnEnable() {
        selectAction.action.Enable();
        confirmAction.action.Enable();
        cancelAction.action.Enable();
        rotateAction.action.Enable();
        positionAction.action.Enable();

        selectAction.action.performed  += OnSelect;
        confirmAction.action.performed += OnConfirm;
        cancelAction.action.performed  += OnCancel;
    }

    private void OnDisable() {
        selectAction.action.Disable();
        confirmAction.action.Disable();
        cancelAction.action.Disable();
        rotateAction.action.Disable();
        positionAction.action.Disable();

        selectAction.action.performed  -= OnSelect;
        confirmAction.action.performed -= OnConfirm;
        cancelAction.action.performed  -= OnCancel;
    }

    private void OnSelect(InputAction.CallbackContext context) {
        placing = VirtualPlacementSystem.Instance.InitPlacement();
        // TODO - chamar novo método "Select" que chama Init ou só Next 
    }

    private void OnConfirm(InputAction.CallbackContext context) {
        placing = VirtualPlacementSystem.Instance.ConfirmPlacement();
    }

    private void OnCancel(InputAction.CallbackContext context) {
        VirtualPlacementSystem.Instance.StopPlacement();
        placing = false;
    }

    void Update() {   
        if (placing)
        {
            Vector3 targetPos = positionAction.action.ReadValue<Vector3>();
            VirtualPlacementSystem.Instance.UpdatePreview(targetPos);

            float rotateValue = rotateAction.action.ReadValue<float>();
            if (rotateValue != 0) VirtualPlacementSystem.Instance?.RotateBy(rotateValue * ROT_FACTOR);
        }     
    }

}