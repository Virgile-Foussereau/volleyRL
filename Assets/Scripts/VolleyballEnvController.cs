using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team
{
    Blue = 0,
    Purple = 1,
    Default = 2
}

public enum Role {

    // The agent is a Hitter
    Hitter = 0,

    // The agent is a Setter
    Setter = 1,

    Default = 2
}

public enum Event
{
    HitPurpleGoal = 0,
    HitBlueGoal = 1,
    HitOutOfBounds = 2,
    HitIntoBlueArea = 3,
    HitIntoPurpleArea = 4
}

public class VolleyballEnvController : MonoBehaviour
{
    int ballSpawnSide;

    VolleyballSettings volleyballSettings;

    public VolleyballAgent blueAgent1;
    public VolleyballAgent purpleAgent1;

    public VolleyballAgent blueAgent2;
    public VolleyballAgent purpleAgent2;

    public List<VolleyballAgent> AgentsList = new List<VolleyballAgent>();
    List<Renderer> RenderersList = new List<Renderer>();

    Rigidbody blueAgent1Rb;
    Rigidbody purpleAgent1Rb;

    Rigidbody blueAgent2Rb;
    Rigidbody purpleAgent2Rb;

    public GameObject ball;
    Rigidbody ballRb;

    public GameObject blueGoal;
    public GameObject purpleGoal;

    Renderer blueGoalRenderer;

    Renderer purpleGoalRenderer;

    public Team lastHitter;

    public Role lastRoleToHit;
    

    private int resetTimer;
    public int MaxEnvironmentSteps;

    void Start()
    {

        // Used to control agent & ball starting positions
        blueAgent1Rb = blueAgent1.GetComponent<Rigidbody>();
        purpleAgent1Rb = purpleAgent1.GetComponent<Rigidbody>();
        blueAgent2Rb = blueAgent2.GetComponent<Rigidbody>();
        purpleAgent2Rb = purpleAgent2.GetComponent<Rigidbody>();
        ballRb = ball.GetComponent<Rigidbody>();

        // Starting ball spawn side
        // -1 = spawn blue side, 1 = spawn purple side
        var spawnSideList = new List<int> { -1, 1 };
        ballSpawnSide = spawnSideList[Random.Range(0, 2)];

        // Render ground to visualise which agent scored
        blueGoalRenderer = blueGoal.GetComponent<Renderer>();
        purpleGoalRenderer = purpleGoal.GetComponent<Renderer>();
        RenderersList.Add(blueGoalRenderer);
        RenderersList.Add(purpleGoalRenderer);

        volleyballSettings = FindObjectOfType<VolleyballSettings>();

        ResetScene();
    }

    /// <summary>
    /// Tracks which agent last had control of the ball
    /// </summary>
    public void UpdateLastHitter(Team team)
    {
        lastHitter = team;
    }

    public void UpdateLastRoleToHit(Role role)
    {
        lastRoleToHit = role;
    }

    /// <summary>
    /// Resolves scenarios when ball enters a trigger and assigns rewards.
    /// Example reward functions are shown below.
    /// To enable Self-Play: Set either Purple or Blue Agent's Team ID to 1.
    /// </summary>
    public void ResolveEvent(Event triggerEvent)
    {
        switch (triggerEvent)
        {
            case Event.HitOutOfBounds:
                if (lastHitter == Team.Blue)
                {
                    // apply penalty to blue agent
                    // blueAgent.AddReward(-0.1f);
                    // purpleAgent.AddReward(0.1f);
                    //compute angle between ballRb.velocity and the net
                    Vector3 flatVelocity = new Vector3(ballRb.velocity.x, 0, ballRb.velocity.z);
                    float rmin = 0.3f;
                    float cos_angle = Vector3.Dot(flatVelocity, -Vector3.forward) / (flatVelocity.magnitude * Vector3.forward.magnitude);
                    float malus_outOfBound = rmin/2 * (cos_angle - 1);
                    if (lastRoleToHit == Role.Hitter)
                    {
                        blueAgent1.AddReward(malus_outOfBound);
                    }
                    else if (lastRoleToHit == Role.Setter)
                    {
                        blueAgent2.AddReward(malus_outOfBound);
                    }

                    //turn floor purple
                    StartCoroutine(GoalScoredSwapGroundMaterial(volleyballSettings.purpleGoalMaterial, RenderersList, .5f));
                }
                else if (lastHitter == Team.Purple)
                {
                    // apply penalty to purple agent
                    // purpleAgent.AddReward(-0.1f);
                    // blueAgent.AddReward(0.1f);
                    //compute angle between ballRb.velocity and the net
                    Vector3 flatVelocity = new Vector3(ballRb.velocity.x, 0, ballRb.velocity.z);
                    float rmin = 0.3f;
                    float cos_angle = Vector3.Dot(flatVelocity, Vector3.forward) / (flatVelocity.magnitude * Vector3.forward.magnitude);
                    float malus_outOfBound = rmin/2 * (cos_angle - 1);
                    if (lastRoleToHit == Role.Hitter)
                    {
                        purpleAgent1.AddReward(malus_outOfBound);
                    }
                    else if (lastRoleToHit == Role.Setter)
                    {
                        purpleAgent2.AddReward(malus_outOfBound);
                    }

                    //turn floor purple
                    StartCoroutine(GoalScoredSwapGroundMaterial(volleyballSettings.blueGoalMaterial, RenderersList, .5f));

                }

                // end episode
                blueAgent1.EndEpisode();
                purpleAgent1.EndEpisode();
                blueAgent2.EndEpisode();
                purpleAgent2.EndEpisode();
                ResetScene();
                break;

            case Event.HitBlueGoal:
                // blue wins
                // blueAgent.AddReward(1f);
                // purpleAgent.AddReward(-1f);
                float distToBall_purple1 = (ballRb.transform.position - purpleAgent1Rb.transform.position).magnitude;
                float distToBall_purple2 = (ballRb.transform.position - purpleAgent2Rb.transform.position).magnitude;

                if (lastHitter == Team.Blue || lastHitter == Team.Default){
                    float malus_purple1 = -1 + Mathf.Exp(-0.5f * distToBall_purple1);
                    purpleAgent1.AddReward(malus_purple1);
                }
                else if (lastHitter == Team.Purple && lastRoleToHit == Role.Hitter){
                    purpleAgent1.AddReward(-0.5f);

                    float malus_purple2 = -1 + Mathf.Exp(-0.5f * distToBall_purple2);
                    purpleAgent2.AddReward(malus_purple2);
                }
                else if (lastHitter == Team.Purple && lastRoleToHit == Role.Setter){
                    float malus_purple2_bad_set = -1 + Mathf.Exp(-0.5f * distToBall_purple1);
                    purpleAgent2.AddReward(malus_purple2_bad_set);

                    float malus_purple1 = -1 + Mathf.Exp(-0.5f * distToBall_purple1);
                    purpleAgent1.AddReward(malus_purple1);
                }

                // turn floor blue
                StartCoroutine(GoalScoredSwapGroundMaterial(volleyballSettings.blueGoalMaterial, RenderersList, .5f));

                // end episode
                blueAgent1.EndEpisode();
                purpleAgent1.EndEpisode();
                blueAgent2.EndEpisode();
                purpleAgent2.EndEpisode();
                ResetScene();
                break;

            case Event.HitPurpleGoal:
                // purple wins
                // purpleAgent.AddReward(1f);
                //malus decrease if agent is closer to the ball
                float distToBall_blue1 = (ballRb.transform.position - blueAgent1Rb.transform.position).magnitude;
                float distToBall_blue2 = (ballRb.transform.position - blueAgent2Rb.transform.position).magnitude;
                if (lastHitter == Team.Purple || lastHitter == Team.Default){
                    float malus_blue1 = -1 + Mathf.Exp(-0.5f * distToBall_blue1);
                    blueAgent1.AddReward(malus_blue1);
                }
                else if (lastHitter == Team.Blue && lastRoleToHit == Role.Hitter){
                    blueAgent1.AddReward(-0.5f);

                    float malus_blue2 = -1 + Mathf.Exp(-0.5f * distToBall_blue2);
                    blueAgent2.AddReward(malus_blue2);
                }
                else if (lastHitter == Team.Blue && lastRoleToHit == Role.Setter){
                    float malus_blue2_bad_set = -1 + Mathf.Exp(-0.5f * distToBall_blue1);
                    blueAgent2.AddReward(malus_blue2_bad_set);

                    float malus_blue1 = -1 + Mathf.Exp(-0.5f * distToBall_blue1);
                    blueAgent1.AddReward(malus_blue1);
                }


                // turn floor purple
                StartCoroutine(GoalScoredSwapGroundMaterial(volleyballSettings.purpleGoalMaterial, RenderersList, .5f));

                // end episode
                blueAgent1.EndEpisode();
                purpleAgent1.EndEpisode();
                blueAgent2.EndEpisode();
                purpleAgent2.EndEpisode();
                ResetScene();
                break;

            case Event.HitIntoBlueArea:
                if (lastHitter == Team.Purple && lastRoleToHit == Role.Hitter)
                {
                    purpleAgent1.AddReward(0.1f);
                    purpleAgent2.AddReward(0.1f);
                }
                break;

            case Event.HitIntoPurpleArea:
                if (lastHitter == Team.Blue && lastRoleToHit == Role.Hitter)
                {
                    blueAgent1.AddReward(0.1f);
                    blueAgent2.AddReward(0.1f);
                }
                break;
        }
    }

    /// <summary>
    /// Changes the color of the ground for a moment.
    /// </summary>
    /// <returns>The Enumerator to be used in a Coroutine.</returns>
    /// <param name="mat">The material to be swapped.</param>
    /// <param name="time">The time the material will remain.</param>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, List<Renderer> rendererList, float time)
    {
        foreach (var renderer in rendererList)
        {
            renderer.material = mat;
        }

        yield return new WaitForSeconds(time); // wait for 2 sec

        foreach (var renderer in rendererList)
        {
            renderer.material = volleyballSettings.defaultMaterial;
        }

    }

    /// <summary>
    /// Called every step. Control max env steps.
    /// </summary>
    void FixedUpdate()
    {
        resetTimer += 1;
        if (resetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            blueAgent1.EpisodeInterrupted();
            purpleAgent1.EpisodeInterrupted();
            blueAgent2.EpisodeInterrupted();
            purpleAgent2.EpisodeInterrupted();
            ResetScene();
        }
    }

    /// <summary>
    /// Reset agent and ball spawn conditions.
    /// </summary>
    public void ResetScene()
    {
        resetTimer = 0;

        lastHitter = Team.Default; // reset last hitter
        lastRoleToHit = Role.Default; // reset last role

        foreach (var agent in AgentsList)
        {
            // randomise starting positions and rotations
            var randomPosX = Random.Range(-2f, 2f);
            var randomPosZ = Random.Range(-2f, 2f);
            var randomPosY = Random.Range(0.5f, 3.75f); // depends on jump height
            var randomRot = Random.Range(-45f, 45f);

            agent.transform.localPosition = new Vector3(randomPosX, randomPosY, randomPosZ);
            agent.transform.eulerAngles = new Vector3(0, randomRot, 0);

            agent.GetComponent<Rigidbody>().velocity = default(Vector3);
        }

        // reset ball to starting conditions
        ResetBall();
    }

    /// <summary>
    /// Reset ball spawn conditions
    /// </summary>
    void ResetBall()
    {
        var randomPosX = Random.Range(-2f, 2f);
        var randomPosZ = Random.Range(6f, 10f);
        var randomPosY = Random.Range(6f, 8f);

        // alternate ball spawn side
        // -1 = spawn blue side, 1 = spawn purple side
        ballSpawnSide = -1 * ballSpawnSide;

        if (ballSpawnSide == -1)
        {
            ball.transform.localPosition = new Vector3(randomPosX, randomPosY, randomPosZ);
        }
        else if (ballSpawnSide == 1)
        {
            ball.transform.localPosition = new Vector3(randomPosX, randomPosY, -1 * randomPosZ);
        }

        ballRb.angularVelocity = Vector3.zero;
        ballRb.velocity = Vector3.zero;
    }
}
