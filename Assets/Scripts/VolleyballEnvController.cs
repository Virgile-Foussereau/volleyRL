using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team
{
    Blue = 0,
    Purple = 1,
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
        lastHitter = team;
    }

    /// <summary>
    /// Resolves scenarios when ball enters a trigger and assigns rewards.
    /// Example reward functions are shown below.
    /// To enable Self-Play: Set either Purple or Blue Agent's Team ID to 1.
    /// </summary>
    void ApplyDistanceToBallMalus(VolleyballAgent[] agents)
    {

        foreach (VolleyballAgent agent in agents)
        {
            float distanceToBall = Vector3.Distance(agent.transform.position, ball.transform.position);
            float malus = Mathf.Exp(-0.5f * distanceToBall) - 1f;
            agent.AddReward(malus);
        }


    }

    void ApplyDirectionMalus(VolleyballAgent[] agents, Team team)
    {
        Vector3 flatVelocity = new Vector3(ballRb.velocity.x, 0, ballRb.velocity.z);
        if (flatVelocity.magnitude == 0)
        {
            return;
        }
        float rmin = 0.3f;
        float cos_angle = 0;
        if (team == Team.Blue)
        {
            cos_angle = Vector3.Dot(flatVelocity.normalized, -Vector3.forward);
        }
        else if (team == Team.Purple)
        {
            cos_angle = Vector3.Dot(flatVelocity.normalized, Vector3.forward);
        }
        float malus_outOfBound = rmin / 2 * (cos_angle - 1);
        foreach (VolleyballAgent agent in agents)
        {
            agent.AddReward(malus_outOfBound);
        }
    }

    void ApplyTeamMateDistanceMalus(VolleyballAgent[] agents)
    {
        foreach (VolleyballAgent agent in agents)
        {
            float distanceToClosestTeammate = Mathf.Infinity;
            foreach (VolleyballAgent teammate in agents)
            {
                if (teammate != agent)
                {
                    float dist = Vector3.Distance(agent.transform.position, teammate.transform.position);
                    if (dist < distanceToClosestTeammate)
                    {
                        distanceToClosestTeammate = dist;
                    }
                }
            }
            float malus = -0.1f * Mathf.Exp(-distanceToClosestTeammate);
            agent.AddReward(malus);
        }
    }
    public void ResolveEvent(Event triggerEvent)
    {
        switch (triggerEvent)
        {
            case Event.HitOutOfBounds:
                if (lastHitter == Team.Blue)
                {
                    ApplyDirectionMalus(new VolleyballAgent[] { blueAgent1, blueAgent2 }, Team.Blue);
                    ApplyTeamMateDistanceMalus(new VolleyballAgent[] { blueAgent1, blueAgent2 });
                }
                else if (lastHitter == Team.Purple)
                {
                    ApplyDirectionMalus(new VolleyballAgent[] { purpleAgent1, purpleAgent2 }, Team.Purple);
                    ApplyTeamMateDistanceMalus(new VolleyballAgent[] { purpleAgent1, purpleAgent2 });
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
                if (lastHitter == Team.Blue || lastHitter == Team.Default)
                {
                    ApplyDistanceToBallMalus(new VolleyballAgent[] { purpleAgent1, purpleAgent2 });
                }
                else
                {
                    ApplyDirectionMalus(new VolleyballAgent[] { purpleAgent1, purpleAgent2 }, Team.Purple);
                }
                ApplyTeamMateDistanceMalus(new VolleyballAgent[] { purpleAgent1, purpleAgent2 });
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
                if (lastHitter == Team.Purple || lastHitter == Team.Default)
                {
                    ApplyDistanceToBallMalus(new VolleyballAgent[] { blueAgent1, blueAgent2 });
                }
                else
                {
                    ApplyDirectionMalus(new VolleyballAgent[] { blueAgent1, blueAgent2 }, Team.Blue);
                }
                ApplyTeamMateDistanceMalus(new VolleyballAgent[] { blueAgent1, blueAgent2 });

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
                    purpleAgent1.AddReward(1);
                    purpleAgent2.AddReward(1);
                    //blueAgent.AddReward(-1);
                }
                break;

            case Event.HitIntoPurpleArea:
                if (lastHitter == Team.Blue)
                {
                    blueAgent1.AddReward(1);
                    blueAgent2.AddReward(1);
                    //purpleAgent.AddReward(-1);
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

        foreach (var agent in AgentsList)
        {
            // randomise starting positions and rotations
            var randomPosX = Random.Range(-2.75f, 2.75f);
            var randomPosZ = Random.Range(-2.75f, 2.75f);
            var randomPosY = 2f;
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
