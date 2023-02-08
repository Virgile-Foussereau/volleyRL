using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public class VolleyballAgent : Agent
{
    public GameObject area;
    Rigidbody[] playerRbs;
    BehaviorParameters behaviorParameters;
    public Team teamId;

    // To get ball's location for observations
    public GameObject ball;
    Rigidbody ballRb;

    VolleyballSettings volleyballSettings;
    VolleyballEnvController envController;

    // Controls jump behavior
    float[] jumpingTimes;
    Vector3[] jumpTargetPoses;
    Vector3[] jumpStartingPoses;
    float[] playerRots;

    public Collider[] hitGroundColliders = new Collider[3];
    EnvironmentParameters resetParams;

    void Start()
    {
        envController = area.GetComponent<VolleyballEnvController>();
    }
    public Rigidbody[] GetPlayerRbs()
    {
        return playerRbs;
    }

    public override void Initialize()
    {
        volleyballSettings = FindObjectOfType<VolleyballSettings>();
        behaviorParameters = gameObject.GetComponent<BehaviorParameters>();

        playerRbs = GetComponentsInChildren<Rigidbody>();
        ballRb = ball.GetComponent<Rigidbody>();

        jumpingTimes = new float[playerRbs.Length];
        jumpTargetPoses = new Vector3[playerRbs.Length];
        jumpStartingPoses = new Vector3[playerRbs.Length];
        playerRots = new float[playerRbs.Length];


        // for symmetry between player side
        if (teamId == Team.Blue)
        {
            for (int i = 0; i < playerRbs.Length; i++)
            {
                playerRots[i] = -1;
            }
        }
        else
        {
            for (int i = 0; i < playerRbs.Length; i++)
            {
                playerRots[i] = 1;
            }
        }

        resetParams = Academy.Instance.EnvironmentParameters;
    }

    /// <summary>
    /// Moves  a rigidbody towards a position smoothly.
    /// </summary>
    /// <param name="targetPos">Target position.</param>
    /// <param name="rb">The rigidbody to be moved.</param>
    /// <param name="targetVel">The velocity to target during the
    ///  motion.</param>
    /// <param name="maxVel">The maximum velocity posible.</param>
    void MoveTowards(
        Vector3 targetPos, Rigidbody rb, float targetVel, float maxVel)
    {
        var moveToPos = targetPos - rb.worldCenterOfMass;
        var velocityTarget = Time.fixedDeltaTime * targetVel * moveToPos;
        if (float.IsNaN(velocityTarget.x) == false)
        {
            rb.velocity = Vector3.MoveTowards(
                rb.velocity, velocityTarget, maxVel);
        }
    }

    /// <summary>
    /// Check if player is on the ground to enable/disable jumping
    /// </summary>
    public bool CheckIfGrounded(int playerIndex)
    {
        hitGroundColliders = new Collider[3];
        var o = playerRbs[playerIndex].gameObject;
        Physics.OverlapBoxNonAlloc(
            o.transform.localPosition + new Vector3(0, -0.05f, 0),
            new Vector3(0.95f / 2f, 0.5f, 0.95f / 2f),
            hitGroundColliders,
            o.transform.rotation);
        var grounded = false;
        foreach (var col in hitGroundColliders)
        {
            if (col != null && col.transform != playerRbs[playerIndex].transform &&
                (col.CompareTag("walkableSurface") ||
                 col.CompareTag("purpleGoal") ||
                 col.CompareTag("blueGoal")))
            {
                grounded = true; //then we're grounded
                break;
            }
        }
        return grounded;
    }

    /// <summary>
    /// Called when player collides with the ball
    /// </summary>
    void OnCollisionEnterChild(Collision c)
    {
        if (c.gameObject.CompareTag("ball"))
        {
            envController.UpdateLastHitter(teamId);
        }
    }

    /// <summary>
    /// Starts the jump sequence
    /// </summary>
    public void Jump(int playerIndex)
    {
        jumpingTimes[playerIndex] = 0.2f;
        jumpStartingPoses[playerIndex] = playerRbs[playerIndex].position;
    }

    /// <summary>
    /// Resolves the player movement
    /// </summary>
    public void MovePlayer(ActionSegment<int> act, int playerIndex)
    {
        var grounded = CheckIfGrounded(playerIndex);
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        var dirToGoForwardAction = act[4 * playerIndex];
        var rotateDirAction = act[4 * playerIndex + 1];
        var dirToGoSideAction = act[4 * playerIndex + 2];
        var jumpAction = act[4 * playerIndex + 3];

        if (dirToGoForwardAction == 1)
            dirToGo = (grounded ? 1f : 0.5f) * playerRbs[playerIndex].transform.forward * 1f;
        else if (dirToGoForwardAction == 2)
            dirToGo = (grounded ? 1f : 0.5f) * playerRbs[playerIndex].transform.forward * volleyballSettings.speedReductionFactor * -1f;

        if (rotateDirAction == 1)
            rotateDir = playerRbs[playerIndex].transform.up * -1f;
        else if (rotateDirAction == 2)
            rotateDir = playerRbs[playerIndex].transform.up * 1f;

        if (dirToGoSideAction == 1)
            dirToGo = (grounded ? 1f : 0.5f) * playerRbs[playerIndex].transform.right * volleyballSettings.speedReductionFactor * -1f;
        else if (dirToGoSideAction == 2)
            dirToGo = (grounded ? 1f : 0.5f) * playerRbs[playerIndex].transform.right * volleyballSettings.speedReductionFactor;

        if (jumpAction == 1)
            if (((jumpingTimes[playerIndex] <= 0f) && grounded))
            {
                Jump(playerIndex);
            }

        playerRbs[playerIndex].transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        playerRbs[playerIndex].AddForce(playerRots[playerIndex] * dirToGo * volleyballSettings.playerRunSpeed,
            ForceMode.VelocityChange);

        if (jumpingTimes[playerIndex] > 0f)
        {
            jumpTargetPoses[playerIndex] =
                new Vector3(playerRbs[playerIndex].position.x,
                    jumpStartingPoses[playerIndex].y + volleyballSettings.playerJumpHeight,
                    playerRbs[playerIndex].position.z) + playerRots[playerIndex] * dirToGo;

            MoveTowards(jumpTargetPoses[playerIndex], playerRbs[playerIndex], volleyballSettings.playerJumpVelocity,
                volleyballSettings.playerJumpVelocityMaxChange);
        }

        if (!(jumpingTimes[playerIndex] > 0f) && !grounded)
        {
            playerRbs[playerIndex].AddForce(
                Vector3.down * volleyballSettings.fallingForce, ForceMode.Acceleration);
        }

        if (jumpingTimes[playerIndex] > 0f)
        {
            jumpingTimes[playerIndex] -= Time.fixedDeltaTime;
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        for (int i = 0; i < playerRbs.Length; i++)
        {
            MovePlayer(actionBuffers.DiscreteActions, i);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent rotation (1 float)
        for (int i = 0; i < playerRots.Length; i++)
        {
            sensor.AddObservation(playerRbs[i].transform.rotation.y);


            // Vector from agent to ball (direction to ball) (3 floats)
            Vector3 toBall = new Vector3((ballRb.transform.position.x - this.transform.position.x) * playerRots[i],
            (ballRb.transform.position.y - this.transform.position.y),
            (ballRb.transform.position.z - this.transform.position.z) * playerRots[i]);

            sensor.AddObservation(toBall.normalized);

            // Distance from the ball (1 float)
            sensor.AddObservation(toBall.magnitude);

            // Agent velocity (3 floats)
            sensor.AddObservation(playerRbs[i].velocity);

            // Ball velocity (3 floats)
            sensor.AddObservation(ballRb.velocity.y);
            sensor.AddObservation(ballRb.velocity.z * playerRots[i]);
            sensor.AddObservation(ballRb.velocity.x * playerRots[i]);
        }
    }

    // For human controller
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            // rotate right
            discreteActionsOut[1] = 2;
        }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            // forward
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            // rotate left
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            // backward
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            // move left
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            // move right
            discreteActionsOut[2] = 2;
        }
        discreteActionsOut[3] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }
}
