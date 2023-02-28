using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    public Transform ball;

    // Update is called once per frame
    void Update()
    {
        if (ball.position.magnitude > 50)
        {
            ball.position = transform.position;
            //Randomize ball velocity
            ball.GetComponent<Rigidbody>().velocity = new Vector3(Random.Range(-5, 5), Random.Range(0, 5), Random.Range(-5, 5));
        }
    }
}
