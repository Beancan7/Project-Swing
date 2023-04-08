using UnityEngine;

[CreateAssetMenu(menuName = "Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("Run")]
    public float runLerp;
    public float maxRunSpeed;
    public float runAccelRate;
    public float runDeccelRate;
    public float accelInAir;
    public float deccelInAir;
    public bool doConserveMomentum;


    [Header("Jump Parameters")]
    public float jumpForce;
    public float coyoteTime;
    public float jumpBuffer;
    public float jumpHangtimeThreshold;


    [Header("Gravity Parameters")]
    public float gravityScale;
    public float fastFallGravityMult;
    public float maxFastFallSpeed;
    public float maxFallSpeed;
    public float jumpCutGravityMult;
    public float jumpHangGravityMult;
    public float fallGravityMult;



}
