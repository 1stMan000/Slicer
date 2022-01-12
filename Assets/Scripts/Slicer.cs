using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace Assets.Scripts
{
    class Slicer
    {
        /// <summary>
        /// Slice the object by the plane 
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="objectToCut"></param>
        /// <returns></returns>
        public static GameObject[] Slice(Plane plane, GameObject objectToCut)
        {            
            //Get the current mesh and its verts and tris
            Mesh mesh = objectToCut.GetComponent<MeshFilter>().mesh;
            var a = mesh.GetSubMesh(0);
            Sliceable sliceable = objectToCut.GetComponent<Sliceable>();

            GameObject root = objectToCut.transform.parent.GetChild(1).gameObject;
            if (sliceable == null)
            {
                throw new NotSupportedException("Cannot slice non sliceable object, add the sliceable script to the object or inherit from sliceable to support slicing");
            }
            
            //Create left and right slice of hollow object
            SlicesMetadata slicesMeta = new SlicesMetadata(plane, objectToCut.transform.parent.GetChild(1).gameObject, objectToCut.GetComponent<SkinnedMeshRenderer>(), mesh, sliceable.IsSolid, sliceable.ReverseWireTriangles, sliceable.ShareVertices, sliceable.SmoothVertices);            

            GameObject positiveObject = CreateMeshGameObject(objectToCut);
            positiveObject.name = string.Format("{0}_positive", objectToCut.name);

            GameObject negativeObject = CreateMeshGameObject(objectToCut);
            negativeObject.name = string.Format("{0}_negative", objectToCut.name);

            var positiveSideMeshData = slicesMeta.PositiveSideMesh;
            var negativeSideMeshData = slicesMeta.NegativeSideMesh;
            positiveObject.GetComponent<MeshFilter>().mesh = positiveSideMeshData;
            negativeObject.GetComponent<MeshFilter>().mesh = negativeSideMeshData;
            positiveObject.GetComponent<SkinnedMeshRenderer>().sharedMesh = positiveSideMeshData;

            NativeArray<BoneWeight1> boneWeight1s = new NativeArray<BoneWeight1>(slicesMeta.vert_boneInfos.Count, Allocator.TempJob);
            for (int b = 0; b < slicesMeta.vert_boneInfos.Count; b++)
            {
                BoneWeight1 weight1 = boneWeight1s[b];
                weight1.boneIndex = slicesMeta.vert_boneInfos[b];
                weight1.weight = 1;
                boneWeight1s[b] = weight1;
                Debug.Log(weight1.boneIndex);
            }
            NativeArray<byte> vs = new NativeArray<byte>(slicesMeta.vert_boneInfos.Count, Allocator.TempJob);
            for (int b = 0; b < slicesMeta.vert_boneInfos.Count; b++)
            {
                vs[b] = 1;
            }
            positiveObject.GetComponent<SkinnedMeshRenderer>().sharedMesh.SetBoneWeights(vs, boneWeight1s);

            GameObject boneObjects = GameObject.Instantiate(root);
            boneObjects.transform.parent = positiveObject.transform;
            boneObjects.transform.localPosition = new Vector3(0, 0, 0);
            positiveObject.GetComponent<SkinnedMeshRenderer>().bones = boneObjects.GetComponentsInChildren<Transform>();
            
            Matrix4x4[] bindPoses = new Matrix4x4[62];
            for (int b = 0; b < 62; b++)
            {
                bindPoses[b] = positiveObject.GetComponent<SkinnedMeshRenderer>().bones[b].worldToLocalMatrix * positiveObject.GetComponent<SkinnedMeshRenderer>().bones[b].parent.transform.localToWorldMatrix;
            }
            positiveObject.GetComponent<SkinnedMeshRenderer>().sharedMesh.bindposes = bindPoses;
            Debug.Log(positiveObject.GetComponent<SkinnedMeshRenderer>().sharedMesh.bindposes.Length);

            negativeObject.GetComponent<SkinnedMeshRenderer>().sharedMesh = negativeSideMeshData;

            SetupCollidersAndRigidBodys(ref positiveObject, positiveSideMeshData, sliceable.UseGravity);
            SetupCollidersAndRigidBodys(ref negativeObject, negativeSideMeshData, sliceable.UseGravity);

            return new GameObject[] { positiveObject, negativeObject};
        }        

        /// <summary>
        /// Creates the default mesh game object.
        /// </summary>
        /// <param name="originalObject">The original object.</param>
        /// <returns></returns>
        private static GameObject CreateMeshGameObject(GameObject originalObject)
        {
            var originalMaterial = originalObject.GetComponent<SkinnedMeshRenderer>().materials;

            GameObject meshGameObject = new GameObject();
            Sliceable originalSliceable = originalObject.GetComponent<Sliceable>();

            meshGameObject.AddComponent<MeshFilter>();
            meshGameObject.AddComponent<SkinnedMeshRenderer>();
            Sliceable sliceable = meshGameObject.AddComponent<Sliceable>();

            sliceable.IsSolid = originalSliceable.IsSolid;
            sliceable.ReverseWireTriangles = originalSliceable.ReverseWireTriangles;
            sliceable.UseGravity = originalSliceable.UseGravity;

            meshGameObject.GetComponent<SkinnedMeshRenderer>().materials = originalMaterial;

            meshGameObject.transform.localScale = originalObject.transform.localScale;
            meshGameObject.transform.rotation = originalObject.transform.rotation;
            meshGameObject.transform.position = originalObject.transform.position;

            meshGameObject.tag = originalObject.tag;

            return meshGameObject;
        }

        /// <summary>
        /// Add mesh collider and rigid body to game object
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="mesh"></param>
        private static void SetupCollidersAndRigidBodys(ref GameObject gameObject, Mesh mesh, bool useGravity)
        {                     
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true;

            var rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = useGravity;
        }
    }
}

public class TestSkinnedMesh : MonoBehaviour
{
    void Start()
    {
        // Get a reference to the mesh
        var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        var mesh = skinnedMeshRenderer.sharedMesh;

        // Get the number of bone weights per vertex
        var bonesPerVertex = mesh.GetBonesPerVertex();
        if (bonesPerVertex.Length == 0)
        {
            return;
        }

        // Get all the bone weights, in vertex index order
        var boneWeights = mesh.GetAllBoneWeights();

        // Keep track of where we are in the array of BoneWeights, as we iterate over the vertices
        var boneWeightIndex = 0;

        // Iterate over the vertices
        for (var vertIndex = 0; vertIndex < mesh.vertexCount; vertIndex++)
        {
            var totalWeight = 0f;
            var numberOfBonesForThisVertex = bonesPerVertex[vertIndex];
            Debug.Log("This vertex has " + numberOfBonesForThisVertex + " bone influences");

            // For each vertex, iterate over its BoneWeights
            for (var i = 0; i < numberOfBonesForThisVertex; i++)
            {
                var currentBoneWeight = boneWeights[boneWeightIndex];
                totalWeight += currentBoneWeight.weight;
                if (i > 0)
                {
                    Debug.Assert(boneWeights[boneWeightIndex - 1].weight >= currentBoneWeight.weight);
                }
                boneWeightIndex++;
            }
            Debug.Assert(Mathf.Approximately(1f, totalWeight));
        }
    }
}
