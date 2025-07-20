using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [field:SerializeField] public PlayerControllerSettings DefaultSettings { get; private set; }
    public PlayerControllerSettings RuntimeSettings { get; private set; }
    
    private PlayerInput _input;
    private Rigidbody _rb;
    private BoxCollider _collider;
    private Coroutine _jumpBufferCoroutine;
    
    private float _currentGravity;
    private float _currentHorizontalAcceleration;
    
    private bool _wantsToJump;
    private bool _alignToGround = true;
    private bool _isJumping = false;
    private bool _isFalling = false;
    private bool _isGrounded;// for testing
    

    void Awake()
    {
        _input = GetComponent<PlayerInput>();
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponentInChildren<BoxCollider>();
        RuntimeSettings = Instantiate(DefaultSettings);
        
        if(_collider == null)
            Debug.LogError("No child box collider attached");
    }
    
    private void Update()
    {
        HandleJumpInput();
        CheckGroundStatus();
        HandleVariableJump();
    }

    private void FixedUpdate()
    {
        HandleHorizontalMovement();
        ApplyForwardMovement();
        ApplyRotation();
        GroundAlignment2();
        ApplyGravity();
        HandleJump();
    }

    

    #region Movement Logic
    void HandleHorizontalMovement()
    {
        float targetVelX = RuntimeSettings.horizontalSpeed * _input.HorizontalMovementInput;
        
        //isBraking simply means: are we trying to go the opposite way we are going right now?
        bool isChangingDirection = Mathf.Abs(_rb.linearVelocity.x) >
                                   Mathf.Abs(Mathf.MoveTowards(_rb.linearVelocity.x, targetVelX, 5f * Time.fixedDeltaTime));
        isChangingDirection = isChangingDirection && targetVelX != 0;
        
        float targetAcceleration = isChangingDirection ? RuntimeSettings.horizontalChangeAcceleration : 
                                   targetVelX == 0 ? RuntimeSettings.horizontalDeceleration : RuntimeSettings.horizontalAcceleration;
        
        bool isReachingMaxSpeed = Mathf.Abs(_rb.linearVelocity.x) > Mathf.Abs(targetVelX) * RuntimeSettings.terminalHorizontalSpeedTH;
        isReachingMaxSpeed = isReachingMaxSpeed && targetVelX != 0 && Mathf.Approximately(Mathf.Sign(_rb.linearVelocity.x), Mathf.Sign(targetVelX));

        float maxSpeedAcceleration =
                Helper.MapValue(Mathf.Abs(_rb.linearVelocity.x),
                Mathf.Abs(targetVelX) * RuntimeSettings.terminalHorizontalSpeedTH, Mathf.Abs(targetVelX),
                targetAcceleration, 0.1f);
        
        _currentHorizontalAcceleration = isReachingMaxSpeed ? maxSpeedAcceleration : isChangingDirection || targetVelX == 0f ? targetAcceleration :
            Mathf.MoveTowards(_currentHorizontalAcceleration, targetAcceleration, RuntimeSettings.horizontalAccelerationChangeSpeed * Time.fixedDeltaTime);
        
        Vector3 velocity = _rb.linearVelocity;
        velocity.x = Mathf.MoveTowards(velocity.x, targetVelX, _currentHorizontalAcceleration * Time.fixedDeltaTime);
        
        _rb.linearVelocity = velocity;
        
    }
    
    void ApplyForwardMovement()
    {
        Vector3 targetVelocity = _rb.linearVelocity;
        targetVelocity.z = RuntimeSettings.forwardSpeed;
        
        bool isBraking = targetVelocity.z < _rb.linearVelocity.z;
        float acceleration = isBraking ? RuntimeSettings.forwardDeceleration : RuntimeSettings.forwardAcceleration;
        
        _rb.linearVelocity = Vector3.MoveTowards(_rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }

    #endregion
    
    #region Jumping
    
    void HandleJumpInput()
    {
        if (!_input.JumpPressed) return;
        
        _wantsToJump = true;
            
        if(_jumpBufferCoroutine != null)
            StopCoroutine(_jumpBufferCoroutine);
            
        _jumpBufferCoroutine = StartCoroutine(JumpBufferCoroutine());
    }
    
    IEnumerator JumpBufferCoroutine()
    {
        yield return new WaitForSeconds(RuntimeSettings.jumpBufferTime);
        _wantsToJump = false;
    }

    void HandleJump()
    {
        if (!IsGrounded(out RaycastHit hit) || !_wantsToJump) return;
        
        _isJumping = true;
        _wantsToJump = false;
        _alignToGround = false;
        
        //compensate if we are not in the desired height (the ground spring)
        float yCorrection = RuntimeSettings.groundHeight - (transform.position.y - hit.point.y);
        
        // reset the gravity so the jump height will be correct
        _currentGravity = RuntimeSettings.gravity;
        
        Vector3 jumpVel = _rb.linearVelocity;
        jumpVel.y = Mathf.Sqrt((RuntimeSettings.jumpHeight + yCorrection) * -2f * Physics.gravity.y * RuntimeSettings.gravity);
        if(float.IsNaN(jumpVel.y)) return;
        _rb.linearVelocity = jumpVel;
    }

    void HandleVariableJump()
    {
        // is jumping is true when we started a jump al the way to where we land meaning that when we fall after the jump isJumping is true
        // isFalling is only true when we fall after a jump
        
        // if we are at the terminal gravity threshold we dont want to change the gravity
        // the CapFallSpeed() has different behavior
        if(_rb.linearVelocity.y < -RuntimeSettings.terminalVelocity * RuntimeSettings.terminalGravityTH) return;
        
        float targetGravity = CalculateTargetGravity(_rb.linearVelocity.y, _input.JumpHeld);
        _currentGravity = Mathf.MoveTowards(_currentGravity, targetGravity, RuntimeSettings.gravityChangeSpeed * Time.deltaTime);
        
        TryCutJump(_rb.linearVelocity.y, !_input.JumpHeld || _input.JumpReleased);
    }
    
    private float CalculateTargetGravity(float velocityY, bool jumpHeld)
    {
        if (!_isJumping)
            return RuntimeSettings.gravity;

        if (!jumpHeld)
            return RuntimeSettings.fallGravity;

        if (Mathf.Abs(velocityY) < RuntimeSettings.apexThreshold)
        {
            _isFalling = true;
            return RuntimeSettings.apexGravity;
        }

        if (velocityY < 0f)
        {
            _isFalling = true;
            return RuntimeSettings.fallGravity;
        }

        return RuntimeSettings.gravity;
    }
    
    private void TryCutJump(float velocityY, bool jumpReleased)
    {
        if (!_isFalling && _isJumping && jumpReleased && velocityY > RuntimeSettings.apexThreshold)
        {
            StartCoroutine(JumpCutCoroutine());
            _isFalling = true;
        }
    }

    IEnumerator JumpCutCoroutine()
    {
        Vector3 targetJumpVel;
        targetJumpVel.y = RuntimeSettings.jumpCutMultiplier * _rb.linearVelocity.y;
        
        while (_rb.linearVelocity.y > targetJumpVel.y)
        {
            Vector3 jumpVel = _rb.linearVelocity;
            jumpVel.y = Mathf.MoveTowards(_rb.linearVelocity.y, targetJumpVel.y, RuntimeSettings.jumpCutAcceleration * Time.fixedDeltaTime);
            _rb.linearVelocity = jumpVel;
            yield return new WaitForFixedUpdate();
        }
    }
    
    #endregion

    #region Gravity
    
    void ApplyGravity()
    {
        if(_alignToGround) return;
        
        Vector3 gravity = _currentGravity * Physics.gravity;
        
        _rb.linearVelocity += gravity * Time.fixedDeltaTime;
        
        CapFallSpeed();
    }

    void CapFallSpeed()
    { 
        //cap the fall speed
        Vector3 cappedVelocity = _rb.linearVelocity;
        cappedVelocity.y = Mathf.Clamp(cappedVelocity.y, -RuntimeSettings.terminalVelocity, Mathf.Infinity);
        _rb.linearVelocity = cappedVelocity;
        
        //smooth transition of the gravity towards reaching terminal velocity
        float gravity = _isJumping ? RuntimeSettings.fallGravity : RuntimeSettings.gravity;
        
        _currentGravity = _rb.linearVelocity.y < -RuntimeSettings.terminalVelocity * RuntimeSettings.terminalGravityTH ? 
                            Helper.MapValue(_rb.linearVelocity.y, 
                            -RuntimeSettings.terminalVelocity, -RuntimeSettings.terminalVelocity * RuntimeSettings.terminalGravityTH
                            , 0.1f, gravity) : 
                            _currentGravity;
    }
    
    #endregion
    
    #region Ground Checks and Alignment

    private void CheckGroundStatus()
    {
        _isGrounded = IsGrounded(RuntimeSettings.groundSpringExtraHeight);  // testing purposes
        if (!_alignToGround && _isGrounded && _rb.linearVelocity.y <= 0.1f)
        {
            _alignToGround = true;
            _isJumping = false;
            _isFalling = false;
        }
    }
    
    void GroundAlignment2()
    {
        if(!_alignToGround) return;

        bool isGrounded = IsGrounded(out RaycastHit hit,RuntimeSettings.groundSpringExtraHeight, true);
        
        // spring logic when on the ground
        if (isGrounded)
        {
            float currentY = transform.position.y;
            float targetY = hit.point.y + RuntimeSettings.groundHeight;
            float displacement = targetY - currentY;

            if (Mathf.Abs(displacement) > 0.01f)
            {
                // Apply spring force
                float springForce = (displacement * RuntimeSettings.groundSpringStrength) - (_rb.linearVelocity.y * RuntimeSettings.groundSpringDamping);
            
                Vector3 targetVelocity = _rb.linearVelocity;
                targetVelocity.y += springForce;

                _rb.linearVelocity = targetVelocity;
            }
            
        }
        else
        {
            _alignToGround = false;
        }
    }
    
    private bool IsGrounded(out RaycastHit hit, float extraDistance = 0f, bool isSpring = false)
    {
        if(!isSpring)
            return Physics.BoxCast
            (
                transform.position + RuntimeSettings.centerOffset, 
                RuntimeSettings.halfExtents, 
                Vector3.down, 
                out hit,Quaternion.identity,
                RuntimeSettings.groundHeight + extraDistance, 
                RuntimeSettings.groundLayer
            ); 
      
        Vector3 center = transform.position + RuntimeSettings.centerOffset + new Vector3(0f, 0f, RuntimeSettings.halfExtents.z);
        Vector3 halfExtents = new Vector3(RuntimeSettings.halfExtents.x, 0.05f, 0.1f);
        
        return Physics.BoxCast
        (
            center, 
            halfExtents, 
            Vector3.down, 
            out hit,Quaternion.identity,
            RuntimeSettings.groundHeight + extraDistance, 
            RuntimeSettings.groundLayer
        ); 
    }
    
    private bool IsGrounded(float extraDistance = 0f)
    {
        return IsGrounded(out _, extraDistance);
    }

    
    
    #endregion

    #region Rotation

    void ApplyRotation()
    {
        Quaternion alignRotation;

        if (_alignToGround && IsGrounded(out RaycastHit hit))
        {
            // Align rotation to slope normal
            alignRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
        }
        else
        {
            // Face movement direction in air
            Vector3 lookDirection = new Vector3(0, _rb.linearVelocity.y, _rb.linearVelocity.z);
            Vector3 blendDirection = Vector3.Lerp(Vector3.forward, lookDirection, RuntimeSettings.airRotationBlend);
            alignRotation = Quaternion.LookRotation(blendDirection);
        }

        // Extract baseRotation's Euler angles
        Vector3 euler = alignRotation.eulerAngles;

        // Apply Z-axis tilt based on player input
        float inputRotation = -_input.HorizontalMovementInput * RuntimeSettings.turningAngle;
        euler.z = inputRotation;
        euler.y = 0f;

        // Apply final rotation
        Quaternion finalRotation = Quaternion.Euler(euler);
        transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, RuntimeSettings.rotationSpeed * Time.fixedDeltaTime);
    }

    #endregion
    

    #if UNITY_EDITOR
    
    private void OnDrawGizmos()
    {
        float castDistance = DefaultSettings.groundHeight;
        Vector3 boxHalfExtents = DefaultSettings.halfExtents; // Change to match your actual BoxCast size
        Vector3 castDirection = Vector3.down;

        // Calculate the center of the box at the end of the cast
        Vector3 start = transform.position + DefaultSettings.centerOffset;
        Vector3 end = start + castDirection * castDistance;
        Vector3 springEnd = start + castDirection * (castDistance + DefaultSettings.groundSpringExtraHeight) + new Vector3(0f, 0f, DefaultSettings.halfExtents.z);
        Quaternion orientation = Quaternion.identity;

        // Draw the starting box (optional)
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(start, orientation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2f);

        // Draw the ending box
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(end, orientation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2f);
        
        // Draw the spring box
        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.TRS(springEnd, orientation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxHalfExtents.x, 0.05f, 0.1f) * 2f);

        // Draw the line between start and end
        Gizmos.color = Color.yellow;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawLine(start, end);
    }
    
    #endif
}
