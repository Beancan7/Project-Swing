using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public PlayerData data;

    private Rigidbody2D rb;
    private BoxCollider2D bc;

    #region Serialized Variables

    [Header("Layers & Tags")]
    [SerializeField] private LayerMask _groundLayer;

    #endregion

    #region Variables

    // bools that check what the player is doing
    public bool isFacingRight { get; private set; }
    public bool isJumping { get; private set; }


    // floats that check certain variables for the timers 
    public float lastOnGroundTime { get; private set; }
    

    // Input variables 
    private Vector2 _moveInput;
    public float lastPressedJumpTime { get; private set; }


    // Jump
    private bool _isJumpCut;
    private bool _isJumpFalling;



    #endregion

    #region Check Parameters
    [Header("Checks")]
    [SerializeField] private Transform _groundCheckPoint;
    [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.49f,0.03f);
    [Space(5)]
    [SerializeField] private Transform _frontWallCheckPoint;
    [SerializeField] private Transform _backWallCheckPoint;
    [SerializeField] private Vector2 _wallCheckSize = new Vector2(0.5f,1f);
    #endregion


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bc = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        isFacingRight = true;
    }

    private void Update()
    {
        #region Timers
        lastOnGroundTime -= Time.deltaTime;
        lastPressedJumpTime -= Time.deltaTime;

        #endregion

        _moveInput.x = Input.GetAxisRaw("Horizontal");
        _moveInput.y = Input.GetAxisRaw("Vertical");

        if (_moveInput.x != 0)
            CheckDirectionToFace(_moveInput.x > 0);

        // This is so we can create a jump buffer that allows us to jump even if we press just before we hit the ground
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnJumpInput();
        }

        // This makes it so our jump velocity is cut the moment we let go of the jump button
        if (Input.GetKeyUp(KeyCode.Space))
        {
            OnJumpUpInput();
        }


        // This lets us jump
        if (CanJump() && lastPressedJumpTime > 0)
        {
            isJumping = true;
            _isJumpCut = false;
            _isJumpFalling = false;
            Jump();
        }


        if (isJumping && rb.velocity.y < 0)
        {
            isJumping = false;
        }

        if (lastOnGroundTime > 0 && !isJumping)
        {
            _isJumpCut = false;
            if (!isJumping)
                _isJumpFalling = false;
        }

        if (lastOnGroundTime > 0 && !isJumping)
        {
            _isJumpCut = false;
        }


        #region Collision Checks
        if (Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0, _groundLayer))
        {
            lastOnGroundTime = data.coyoteTime;
        }


        #endregion


        #region Gravity
        if (rb.velocity.y < 0 && _moveInput.y < 0 )
        {
            // Much higher gravity if holding down (to fall faster)
            SetGravityScale(data.gravityScale * data.fastFallGravityMult);
            // Caps max fall speed to not over accelerate
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -data.maxFastFallSpeed));

        }
        else if (_isJumpCut)
        {
            // Increases gravity when jump button is released
            SetGravityScale(data.gravityScale * data.jumpCutGravityMult);
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -data.maxFallSpeed));
        }
        else if ((isJumping || _isJumpFalling) && Mathf.Abs(rb.velocity.y) < data.jumpHangtimeThreshold)
        {
            // Sets gravity so you float a little at the peak of your jump
            SetGravityScale(data.gravityScale * data.jumpHangGravityMult);
        }
        else if (rb.velocity.y < 0)
        {
            // Higher gravity when just falling
            SetGravityScale(data.gravityScale * data.fallGravityMult);
            // Caps max fall speed
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -data.maxFallSpeed));
        }
        else
        {
            // Default gravity when on platform or going up
            SetGravityScale(data.gravityScale);
        }

        #endregion

    }


    private void FixedUpdate()
    {
        Run(data.runLerp);
    }


    public void OnJumpInput()
    {
        lastPressedJumpTime = data.jumpBuffer;
    }

    public void OnJumpUpInput()
    {
        if (CanJumpCut())
        {
            _isJumpCut = true;
        }
    }


    private void Run(float lerpAmount)
    {

        float targetSpeed = _moveInput.x * data.maxRunSpeed;
        targetSpeed = Mathf.Lerp(rb.velocity.x, targetSpeed, lerpAmount);

        #region Acceleration
        float accelRate;
        if (lastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.0f) ? data.runAccelRate : data.runDeccelRate;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.0f) ? data.runAccelRate * data.accelInAir : data.runDeccelRate * data.deccelInAir;
        #endregion

        #region Momentum
        if (data.doConserveMomentum && Mathf.Abs(rb.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(rb.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && lastOnGroundTime < 0)
            accelRate = 0;

        #endregion

        float speedDif = targetSpeed - rb.velocity.x;

        float movement = speedDif * accelRate;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);



    }


    private void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != isFacingRight)
            Turn();
    }


    // We start facing right always so this runs when we are moving in the direction we are not facing
    private void Turn()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        isFacingRight = !isFacingRight;
    }

    // This function is what adds the force to let us "jump"
    private void Jump()
    {
        lastPressedJumpTime = 0;
        lastOnGroundTime = 0;

        float force = data.jumpForce;
        if (rb.velocity.y < 0)
            force -= rb.velocity.y;

        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

    }

    private void SetGravityScale(float scale)
    {
        rb.gravityScale = scale;
    }



    #region Check Methods
    private bool CanJump()
    {
        return lastOnGroundTime > 0 && !isJumping;
    }

    private bool CanJumpCut()
    {
        return isJumping && rb.velocity.y > 0;
    }

    #endregion


    #region Editor Things
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(_groundCheckPoint.position, _groundCheckSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_frontWallCheckPoint.position, _wallCheckSize);
        Gizmos.DrawWireCube(_backWallCheckPoint.position, _wallCheckSize);

    }
    

    #endregion


}
