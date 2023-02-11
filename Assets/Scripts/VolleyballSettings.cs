using UnityEngine;

public class VolleyballSettings : MonoBehaviour
{
    public float agentRunSpeed = 1.5f;
    public float agentJumpHeight = 5f;
    public float agentJumpVelocity = 777;
    public float agentJumpVelocityMaxChange = 10;

    public float agentRange = 1.2f;
    public float ballTouchSpeed = 30f;
    public float ballSmashSpeed = 50f;

    // Slows down strafe & backward movement
    public float speedReductionFactor = 0.75f;

    public Material blueGoalMaterial;
    public Material purpleGoalMaterial;
    public Material defaultMaterial;

    // This is a downward force applied when falling to make jumps look less floaty
    public float fallingForce = 150;

    public float setTargetZOffset = 3f;
}
