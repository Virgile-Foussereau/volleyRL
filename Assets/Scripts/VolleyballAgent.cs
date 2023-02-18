using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public class VolleyballAgent : Agent
{
    public GameObject area;
    Rigidbody agentRb;
    public GameObject player1;
    Rigidbody player1Rb;
    public GameObject player2;
    Rigidbody player2Rb;
    public GameObject player3;
    Rigidbody player3Rb;
    public GameObject player4;
    Rigidbody player4Rb;

    BehaviorParameters behaviorParameters;
    public Team teamId;

    public Role roleId;

    // To get ball's location for observations
    public GameObject ball;
    Rigidbody ballRb;

    // To get net's location for observations
    public GameObject net;

    Vector3 netPos;

    Vector3 setPos;

    Vector3 smashPos;

    VolleyballSettings volleyballSettings;
    public VolleyballEnvController envController;

    // Controls jump behavior
    float jumpingTime;
    Vector3 jumpTargetPos;
    Vector3 jumpStartingPos;
    float agentRot;

    public Collider[] hitGroundColliders = new Collider[10];
    EnvironmentParameters resetParams;

    int[] lastActions;
    void Start()
    {
        envController = area.GetComponent<VolleyballEnvController>();
    }

    public override void Initialize()
    {
        volleyballSettings = FindObjectOfType<VolleyballSettings>();
        behaviorParameters = GetComponent<BehaviorParameters>();


        agentRb = GetComponent<Rigidbody>();
        player1Rb = player1.GetComponent<Rigidbody>();
        player2Rb = player2.GetComponent<Rigidbody>();
        player3Rb = player3.GetComponent<Rigidbody>();
        player4Rb = player4.GetComponent<Rigidbody>();
        ballRb = ball.GetComponent<Rigidbody>();

        // for symmetry between player side
        if (teamId == Team.Blue)
        {
            agentRot = -1;
        }
        else
        {
            agentRot = 1;
        }

        netPos = net.transform.position;
        setPos = netPos + new Vector3(5*agentRot, 0, -3*agentRot);
        setPos.y = 0;
        smashPos = netPos + new Vector3(-5*agentRot, 0, -3*agentRot);
        smashPos.y = 0;

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
    void Set()
    {
        //calculate vector from agent to ball
        Vector3 agentToBall = ball.transform.position - transform.position;

        if (agentToBall.magnitude < volleyballSettings.agentRange)
        {
            if (envController.GetLastHitter() == teamId &&
                roleId == Role.Setter)
            {
                Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
                float distSetterToPos = Vector3.Distance(flatPos, setPos);
                float coef = 1.006f - Mathf.Exp(Mathf.Min(5f, distSetterToPos) - 5.2f);
                if (envController.GetLastRole() == Role.DefenderLeft)
                {
                    // add reward to relevant
                    player1.GetComponent<VolleyballAgent>().AddReward(1f*coef); //good defense
                    player3.GetComponent<VolleyballAgent>().AddReward(0.1f*coef); //set 
                }
                else if (envController.GetLastRole() == Role.DefenderRight)
                {
                    // add reward to relevant
                    player2.GetComponent<VolleyballAgent>().AddReward(1f*coef); //good defense
                    player3.GetComponent<VolleyballAgent>().AddReward(0.1f*coef); //set
                }
            }


            ballRb.velocity = agentToBall.normalized * volleyballSettings.ballTouchSpeed;
            envController.UpdateLastHitter(teamId);
            envController.UpdateLastRole(roleId);
            envController.UpdateLastTouch(Touch.Set);
        }
    }

    void Smash()
    {
        //calculate vector from agent to ball
        Vector3 agentToBall = ball.transform.position - transform.position;
        if (agentToBall.magnitude < volleyballSettings.agentRange)
        {
            if (envController.GetLastHitter() == teamId &&
                envController.GetPreviousHitter() == teamId &&
                envController.GetLastRole() == Role.Setter && roleId == Role.Hitter)
            {
                Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
                float distHitterToPos = Vector3.Distance(flatPos, smashPos);
                float coef = 1.006f - Mathf.Exp(Mathf.Min(5f, distHitterToPos) - 5.2f);
                if (envController.GetPreviousRole() == Role.DefenderLeft)
                {
                    // add reward to relevant
                    player1.GetComponent<VolleyballAgent>().AddReward(0.1f*coef); //good defense lead to good set
                    player3.GetComponent<VolleyballAgent>().AddReward(1f*coef); //good set 
                    player4.GetComponent<VolleyballAgent>().AddReward(0.1f*coef); //smash
                }
                else if (envController.GetPreviousRole() == Role.DefenderRight)
                {
                    // add reward to relevant
                    player2.GetComponent<VolleyballAgent>().AddReward(0.1f*coef); //good defense lead to good set
                    player3.GetComponent<VolleyballAgent>().AddReward(1f*coef); //good set 
                    player4.GetComponent<VolleyballAgent>().AddReward(0.1f*coef); //smash
                }
            }
            Vector3 planeNormal = Vector3.Cross(Vector3.up, agentToBall.normalized);
            Vector3 smashDir = Vector3.Cross(planeNormal, Vector3.up);
            ballRb.velocity = smashDir * volleyballSettings.ballSmashSpeed;

            envController.UpdateLastHitter(teamId);
            envController.UpdateLastRole(roleId);
            envController.UpdateLastTouch(Touch.Smash);
        }
    }

    /// <summary>
    /// Check if agent is on the ground to enable/disable jumping
    /// </summary>
    public bool CheckIfGrounded()
    {
        hitGroundColliders = new Collider[10];
        Physics.OverlapBoxNonAlloc(
            transform.localPosition + new Vector3(0, -0.05f, 0),
            new Vector3(0.95f / 2f, 0.5f, 0.95f / 2f),
            hitGroundColliders,
            transform.rotation);
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
            envController.UpdateLastHitter(teamId);
            envController.UpdateLastRole(roleId);
        }
    }

    /// <summary>
    /// Starts the jump sequence
    /// </summary>
    public void Jump()
    {
        if (roleId != Role.Hitter)
        {
            AddReward(-0.1f);
        }
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


        dirToGo = agentRot * dirToGo.normalized;

        if (jumpAction == 1)
            if (((jumpingTime <= 0f) && grounded))
            {
                Jump();
            }

        agentRb.AddForce(dirToGo * volleyballSettings.agentRunSpeed,
            ForceMode.VelocityChange);

        // Rotate the agent towards the direction it is moving
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

        if (touchAction == 1)
        {
            if (grounded)
            {
                Set();
            }
            else
            {
                if (roleId != Role.Hitter)
                {
                    Set();
                }
                else
                {
                    Smash();
                }
            }
        }

    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscreteActions);
        lastActions = actionBuffers.DiscreteActions.Array;
        if (roleId == Role.Hitter)
        {
            Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
            float distHitterToPos = Vector3.Distance(flatPos, smashPos);
            float coef = 1.0067f - Mathf.Exp(Mathf.Min(5f, distHitterToPos) - 5f);
            AddReward(0.0003f * (coef - 1f));
        }
        else if (roleId == Role.Setter)
        {
            Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
            float distSetterToPos = Vector3.Distance(flatPos, setPos);
            float coef = 1.0067f - Mathf.Exp(Mathf.Min(5f, distSetterToPos) - 5f);
            AddReward(0.0003f * (coef - 1f));
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        // vector to ball (vector3)  
        Vector3 toBall = new Vector3((ballRb.transform.position.x - this.transform.position.x) * agentRot,
        (ballRb.transform.position.y - this.transform.position.y),
        (ballRb.transform.position.z - this.transform.position.z) * agentRot);

        sensor.AddObservation(toBall.normalized);

        // Distance to ball (float)
        sensor.AddObservation(toBall.magnitude);

        // vector to relevant teammate (vector3)
        Vector3 toTeamMate;
        if (roleId == Role.Setter)
        {
            toTeamMate = new Vector3((player4Rb.transform.position.x - this.transform.position.x) * agentRot,
            (player4Rb.transform.position.y - this.transform.position.y),
            (player4Rb.transform.position.z - this.transform.position.z) * agentRot);
        }
        else
        {
            toTeamMate = new Vector3((player3Rb.transform.position.x - this.transform.position.x) * agentRot,
            (player3Rb.transform.position.y - this.transform.position.y),
            (player3Rb.transform.position.z - this.transform.position.z) * agentRot);
        }

        sensor.AddObservation(toTeamMate.normalized);

        // Distance to teammate (float)
        sensor.AddObservation(toTeamMate.magnitude);


        // Ball velocity (3 floats)
        sensor.AddObservation(ballRb.velocity.y);
        sensor.AddObservation(ballRb.velocity.z * agentRot);
        sensor.AddObservation(ballRb.velocity.x * agentRot);

        // is it my turn to play ? (binary)
        Team lastHitter = envController.GetLastHitter();
        Role lastRole = envController.GetLastRole();
        if (roleId == Role.DefenderLeft || roleId == Role.DefenderRight)
        {
            bool myTurn = lastHitter != teamId;
            sensor.AddObservation(myTurn ? 1 : 0);
        }
        else if (roleId == Role.Setter)
        {
            bool myTurn = lastHitter == teamId && (lastRole == Role.DefenderLeft || lastRole == Role.DefenderRight);
            sensor.AddObservation(myTurn ? 1 : 0);
        }
        else if (roleId == Role.Hitter)
        {
            bool myTurn = lastHitter == teamId && lastRole == Role.Setter;
            sensor.AddObservation(myTurn ? 1 : 0);
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

    public int[] GetLastActions()
    {
        return lastActions;
    }
}
