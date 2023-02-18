using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentDataInterface : MonoBehaviour
{
    VolleyballAgent agent;

    public void Start()
    {
        agent = GetComponent<VolleyballAgent>();
    }
    public AgentDataStruct GetAgentData()
    {
        AgentDataStruct agentData = new AgentDataStruct();
        agentData.discreteActions = agent.GetLastActions();
        agentData.teamMateDist = Vector3.Distance(agent.transform.position, agent.player1.transform.position);
        agentData.ballDist = Vector3.Distance(agent.transform.position, agent.ball.transform.position);
        agentData.lastHitter = agent.envController.GetLastHitter();
        agentData.lastRole = agent.envController.GetLastRole();
        agentData.reward = agent.GetCumulativeReward();
        agentData.role = agent.roleId;
        agentData.name = agent.name;
        return agentData;
    }
}

public struct AgentDataStruct
{
    public string name;
    public int[] discreteActions;
    public float teamMateDist, ballDist;
    public Team lastHitter;
    public Role lastRole;
    public Role role;
    public float reward;
}