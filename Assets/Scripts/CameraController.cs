using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] float zoomSensitivity, displacementSpeed;
    // Start is called before the first frame update

    void TranslateCamera()
    {
        Vector3 direction = Vector3.zero;
        direction += Input.GetAxisRaw("Horizontal") * Vector3.forward;
        direction -= Input.GetAxisRaw("Vertical") * Vector3.right;
        direction = direction.normalized * displacementSpeed * transform.position.y * Time.deltaTime;
        transform.position += direction;
    }

    void Zoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        transform.position -= Vector3.up * scroll * zoomSensitivity * Time.deltaTime * 1000;

    }

    // Update is called once per frame
    void Update()
    {
        TranslateCamera();
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        Zoom();
    }
}
