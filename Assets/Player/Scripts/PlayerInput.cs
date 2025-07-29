using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-1)]
public class PlayerInput : MonoBehaviour, PlayerControls.IActionsActions
{
    public PlayerControls PlayerControls { get; private set; }
    public float HorizontalMovementInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpReleased { get; private set; }
    public bool JumpHeld { get; private set; }
    
    public bool ShootHeld { get; private set; }
    
    private void OnEnable()
    {
        EnableInput();
    }

    private void OnDisable()
    {
        DisableInput();
    }

    private void LateUpdate()
    {
        JumpPressed = false;
        JumpReleased = false;
    }
    

    void EnableInput()
    {
        PlayerControls = new PlayerControls();
        PlayerControls.Enable();

        PlayerControls.Actions.Enable();
        PlayerControls.Actions.SetCallbacks(this);
    }
    
    void DisableInput()
    {
        PlayerControls.Actions.Disable();
        PlayerControls.Actions.RemoveCallbacks(this);
        
        PlayerControls.Dispose();
        PlayerControls = null;
    }

   

    public void OnSideMovement(InputAction.CallbackContext context)
    {
        HorizontalMovementInput = context.ReadValue<float>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        JumpHeld = context.ReadValueAsButton();
        
        if (context.performed)
            JumpPressed = true;
        
        if(context.canceled)
            JumpReleased = true;
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        ShootHeld = context.ReadValueAsButton();
    }
}
