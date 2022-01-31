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

            if (sliceable == null)
            {
                throw new NotSupportedException("Cannot slice non sliceable object, add the sliceable script to the object or inherit from sliceable to support slicing");
            }

            GameObject root = null;

            MeshRenderer meshRenderer = objectToCut.GetComponent<MeshRenderer>();
            SlicesMetadata slicesMeta;
            bool isMeshRend;
            //Create left and right slice of hollow object
            if (meshRenderer != null)
            {
                isMeshRend = true;
                slicesMeta = new SlicesMetadata(plane, objectToCut, mesh, sliceable.IsSolid, sliceable.ReverseWireTriangles, sliceable.ShareVertices, sliceable.SmoothVertices);
            }
            else
            {
                isMeshRend = false;
                slicesMeta = new SlicesMetadata(plane, objectToCut.transform.parent.GetChild(1).gameObject, objectToCut.GetComponent<SkinnedMeshRenderer>(), objectToCut.GetComponent<SkinnedMeshRenderer>().sharedMesh, sliceable.IsSolid, sliceable.ReverseWireTriangles, sliceable.ShareVertices, sliceable.SmoothVertices);

                root = objectToCut.transform.root.GetChild(1).gameObject;
            }

            GameObject positiveObject = CreateMeshGameObject(objectToCut, isMeshRend);
            positiveObject.name = string.Format("{0}_positive", objectToCut.name);

            GameObject negativeObject = CreateMeshGameObject(objectToCut, isMeshRend);
            negativeObject.name = string.Format("{0}_negative", objectToCut.name);

            var positiveSideMeshData = slicesMeta.PositiveSideMesh;
            var negativeSideMeshData = slicesMeta.NegativeSideMesh;
            positiveObject.GetComponent<MeshFilter>().mesh = positiveSideMeshData;
            negativeObject.GetComponent<MeshFilter>().mesh = negativeSideMeshData;
            if (!isMeshRend)
            {
                SkinnedMeshRenderer skinnedMesh = objectToCut.GetComponent<SkinnedMeshRenderer>();
                positiveObject.GetComponent<SkinnedMeshRenderer>().sharedMesh = positiveSideMeshData;

                NativeArray<byte> vs = new NativeArray<byte>(slicesMeta.vertInOrigin.Count, Allocator.Persistent);

                // Get the number of bone weights per vertex
                var bonesPerVertex = skinnedMesh.sharedMesh.GetBonesPerVertex();

                // Get all the bone weights, in vertex index order
                var boneWeights = skinnedMesh.sharedMesh.GetAllBoneWeights();

                // Keep track of where we are in the array of BoneWeights, as we iterate over the vertices
                var boneWeightIndex = 0;

                List<int> selectedOrders = new List<int>();
                List<BoneWeight1> boneWeight1sforNative = new List<BoneWeight1>();
                // Iterate over the vertices
                for (var vertIndex = 0; vertIndex < skinnedMesh.sharedMesh.vertexCount; vertIndex++)
                {
                    bool debug = false;
                    int order = 0;
                    for (int z = 0; z < slicesMeta.vertInOrigin.Count; z++)
                    {
                        if (vertIndex == slicesMeta.vertInOrigin[z])
                        {
                            debug = true;
                            
                            order = z;
                            break;
                        }
                    }

                    var totalWeight = 0f;
                    var numberOfBonesForThisVertex = bonesPerVertex[vertIndex];
                    if (debug == true)
                    {
                        vs[order] = numberOfBonesForThisVertex;
                    }
                        
                    // For each vertex, iterate over its BoneWeights
                    for (var i = 0; i < numberOfBonesForThisVertex; i++)
                    {
                        var currentBoneWeight = boneWeights[boneWeightIndex];
                        totalWeight += currentBoneWeight.weight;
                        if (i > 0)
                        {
                            Debug.Assert(boneWeights[boneWeightIndex - 1].weight >= currentBoneWeight.weight);
                        }
                        if (debug == true)
                        {
                            int biggest = 0;
                            for (int count = 0; count < selectedOrders.Count; count++)
                            {
                                if (selectedOrders[count] < order )
                                    biggest++;
                            }

                            if (biggest == selectedOrders.Count)
                            {
                                boneWeight1sforNative.Add(currentBoneWeight);
                            }
                            else
                            {
                                boneWeight1sforNative.Insert(biggest, currentBoneWeight);
                            }

                            selectedOrders.Add(order);
                        }  
                        boneWeightIndex++;
                    }
                    Debug.Assert(Mathf.Approximately(1f, totalWeight));
                }
                
                for (int boneCount = 0; boneCount < vs.Length; boneCount++)
                {
                    bool isNull = true;
                    for (int selectComp = 0; selectComp < selectedOrders.Count; selectComp++)
                    {
                        if (boneCount == selectedOrders[selectComp])
                        {
                            isNull = false;
                        }
                    }

                    if (isNull == true)
                    {
                        vs[boneCount] = 1;
                        int biggest = 0;
                        for (int count = 0; count < selectedOrders.Count; count++)
                        {
                            if (selectedOrders[count] < boneCount)
                            {
                                biggest++;
                            }
                        }

                        BoneWeight1 boneWeight1 = new BoneWeight1();
                        boneWeight1.boneIndex = 28;
                        boneWeight1.weight = 1;
                        if (biggest == selectedOrders.Count)
                        {
                            boneWeight1sforNative.Add(boneWeight1);
                        }
                        else
                        {
                            boneWeight1sforNative.Insert(biggest, boneWeight1);
                        }

                        selectedOrders.Add(boneCount);
                    }
                }

                NativeArray<BoneWeight1> boneWeight1s = new NativeArray<BoneWeight1>(boneWeight1sforNative.Count, Allocator.Persistent);
                for (int count = 0; count < boneWeight1sforNative.Count; count++)
                {
                    boneWeight1s[count] = boneWeight1sforNative[count];
                }
                positiveObject.GetComponent<SkinnedMeshRenderer>().sharedMesh.SetBoneWeights(vs, boneWeight1s);
                
                GameObject boneObjects = GameObject.Instantiate(root);
                boneObjects.transform.parent = positiveObject.transform;
                boneObjects.transform.localPosition = new Vector3(0, 0, 0);

                Transform[] slicedBones = new Transform[objectToCut.GetComponent<SkinnedMeshRenderer>().bones.Length];
                for (int boneNum = 0; boneNum < objectToCut.GetComponent<SkinnedMeshRenderer>().bones.Length; boneNum++)
                {
                    for (int bone2Num = 0; bone2Num < boneObjects.GetComponentsInChildren<Transform>().Length; bone2Num++)
                    {
                        if (objectToCut.GetComponent<SkinnedMeshRenderer>().bones[boneNum].name == boneObjects.GetComponentsInChildren<Transform>()[bone2Num].name)
                        {
                            slicedBones[boneNum] = boneObjects.GetComponentsInChildren<Transform>()[bone2Num];
                            slicedBones[boneNum].name = boneObjects.GetComponentsInChildren<Transform>()[bone2Num].name;
                            if (boneObjects.GetComponentsInChildren<Transform>()[bone2Num].parent != null)
                            {
                                slicedBones[boneNum].parent = boneObjects.GetComponentsInChildren<Transform>()[bone2Num].parent;
                                slicedBones[boneNum].parent.name = boneObjects.GetComponentsInChildren<Transform>()[bone2Num].parent.name;
                            }
                            break;
                        }
                    }
                }

                for (int boneNum = 0; boneNum < slicedBones.Length; boneNum++)
                {
                    if (slicedBones[boneNum].parent != null)
                    {
                        for (int bone2Num = 0; bone2Num < objectToCut.GetComponent<SkinnedMeshRenderer>().bones.Length; bone2Num++)
                        {
                            if (slicedBones[boneNum].parent.name == slicedBones[bone2Num].name)
                            {
                                slicedBones[boneNum].parent = slicedBones[bone2Num];
                            }
                        }
                    }
                }
                positiveObject.GetComponent<SkinnedMeshRenderer>().bones = slicedBones;
                positiveObject.GetComponent<SkinnedMeshRenderer>().rootBone = slicedBones[0].GetChild(0).GetChild(0);

                Matrix4x4[] bindPoses = new Matrix4x4[52];
                for (int b = 0; b < 52; b++)
                {
                    bindPoses[b] = positiveObject.GetComponent<SkinnedMeshRenderer>().bones[b].worldToLocalMatrix * positiveObject.GetComponent<SkinnedMeshRenderer>().bones[b].root.transform.localToWorldMatrix;
                }
                positiveObject.GetComponent<SkinnedMeshRenderer>().sharedMesh.bindposes = bindPoses;
                negativeObject.GetComponent<SkinnedMeshRenderer>().sharedMesh = negativeSideMeshData;
            }
            
            SetupCollidersAndRigidBodys(ref positiveObject, positiveSideMeshData, sliceable.UseGravity);
            SetupCollidersAndRigidBodys(ref negativeObject, negativeSideMeshData, sliceable.UseGravity);

            return new GameObject[] { positiveObject, negativeObject};
        }        

        /// <summary>
        /// Creates the default mesh game object.
        /// </summary>
        /// <param name="originalObject">The original object.</param>
        /// <returns></returns>
        private static GameObject CreateMeshGameObject(GameObject originalObject, bool isMeshRend = true)
        {
            var originalMaterial = new Material[4];
            if (isMeshRend)
            {
                originalMaterial = originalObject.GetComponent<MeshRenderer>().materials;
            }
            else
            {
               originalMaterial = originalObject.GetComponent<SkinnedMeshRenderer>().materials;
            }
            GameObject meshGameObject = new GameObject();
            Sliceable originalSliceable = originalObject.GetComponent<Sliceable>();

            meshGameObject.AddComponent<MeshFilter>();
            if (isMeshRend)
            {
                meshGameObject.AddComponent<MeshRenderer>();
            }
            else
            {
                meshGameObject.AddComponent<SkinnedMeshRenderer>();
            }
            Sliceable sliceable = meshGameObject.AddComponent<Sliceable>();

            sliceable.IsSolid = originalSliceable.IsSolid;
            sliceable.ReverseWireTriangles = originalSliceable.ReverseWireTriangles;
            sliceable.UseGravity = originalSliceable.UseGravity;

            if (isMeshRend)
            {
                meshGameObject.GetComponent<MeshRenderer>().materials = originalMaterial;
            }
            else
            {
                meshGameObject.GetComponent<SkinnedMeshRenderer>().materials = originalMaterial;
            }

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
