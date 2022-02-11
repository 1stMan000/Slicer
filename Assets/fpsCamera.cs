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
                    modifiedVerts.Add(hit.transform.localToWorldMatrix.MultiplyPoint3x4(vert));
                }
                
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = hit.point;
                
                List<int> triangles= new List<int>();
                foreach (int triangle in mesh.triangles)
                {
                    triangles.Add(triangle);
                }

                float distance = 100;
                Vector3 closest = new Vector3();
                foreach (Vector3 vector in modifiedVerts)
                {
                    float current = Vector3.Distance(hit.point, vector);
                    if (current < distance)
                    {
                        distance = current;
                        closest = vector;
                    }
                }

                modifiedVerts[modifiedVerts.IndexOf(closest)] = modifiedVerts[modifiedVerts.IndexOf(closest)] + new Vector3(0, -10, 0);
                List<Vector3> localVerts = new List<Vector3>();
                foreach (Vector3 vert in modifiedVerts)
                {
                    localVerts.Add(hit.transform.worldToLocalMatrix.MultiplyPoint3x4(vert));
                }
                mesh.vertices = localVerts.ToArray();
                mesh.triangles = triangles.ToArray();

                hit.transform.GetComponent<MeshCollider>().sharedMesh = hit.transform.GetComponent<MeshFilter>().mesh;
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

class GFG
{

    /* A utility function to calculate area of triangle
    formed by (x1, y1) (x2, y2) and (x3, y3) */
    static float area(float x1, float y1, float x2,
                       float y2, float x3, float y3)
    {
        return Mathf.Abs((x1 * (y2 - y3) +
                         x2 * (y3 - y1) +
                         x3 * (y1 - y2)) / 2.0f);
    }

    /* A function to check whether point P(x, y) lies
    inside the triangle formed by A(x1, y1),
    B(x2, y2) and C(x3, y3) */
    public bool isInside(float x1, float y1, float x2,
                         float y2, float x3, float y3,
                         float x, float y)
    {
        /* Calculate area of triangle ABC */
        float A = area(x1, y1, x2, y2, x3, y3);

        /* Calculate area of triangle PBC */
        float A1 = area(x, y, x2, y2, x3, y3);

        /* Calculate area of triangle PAC */
        float A2 = area(x1, y1, x, y, x3, y3);

        /* Calculate area of triangle PAB */
        float A3 = area(x1, y1, x2, y2, x, y);

        /* Check if sum of A1, A2 and A3 is same as A */
        return (A == A1 + A2 + A3);
    }
}
