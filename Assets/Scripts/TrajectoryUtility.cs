using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public static class TrajectoryUtility
{
    static float ZeroFunction(double theta, double xTarget, double yTarget, double initialVelocity, double g)
    {
        return (float)(Math.Tan(theta) - (yTarget + g / (2 * initialVelocity * initialVelocity) * Math.Pow(xTarget / Math.Cos(theta), 2)) / xTarget);
    }
    static float ZeroFunctionDerivative(double theta, double xTarget, double yTarget, double initialVelocity, double g)
    {
        return (float)(1 + Math.Pow(Math.Tan(theta), 2) - g / (initialVelocity * initialVelocity) * Math.Sin(theta) / Math.Pow(Math.Cos(theta), 3) * xTarget);
    }
    public static float FindAimAngle2D(float xTarget, float yTarget, float initialVelocity, float g)
    {

        double theta = Math.PI / 4;
        double thetaNew = 3 * Math.PI / 4;
        double thetaOld = 3 * Math.PI / 4;
        double epsilon = 0.0001;
        int maxIterations = 10000;
        int i = 0;
        do
        {
            thetaOld = thetaNew;
            thetaNew = thetaOld - ZeroFunction(thetaOld, xTarget, yTarget, initialVelocity, g) / ZeroFunctionDerivative(thetaOld, xTarget, yTarget, initialVelocity, g);
            i++;
            //pause Unity Editor
        } while (Math.Abs(thetaNew - thetaOld) > epsilon && i < maxIterations);
        theta = thetaNew;
        return (float)theta;
    }
    public static float FindAimAngle2DDichotomy(float xTarget, float yTarget, float initialVelocity, float g)
    {
        double theta1 = Math.PI / 4;
        double theta2 = Math.PI / 2 - 0.001;
        while (theta2 - theta1 > 0.001)
        {
            double theta = (theta1 + theta2) / 2;
            if (ZeroFunction(theta, xTarget, yTarget, initialVelocity, g) > 0)
            {
                theta1 = theta;
            }
            else
            {
                theta2 = theta;
            }
        }
        return (float)theta1;
    }

    public static Vector3 FindInitialDirection3D(Vector3 target, Vector3 initialBallPos, float initialVelocity, float g)
    {
        Vector3 targetRelative = target - initialBallPos;
        Vector3 uxPrime = Vector3.Cross(Vector3.Cross(Vector3.up, targetRelative), Vector3.up).normalized;

        float xTarget = Vector3.Dot(targetRelative, uxPrime);
        float yTarget = Vector3.Dot(targetRelative, Vector3.up);
        float theta = FindAimAngle2DDichotomy(xTarget, yTarget, initialVelocity, g);
        return uxPrime * Mathf.Cos(theta) + Vector3.up * Mathf.Sin(theta);
    }

    public static Vector3 PredictImpactPoint(Vector3 initialBallPos, Vector3 initialBallSpeed, float g, float impactY)
    {
        Vector3 uxPrime = Vector3.Cross(Vector3.Cross(Vector3.up, initialBallSpeed), Vector3.up).normalized;
        if (uxPrime != Vector3.zero)
        {
            float xPrimeSpeed = Vector3.Dot(initialBallSpeed, uxPrime);


            float xPrimeImpact = xPrimeSpeed * (initialBallSpeed.y / g + Mathf.Sqrt(Mathf.Pow(initialBallSpeed.y / g, 2) + 2 * (initialBallPos.y - impactY) / g));
            return uxPrime * xPrimeImpact + Vector3.up * impactY + new Vector3(initialBallPos.x, 0, initialBallPos.z);
        }
        else
        {
            return new Vector3(initialBallPos.x, impactY, initialBallPos.z);
        }
    }
    public static Vector3 PredictImpactVelocity(Vector3 initialBallPos, Vector3 initialBallSpeed, float g, float impactY)
    {
        Vector3 uxPrime = Vector3.Cross(Vector3.Cross(Vector3.up, initialBallSpeed), Vector3.up).normalized;
        if (uxPrime != Vector3.zero)
        {
            float xPrimeInitialSpeed = Vector3.Dot(initialBallSpeed, uxPrime);
            float yInitialSpeed = initialBallSpeed.y;

            float xPrimeImpactSpeed = xPrimeInitialSpeed;
            float yImpactSpeed = yInitialSpeed - g * (yInitialSpeed / g + Mathf.Sqrt(Mathf.Pow(yInitialSpeed / g, 2) + 2 * (initialBallPos.y - impactY) / g));

            return uxPrime * xPrimeImpactSpeed + Vector3.up * yImpactSpeed;
        }
        else
        {
            float yInitialSpeed = initialBallSpeed.y;
            return Vector3.up * (initialBallSpeed.y - g * (yInitialSpeed / g + Mathf.Sqrt(Mathf.Pow(yInitialSpeed / g, 2) + 2 * (initialBallPos.y - impactY) / g)));
        }
    }

    public static Vector3[] CalculateTrajectory(Vector3 initialBallPos, Vector3 initialBallSpeed, float g, int numPoints = 100, float timeStep = 0.1f)
    {
        Vector3[] trajectory = new Vector3[numPoints];
        float time = 0;
        for (int i = 0; i < trajectory.Length; i++)
        {
            trajectory[i] = initialBallPos + initialBallSpeed * time + Vector3.down * g * time * time / 2;
            time += timeStep;
        }
        return trajectory;
    }

}
