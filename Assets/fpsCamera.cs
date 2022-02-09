using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fpsCamera : MonoBehaviour
{
    public float turnSpeed = 4.0f;
    public float moveSpeed = 2.0f;

    public float minTurnAngle = -90.0f;
    public float maxTurnAngle = 90.0f;
    private float rotX;

    Vector3[] vertices;
    List<Vector3> modifiedVerts = new List<Vector3>();

    void Update()
    {
        MouseAiming();
        KeyboardMovement();

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            // Does the ray intersect any objects excluding the player layer
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity))
            {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow, 10f);

                Mesh mesh = hit.transform.GetComponent<MeshFilter>().mesh;
                vertices = mesh.vertices;
                foreach (Vector3 vert in vertices)
                {
                    modifiedVerts.Add(vert);
                }
                
                for (int i = 0; i < mesh.triangles.Length; i += 3)
                {
                    int a = mesh.triangles[i];
                    int b = mesh.triangles[i + 1];
                    int c = mesh.triangles[i + 2];

                    bool onTriangle = true;
                    Vector3 cp1 = Vector3.Cross(modifiedVerts[b] - modifiedVerts[a], hit.point - modifiedVerts[a]);
                    Vector3 cp2 = Vector3.Cross(modifiedVerts[b] - modifiedVerts[a], modifiedVerts[c] - modifiedVerts[a]);
                    if (Vector3.Dot(cp1, cp2) <= 0)
                    {
                        onTriangle = false;
                    }
                    cp1 = Vector3.Cross(modifiedVerts[b] - modifiedVerts[c], hit.point - modifiedVerts[c]);
                    cp2 = Vector3.Cross(modifiedVerts[b] - modifiedVerts[c], modifiedVerts[a] - modifiedVerts[c]);
                    if (Vector3.Dot(cp1, cp2) <= 0)
                    {
                        onTriangle = false;
                    }
                    cp1 = Vector3.Cross(modifiedVerts[c] - modifiedVerts[a], hit.point - modifiedVerts[a]);
                    cp2 = Vector3.Cross(modifiedVerts[c] - modifiedVerts[a], modifiedVerts[b] - modifiedVerts[a]);
                    if (Vector3.Dot(cp1, cp2) <= 0)
                    {
                        onTriangle = false;
                    }

                    if (onTriangle == true)
                    {
                        Debug.Log(true);
                    }
                }
            }
            else
            {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
                Debug.Log("No Hit");
            }
        }
    }

    void MouseAiming()
    {
        // get the mouse inputs
        float y = Input.GetAxis("Mouse X") * turnSpeed;
        rotX += Input.GetAxis("Mouse Y") * turnSpeed;

        // clamp the vertical rotation
        rotX = Mathf.Clamp(rotX, minTurnAngle, maxTurnAngle);

        // rotate the camera
        transform.eulerAngles = new Vector3(-rotX, transform.eulerAngles.y + y, 0);
    }

    void KeyboardMovement()
    {
        Vector3 dir = new Vector3(0, 0, 0);

        dir.x = Input.GetAxis("Horizontal");
        dir.z = Input.GetAxis("Vertical");

        transform.Translate(dir * moveSpeed * Time.deltaTime);
    }
}
