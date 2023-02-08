using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildCollisionDetector : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        transform.parent.gameObject.SendMessage("OnCollisionEnterChild", collision);
    }
}
