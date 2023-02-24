using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class AgentDataDisplayManager : MonoBehaviour
{
    [SerializeField]
    Camera trainingCamera;
    AgentDataInterface target;

    [SerializeField]
    GameObject observationPrefab, actionPrefab;

    [SerializeField]
    GameObject agentName, observationDisplayer, actionDisplayer;

    [SerializeField]
    GameObject marker;

    List<GameObject> observationListItems, actionListItems;

    Color[] actionColors = new Color[3] { Color.red, Color.green, Color.blue };
    void Start()
    {
        observationListItems = new List<GameObject>();
        actionListItems = new List<GameObject>();
    }
    void Update()
    {
        SearchNewTarget();
        if (target != null)
        {
            UpdateTargetData();
            marker.transform.position = target.transform.position + Vector3.up;
        }
    }
    void SearchNewTarget()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Ray ray = trainingCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            foreach (RaycastHit hit in hits)
            {
                AgentDataInterface agentDataInterface = hit.collider.GetComponent<AgentDataInterface>();
                if (agentDataInterface != null)
                {
                    UpdateTarget(agentDataInterface);
                    break;
                }
            }
        }
    }
    void UpdateTarget(AgentDataInterface newTarget)
    {
        if (target != null)
        {
            foreach (Transform child in actionDisplayer.transform)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in observationDisplayer.transform)
            {
                Destroy(child.gameObject);
            }
            actionListItems.Clear();
            observationListItems.Clear();
        }
        target = newTarget;
        agentName.GetComponent<TMPro.TMP_Text>().text = newTarget.name + " (" + newTarget.GetAgentData().role.ToString() + ")";
        AgentDataStruct agentData = target.GetAgentData();
        for (int i = 0; i < agentData.discreteActions.Length; i++)
        {
            GameObject actionItem = Instantiate(actionPrefab, actionDisplayer.transform);
            actionListItems.Add(actionItem);
        }
        for (int i = 0; i < 4; i++)
        {
            GameObject observationItem = Instantiate(observationPrefab, observationDisplayer.transform);
            observationListItems.Add(observationItem);
        }
    }
    void UpdateTargetData()
    {
        AgentDataStruct agentData = target.GetAgentData();
        for (int i = 0; i < agentData.discreteActions.Length; i++)
        {
            actionListItems[i].GetComponent<UnityEngine.UI.Image>().color = actionColors[agentData.discreteActions[i]];
        }
        //print teammate distance up to 1 decimal
        observationListItems[0].GetComponent<TMPro.TMP_Text>().text = "Teammate distance : " + agentData.teamMateDist.ToString("F1");
        observationListItems[1].GetComponent<TMPro.TMP_Text>().text = "Ball distance : " + agentData.ballDist.ToString("F1");
        observationListItems[2].GetComponent<TMPro.TMP_Text>().text = "Cumulative reward : " + agentData.reward.ToString();
        observationListItems[3].GetComponent<TMPro.TMP_Text>().text = "Grounded : " + agentData.grounded.ToString();

        if (agentData.discreteActions[3] == 1)
        {
            Debug.DrawLine(target.transform.position, agentData.ball.position, Color.red, 0.1f);
        }
    }
}

