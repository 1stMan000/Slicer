using UnityEngine;
using System.Collections;

// this example creates a quad mesh from scratch, creates bones
// and assigns them, and animates the bones motion to make the
// quad animate based on a simple animation curve.
public class BindPoseExample : MonoBehaviour
{
    void Start()
    {
        gameObject.AddComponent<Animation>();
        gameObject.AddComponent<SkinnedMeshRenderer>();
        SkinnedMeshRenderer rend = GetComponent<SkinnedMeshRenderer>();
        Animation anim = GetComponent<Animation>();

        // Build basic mesh
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { new Vector3(-1, 0, 0), new Vector3(1, 0, 0), new Vector3(-1, 5, 0), new Vector3(1, 5, 0), new Vector3(-1, 0, 1), new Vector3(1, 0, 1), new Vector3(-1, 5, 1), new Vector3(1, 5, 1) };
        mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        mesh.triangles = new int[] { 2, 1, 0, 2, 3, 1, 3, 5, 1, 3, 7, 5, 7, 6, 4, 7, 4, 5, 6, 2, 0, 6, 0, 4, 6, 7, 3, 6, 3, 2 };
        mesh.RecalculateNormals();
        rend.material = new Material(Shader.Find("UI/Default"));

        // assign bone weights to mesh
        BoneWeight[] weights = new BoneWeight[8];
        weights[0].boneIndex0 = 1;
        weights[0].weight0 = 1;
        weights[1].boneIndex0 = 1;
        weights[1].weight0 = 1;
        weights[2].boneIndex0 = 0;
        weights[2].weight0 = 1;
        weights[3].boneIndex0 = 0;
        weights[3].weight0 = 1;
        weights[4].boneIndex0 = 1;
        weights[4].weight0 = 1;
        weights[5].boneIndex0 = 1;
        weights[5].weight0 = 1;
        weights[6].boneIndex0 = 0;
        weights[6].weight0 = 1;
        weights[7].boneIndex0 = 0;
        weights[7].weight0 = 1;
        mesh.boneWeights = weights;
        var boneWeight = mesh.GetAllBoneWeights();
        // Create Bone Transforms and Bind poses
        // One bone at the bottom and one at the top

        Transform[] bones = new Transform[2];
        Matrix4x4[] bindPoses = new Matrix4x4[2];
        bones[0] = new GameObject("Lower").transform;
        bones[0].parent = transform;
        // Set the position relative to the parent
        bones[0].localRotation = Quaternion.identity;
        bones[0].localPosition = Vector3.zero;
        // The bind pose is bone's inverse transformation matrix
        // In this case the matrix we also make this matrix relative to the root
        // So that we can move the root game object around freely
        bindPoses[0] = bones[0].worldToLocalMatrix * transform.localToWorldMatrix;

        bones[1] = new GameObject("Upper").transform;
        bones[1].parent = transform;
        // Set the position relative to the parent
        bones[1].localRotation = Quaternion.identity;
        bones[1].localPosition = new Vector3(0, 5, 0);
        // The bind pose is bone's inverse transformation matrix
        // In this case the matrix we also make this matrix relative to the root
        // So that we can move the root game object around freely
        bindPoses[1] = bones[1].worldToLocalMatrix * transform.localToWorldMatrix;

        // bindPoses was created earlier and was updated with the required matrix.
        // The bindPoses array will now be assigned to the bindposes in the Mesh.
        mesh.bindposes = bindPoses;

        // Assign bones and bind poses
        rend.bones = bones;
        rend.sharedMesh = mesh;
        GameObject gameObject2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gameObject2.transform.parent = transform;
        gameObject2.transform.localPosition = new Vector3(bindPoses[1][0, 3], bindPoses[1][1, 3], bindPoses[1][2, 3]);
        gameObject2.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // Assign a simple waving animation to the bottom bone
        AnimationCurve curve = new AnimationCurve();
        curve.keys = new Keyframe[] { new Keyframe(0, 0, 0, 0), new Keyframe(1, 3, 0, 0), new Keyframe(2, 0.0F, 0, 0) };

        // Create the clip with the curve
        AnimationClip clip = new AnimationClip();
        clip.SetCurve("Lower", typeof(Transform), "m_LocalPosition.z", curve);
        clip.legacy = true;

        // Add and play the clip
        clip.wrapMode = WrapMode.Loop;
        anim.AddClip(clip, "test");
        anim.Play("test");
    }
}
