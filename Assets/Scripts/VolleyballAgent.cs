using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public class VolleyballAgent : Agent
{
    public GameObject area;
    Rigidbody agentRb;
    BehaviorParameters behaviorParameters;
    public Team teamId;

    // To get ball's location for observations
    public GameObject ball;
    Rigidbody ballRb;

    // To get teammate's location for observations
    public GameObject teammate;
    Rigidbody teammateRb;

    // To get net's location for observations
    public GameObject net;

    Vector3 netPos;

    Vector3 setTargetPos;

    VolleyballSettings volleyballSettings;
    VolleyballEnvController envController;

    // Controls jump behavior
    float jumpingTime;
    Vector3 jumpTargetPos;
    Vector3 jumpStartingPos;
    float agentRot;

    float lastTouchTime = 0f;

    // The agent's role
    public Role agentRole;

    public Collider[] hitGroundColliders = new Collider[3];
    EnvironmentParameters resetParams;

    void Start()
    {
        envController = area.GetComponent<VolleyballEnvController>();
    }

    public override void Initialize()
    {
        volleyballSettings = FindObjectOfType<VolleyballSettings>();
        behaviorParameters = gameObject.GetComponent<BehaviorParameters>();

        agentRb = GetComponent<Rigidbody>();
        ballRb = ball.GetComponent<Rigidbody>();
        teammateRb = teammate.GetComponent<Rigidbody>();

        netPos = net.transform.position;
        setTargetPos = netPos + new Vector3(0, 0, volleyballSettings.setTargetZOffset*agentRot);

        // Set the agent's role
        if (behaviorParameters.BehaviorName == "Hitter")
        {
            agentRole = Role.Hitter;
        }
        else if (behaviorParameters.BehaviorName == "Setter")
        {
            agentRole = Role.Setter;
        }


        // for symmetry between player side
        if (teamId == Team.Blue)
        {
            agentRot = -1;
        }
        else
        {
            agentRot = 1;
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

    void Smash()
    {
        //calculate vector from agent to ball
        Vector3 agentToBall = ball.transform.position - this.transform.position;
        if (agentToBall.magnitude < volleyballSettings.agentRange)
        {
            if (Time.time - lastTouchTime < 0.5f)
            {
                return;
            }
            Vector3 planeNormal = Vector3.Cross(Vector3.up, agentToBall.normalized);
            Vector3 smashDir = Vector3.Cross(planeNormal, Vector3.up);
            ballRb.velocity = smashDir * volleyballSettings.ballSmashSpeed;
            lastTouchTime = Time.time;

            if (agentRole == Role.Setter) {
                this.AddReward(-0.5f);
            }
            else if (agentRole == Role.Hitter) {
                if (envController.lastHitter == teamId && envController.lastRoleToHit == Role.Setter) {
                    float distToLineOfAttack = this.transform.position.z - (netPos.z + volleyballSettings.setTargetZOffset);
                    distToLineOfAttack = Mathf.Abs(distToLineOfAttack);
                    if (distToLineOfAttack > 15f) {
                        Debug.Log("distToLineOfAttack is SUPERIOR to 15f");
                        Debug.Log("distToLineOfAttack: " + distToLineOfAttack);
                    }
                    this.AddReward(0.1f + 0.4f * (1 - distToLineOfAttack / 15f));
                    teammate.GetComponent<VolleyballAgent>().AddReward(0.1f + 0.4f * (1 - distToLineOfAttack / 15f));
                }
                else {
                    this.AddReward(-0.01f);
                }
            }

            // update last hitter
            envController.UpdateLastHitter(teamId);
            envController.UpdateLastRoleToHit(agentRole);
        }
    }

    /// <summary>
    /// Check if agent is on the ground to enable/disable jumping
    /// </summary>
    public bool CheckIfGrounded()
    {
        hitGroundColliders = new Collider[3];
        var o = gameObject;
        Physics.OverlapBoxNonAlloc(
            o.transform.localPosition + new Vector3(0, -0.05f, 0),
            new Vector3(0.95f / 2f, 0.5f, 0.95f / 2f),
            hitGroundColliders,
            o.transform.rotation);
        var grounded = false;
        foreach (var col in hitGroundColliders)
        {
            if (col != null && col.transform != transform &&
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
    /// Called when agent collides with the ball
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.CompareTag("ball"))
        {
            if (agentRole == Role.Setter) {
                if (envController.lastHitter == teamId && envController.lastRoleToHit == Role.Hitter) {
                float distToSetTarget = Vector3.Distance(this.transform.position, setTargetPos);
                if (distToSetTarget > 15f) {
                    Debug.Log("distToSetTarget is SUPERIOR to 15f");
                    Debug.Log("distToSetTarget: " + distToSetTarget);
                }
                this.AddReward(0.1f + 0.4f * (1 - distToSetTarget / 15f));
                teammate.GetComponent<VolleyballAgent>().AddReward(0.1f + 0.4f * (1 - distToSetTarget / 15f));
                float distTeammateToLineOfAttack = teammate.transform.position.z - (netPos.z + volleyballSettings.setTargetZOffset);
                distTeammateToLineOfAttack = Mathf.Abs(distTeammateToLineOfAttack);
                teammate.GetComponent<VolleyballAgent>().AddReward(0.1f * (1 - distTeammateToLineOfAttack / 15f));
                }
                else {
                    this.AddReward(-0.01f);
                }
            }
            else if (agentRole == Role.Hitter) {
                if (envController.lastHitter != teamId) {
                    float distTeammateToTarget = Vector3.Distance(teammate.transform.position, setTargetPos);
                    if (distTeammateToTarget > 15f) {
                        Debug.Log("distTeammateToTarget is SUPERIOR to 15f");
                        Debug.Log("distTeammateToTarget: " + distTeammateToTarget);
                    }
                    teammate.GetComponent<VolleyballAgent>().AddReward(0.1f*(1 - distTeammateToTarget / 15f));
                }
                else {
                    this.AddReward(-0.01f);
                }
            }


            envController.UpdateLastHitter(teamId);
            envController.UpdateLastRoleToHit(agentRole);
            
        }
    }

    /// <summary>
    /// Starts the jump sequence
    /// </summary>
    public void Jump()
    {
        jumpingTime = 0.2f;
        jumpStartingPos = agentRb.position;
    }

    /// <summary>
    /// Resolves the agent movement
    /// </summary>
    public void MoveAgent(ActionSegment<int> act)
    {
        var grounded = CheckIfGrounded();
        var dirToGo = Vector3.zero;
        var dirToGoForwardAction = act[0];
        var dirToGoSideAction = act[1];
        var jumpAction = act[2];
        var touchAction = act[3];

        //Debug.Log(teamId + " " + transform.forward);

        if (dirToGoForwardAction == 1)
            dirToGo += (grounded ? 1f : 0.5f) * Vector3.forward * 1f;
        else if (dirToGoForwardAction == 2)
            dirToGo += (grounded ? 1f : 0.5f) * Vector3.forward * -1f;
        if (dirToGoSideAction == 1)
            dirToGo += (grounded ? 1f : 0.5f) * Vector3.right * -1f;
        else if (dirToGoSideAction == 2)
            dirToGo += (grounded ? 1f : 0.5f) * Vector3.right;
        if (touchAction == 1)
            Smash();


        dirToGo = agentRot * dirToGo.normalized;

        if (jumpAction == 1)
            if (((jumpingTime <= 0f) && grounded))
            {
                Jump();
            }

        agentRb.AddForce(dirToGo * volleyballSettings.agentRunSpeed,
            ForceMode.VelocityChange);

        if (dirToGo.magnitude != 0f)
        {
            agentRb.transform.rotation = Quaternion.LookRotation(dirToGo);
        }


        if (jumpingTime > 0f)
        {
            jumpTargetPos =
                new Vector3(agentRb.position.x,
                    jumpStartingPos.y + volleyballSettings.agentJumpHeight,
                    agentRb.position.z) + dirToGo;

            MoveTowards(jumpTargetPos, agentRb, volleyballSettings.agentJumpVelocity,
                volleyballSettings.agentJumpVelocityMaxChange);
        }

        if (!(jumpingTime > 0f) && !grounded)
        {
            agentRb.AddForce(
                Vector3.down * volleyballSettings.fallingForce, ForceMode.Acceleration);
        }

        if (jumpingTime > 0f)
        {
            jumpingTime -= Time.fixedDeltaTime;
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscreteActions);
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        // Agent position (vector3)

        Vector3 agentPos = new Vector3((this.transform.position.x-netPos.x)*agentRot, this.transform.position.y-netPos.y, (this.transform.position.z-netPos.z)*agentRot);

        sensor.AddObservation(agentPos);

        // Vector from net to ball (direction to ball) (3 floats)
        Vector3 ballPos = new Vector3((ballRb.transform.position.x - netPos.x) * agentRot,
        (ballRb.transform.position.y - netPos.y),
        (ballRb.transform.position.z - netPos.z) * agentRot);

        sensor.AddObservation(ballPos);

        // Vector from agent to ball (direction to ball) (4 floats)
        Vector3 toBall = new Vector3((ballRb.transform.position.x - this.transform.position.x) * agentRot,
        (ballRb.transform.position.y - this.transform.position.y),
        (ballRb.transform.position.z - this.transform.position.z) * agentRot);

        sensor.AddObservation(toBall.normalized);
        sensor.AddObservation(toBall.magnitude);

        // Ball velocity (3 floats)
        sensor.AddObservation(ballRb.velocity.y);
        sensor.AddObservation(ballRb.velocity.z * agentRot);
        sensor.AddObservation(ballRb.velocity.x * agentRot);

        // Pose of teammate (3 floats)
        Vector3 teammatePos = new Vector3((teammate.transform.position.x - netPos.x) * agentRot,
        (teammate.transform.position.y - netPos.y),
        (teammate.transform.position.z - netPos.z) * agentRot);

        sensor.AddObservation(teammatePos);

        // last hitter
        if (envController.lastHitter == teamId)
        {
            if (envController.lastRoleToHit == agentRole)
            {
                sensor.AddObservation(2f);
            }
            else
            {
                sensor.AddObservation(1f);
            }
        }
        else
        {
            sensor.AddObservation(0f);
        }
    }

    // For human controller
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            // forward
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            // backward
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            // move left
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            // move right
            discreteActionsOut[1] = 2;
        }
        discreteActionsOut[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }
}
