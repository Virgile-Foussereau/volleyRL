using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team
{
    Blue = 0,
    Purple = 1,
    Default = 2
}

public enum Role
{
    Hitter = 0,
    Setter = 1,
    Default = 2
}

public enum Touch
{

    Set = 0,
    Smash = 1,
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
    public VolleyballAgent blueAgent2;
    public VolleyballAgent purpleAgent1;
    public VolleyballAgent purpleAgent2;

    public List<VolleyballAgent> AgentsList = new List<VolleyballAgent>();
    List<Renderer> RenderersList = new List<Renderer>();

    Rigidbody blueAgentRb1;
    Rigidbody blueAgentRb2;
    Rigidbody purpleAgentRb1;
    Rigidbody purpleAgentRb2;

    public GameObject ball;
    Rigidbody ballRb;

    public GameObject blueGoal;
    public GameObject purpleGoal;

    Renderer blueGoalRenderer;

    Renderer purpleGoalRenderer;

    Team lastHitter;

    Team previousHitter;

    Role lastRole;

    Role previousRole;

    Touch lastTouch;

    Touch previousTouch;

    private int resetTimer;
    public int MaxEnvironmentSteps;

    void Start()
    {

        // Used to control agent & ball starting positions
        blueAgentRb1 = blueAgent1.GetComponent<Rigidbody>();
        blueAgentRb2 = blueAgent2.GetComponent<Rigidbody>();
        purpleAgentRb1 = purpleAgent1.GetComponent<Rigidbody>();
        purpleAgentRb2 = purpleAgent2.GetComponent<Rigidbody>();

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
        previousHitter = lastHitter;
        lastHitter = team;
    }

    public Team GetLastHitter()
    {
        return lastHitter;
    }

    public void UpdateLastRole(Role role)
    {
        previousRole = lastRole;
        lastRole = role;
    }

    public Role GetLastRole()
    {
        return lastRole;
    }

    public void UpdateLastTouch(Touch touch)
    {
        previousTouch = lastTouch;
        lastTouch = touch;
    }

    public Touch GetLastTouch()
    {
        return lastTouch;
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

                    if (lastRole == Role.Setter)
                    {
                        Vector3 dir_to_teamMate = (blueAgent1.transform.position - blueAgent2.transform.position).normalized;
                        float cos_angle = Vector3.Dot(dir_to_teamMate, flatVelocity) / (dir_to_teamMate.magnitude * flatVelocity.magnitude);
                        float malus_outOfBound = rmin / 2 * (cos_angle - 1);
                        blueAgent2.AddReward(malus_outOfBound);
                    }
                    else if (lastRole == Role.Hitter)
                    {
                        float cos_angle = Vector3.Dot(flatVelocity, -Vector3.forward) / (flatVelocity.magnitude * Vector3.forward.magnitude);
                        float malus_outOfBound = rmin / 2 * (cos_angle - 1);
                        blueAgent1.AddReward(malus_outOfBound);
                    }

                    // turn floor purple
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
                    if (lastRole == Role.Setter)
                    {
                        Vector3 dir_to_teamMate = (purpleAgentRb1.transform.position - purpleAgentRb2.transform.position).normalized;
                        float cos_angle = Vector3.Dot(dir_to_teamMate, flatVelocity) / (dir_to_teamMate.magnitude * flatVelocity.magnitude);
                        float malus_outOfBound = rmin / 2 * (cos_angle - 1);
                        purpleAgent2.AddReward(malus_outOfBound);
                    }
                    else if (lastRole == Role.Hitter)
                    {
                        float cos_angle = Vector3.Dot(flatVelocity, Vector3.forward) / (flatVelocity.magnitude * Vector3.forward.magnitude);
                        float malus_outOfBound = rmin / 2 * (cos_angle - 1);
                        purpleAgent1.AddReward(malus_outOfBound);
                    }

                    // turn floor blue
                    StartCoroutine(GoalScoredSwapGroundMaterial(volleyballSettings.blueGoalMaterial, RenderersList, .5f));

                }

                // end episode
                blueAgent1.EndEpisode();
                blueAgent2.EndEpisode();

                purpleAgent1.EndEpisode();
                purpleAgent2.EndEpisode();
                ResetScene();
                break;

            case Event.HitBlueGoal:
                // blue wins
                // blueAgent.AddReward(1f);
                // purpleAgent.AddReward(-1f);
                if (lastHitter == Team.Purple && lastRole == Role.Hitter && lastTouch == Touch.Smash)
                {
                    Vector3 flatVelocity = new Vector3(ballRb.velocity.x, 0, ballRb.velocity.z);
                    float cos_angle = Vector3.Dot(flatVelocity, Vector3.forward) / (flatVelocity.magnitude * Vector3.forward.magnitude);
                    float rmin = 0.3f;
                    float malusBadSmash = rmin / 2 * (cos_angle - 1);
                    purpleAgent1.AddReward(malusBadSmash);

                }
                else if (lastHitter == Team.Purple && lastRole == Role.Setter)
                {
                    float distToBall_purple = (ballRb.transform.position - purpleAgentRb1.transform.position).magnitude;
                    float malus_purple = -1f + Mathf.Exp(-0.5f * distToBall_purple);
                    purpleAgent1.AddReward(malus_purple);
                }
                else
                {
                    float distToBall_purple = (ballRb.transform.position - purpleAgentRb2.transform.position).magnitude;
                    float malus_purple = -1f + Mathf.Exp(-0.5f * distToBall_purple);
                    purpleAgent2.AddReward(malus_purple);
                }

                // turn floor blue
                StartCoroutine(GoalScoredSwapGroundMaterial(volleyballSettings.blueGoalMaterial, RenderersList, .5f));

                // end episode
                blueAgent1.EndEpisode();
                blueAgent2.EndEpisode();
                purpleAgent1.EndEpisode();
                purpleAgent2.EndEpisode();
                ResetScene();
                break;

            case Event.HitPurpleGoal:
                // purple wins
                // purpleAgent.AddReward(1f);
                //malus decrease if agent is closer to the ball
                if (lastHitter == Team.Blue && lastRole == Role.Hitter && lastTouch == Touch.Smash)
                {
                    Vector3 flatVelocity = new Vector3(ballRb.velocity.x, 0, ballRb.velocity.z);
                    float cos_angle = Vector3.Dot(flatVelocity, -Vector3.forward) / (flatVelocity.magnitude * Vector3.forward.magnitude);
                    float rmin = 0.3f;
                    float malusBadSmash = rmin / 2 * (cos_angle - 1);
                    blueAgent1.AddReward(malusBadSmash);
                }
                else if (lastHitter == Team.Blue && lastRole == Role.Setter)
                {
                    float distToBall_blue = (ballRb.transform.position - blueAgentRb1.transform.position).magnitude;
                    float malus_blue = -1f + Mathf.Exp(-0.5f * distToBall_blue);
                    blueAgent1.AddReward(malus_blue);
                }
                else
                {
                    float distToBall_blue = (ballRb.transform.position - blueAgentRb2.transform.position).magnitude;
                    float malus_blue = -1f + Mathf.Exp(-0.5f * distToBall_blue);
                    blueAgent2.AddReward(malus_blue);
                }

                // turn floor purple
                StartCoroutine(GoalScoredSwapGroundMaterial(volleyballSettings.purpleGoalMaterial, RenderersList, .5f));

                // end episode
                blueAgent1.EndEpisode();
                blueAgent2.EndEpisode();
                purpleAgent1.EndEpisode();
                purpleAgent2.EndEpisode();

                ResetScene();
                break;

            case Event.HitIntoBlueArea:
                if (lastHitter == Team.Purple)
                {
                    if (lastRole == Role.Hitter && lastTouch == Touch.Smash && previousHitter == Team.Purple && previousRole == Role.Setter && previousTouch == Touch.Set)
                    {
                        purpleAgent1.AddReward(1f);
                        purpleAgent2.AddReward(1f);
                    }
                }
                break;

            case Event.HitIntoPurpleArea:
                if (lastHitter == Team.Blue)
                {
                    if (lastRole == Role.Hitter && lastTouch == Touch.Smash && previousHitter == Team.Blue && previousRole == Role.Setter && previousTouch == Touch.Set)
                    {
                        blueAgent1.AddReward(1f);
                        blueAgent2.AddReward(1f);
                    }
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
            blueAgent2.EpisodeInterrupted();
            purpleAgent1.EpisodeInterrupted();
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
        lastRole = Role.Default; // reset last role
        lastTouch = Touch.Default; // reset last touch
        previousHitter = Team.Default; // reset previous hitter
        previousRole = Role.Default; // reset previous role
        previousTouch = Touch.Default; // reset previous touch

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
