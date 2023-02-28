using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectoryUtilityTest : MonoBehaviour
{
    public float initialBallSpeed, initialBallDist;
    public Transform target;
    public Rigidbody ball;
    public float impactY = 0;


    // Update is called once per frame
    void Update()
    {

        Vector3 initialDir = TrajectoryUtility.FindInitialDirection3D(target.position, transform.position, initialBallSpeed, 9.81f);
        Vector3 initialBallPos = transform.position + initialDir * initialBallDist;
        Vector3[] trajectory = TrajectoryUtility.CalculateTrajectory(initialBallPos, initialDir * initialBallSpeed, 9.81f);
        for (int i = 0; i < trajectory.Length - 1; i++)
        {
            Debug.DrawLine(trajectory[i], trajectory[i + 1], Color.red, 0.1f);
        }
        Vector3 impactPoint = TrajectoryUtility.PredictImpactPoint(initialBallPos, initialDir * initialBallSpeed, 9.81f, impactY);
        Debug.DrawLine(impactPoint, impactPoint + Vector3.up, Color.green, 0.1f);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ball.position = initialBallPos;
            ball.velocity = initialDir * initialBallSpeed;
        }
        Vector3 trueImpactPoint = TrajectoryUtility.PredictImpactPoint(ball.position, ball.velocity, 9.81f, impactY);
        Debug.DrawLine(trueImpactPoint, trueImpactPoint + Vector3.up, Color.blue, 0.1f);
    }
}
