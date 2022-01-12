using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class worldPoint : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Matrix4x4 text = transform.localToWorldMatrix * transform.worldToLocalMatrix;
        Matrix4x4 text2 = transform.localToWorldMatrix * text;
        var position = new Vector3(transform.localToWorldMatrix[0, 3], transform.localToWorldMatrix[1, 3], transform.localToWorldMatrix[2, 3]);
        var position2 = new Vector3(text2[0, 3], text2[1, 3], text2[2, 3]);
    }
}
