using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{ 
    [SerializeField] Transform nova;
    // Update is called once per frame
    void Update()
    {
        // float mouseX = Input.GetAxis("MouseX");
        // float mouseY = Input.GetAxis("MouseY");
        float dz = Input.GetAxis("Horizontal");
        float dx = Input.GetAxis("Vertical");
        bool up = Input.GetKey(KeyCode.E);
        bool down = Input.GetKey(KeyCode.Q);
        float dy = up ? 1 : (down ? -1 : 0);
        Vector3 dr = transform.forward*dx + transform.up*dy + transform.right*dz;

        transform.position += dr * Time.deltaTime * (transform.position-nova.position).magnitude;
        transform.LookAt(nova.position);
    }
}
