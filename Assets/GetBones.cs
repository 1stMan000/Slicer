using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetBones : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        BoneWeight[] bones = GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.boneWeights;
        Debug.Log(bones[0].boneIndex0);
        Debug.Log(bones[1].boneIndex1);
    }
}
