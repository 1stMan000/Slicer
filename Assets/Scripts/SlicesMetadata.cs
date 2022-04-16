using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine.UIElements;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;


namespace Assets.Scripts
{
    /// <summary>
    /// The side of the mesh
    /// </summary>
    public enum MeshSide
    {
        Positive = 0,
        Negative = 1
    }

    /// <summary>
    /// An object used to manage the positive and negative side mesh data for a sliced object
    /// </summary>
    class SlicesMetadata
    {
        private Mesh _positiveSideMesh;
        private List<Vector3> _positiveSideVertices;
        private List<int> _positiveSideTriangles;
        private List<Vector2> _positiveSideUvs;
        private List<Vector3> _positiveSideNormals;
        public List<int> orderOfVertInOriginalMesh = new List<int>();

        private Mesh _negativeSideMesh;
        private List<Vector3> _negativeSideVertices;
        private List<int> _negativeSideTriangles;
        private List<Vector2> _negativeSideUvs;
        private List<Vector3> _negativeSideNormals;

        private readonly List<Vector3> _pointsAlongPlane;
        private Plane _plane;
        public Transform[] bones;
        private SkinnedMeshRenderer skinnedMesh;
        private Mesh _mesh;
        private bool _isSolid;
        private bool _useSharedVertices = false;
        private bool _smoothVertices = false;
        private bool _createReverseTriangleWindings = false;

        GameObject sliceObject;

        public bool IsSolid
        {
            get
            {
                return _isSolid;
            }
            set
            {
                _isSolid = value;
            }
        }

        public Mesh PositiveSideMesh
        {
            get
            {
                if (_positiveSideMesh == null)
                {
                    _positiveSideMesh = new Mesh();
                }

                SetMeshData(MeshSide.Positive);
                return _positiveSideMesh;
            }
        }

        public Mesh NegativeSideMesh
        {
            get
            {
                if (_negativeSideMesh == null)
                {
                    _negativeSideMesh = new Mesh();
                }

                SetMeshData(MeshSide.Negative);

                return _negativeSideMesh;
            }
        }

        public SlicesMetadata(Plane plane, GameObject gameObject, Mesh mesh, bool isSolid, bool createReverseTriangleWindings, bool shareVertices, bool smoothVertices)
        {
            _positiveSideTriangles = new List<int>();
            _positiveSideVertices = new List<Vector3>();
            _negativeSideTriangles = new List<int>();
            _negativeSideVertices = new List<Vector3>();
            _positiveSideUvs = new List<Vector2>();
            _negativeSideUvs = new List<Vector2>();
            _positiveSideNormals = new List<Vector3>();
            _negativeSideNormals = new List<Vector3>();
            _pointsAlongPlane = new List<Vector3>();
            _plane = plane;
            sliceObject = gameObject;
            bones = gameObject.transform.GetComponentsInChildren<Transform>();
            _mesh = mesh;
            _isSolid = isSolid;
            _createReverseTriangleWindings = createReverseTriangleWindings;
            _useSharedVertices = shareVertices;
            _smoothVertices = smoothVertices;

            ComputeNewMeshes();
        }

        public SlicesMetadata(Plane plane, GameObject gameObject, SkinnedMeshRenderer skinnedMeshRenderer, Mesh mesh, bool isSolid, bool createReverseTriangleWindings, bool shareVertices, bool smoothVertices)
        {
            _positiveSideTriangles = new List<int>();
            _positiveSideVertices = new List<Vector3>();
            _negativeSideTriangles = new List<int>();
            _negativeSideVertices = new List<Vector3>();
            _positiveSideUvs = new List<Vector2>();
            _negativeSideUvs = new List<Vector2>();
            _positiveSideNormals = new List<Vector3>();
            _negativeSideNormals = new List<Vector3>();
            _pointsAlongPlane = new List<Vector3>();
            _plane = plane;
            sliceObject = gameObject;
            bones = gameObject.transform.GetComponentsInChildren<Transform>();
            skinnedMesh = skinnedMeshRenderer;
            _mesh = mesh;
            _isSolid = isSolid;
            _createReverseTriangleWindings = createReverseTriangleWindings;
            _useSharedVertices = shareVertices;
            _smoothVertices = smoothVertices;

            ComputeNewMeshes();
        }

        /// <summary>
        /// Add the mesh data to the correct side and calulate normals
        /// </summary>
        /// <param name="side"></param>
        /// <param name="vertex1"></param>
        /// <param name="vertex1Uv"></param>
        /// <param name="vertex2"></param>
        /// <param name="vertex2Uv"></param>
        /// <param name="vertex3"></param>
        /// <param name="vertex3Uv"></param>
        /// <param name="shareVertices"></param>
        private void AddTrianglesNormalAndUvs(MeshSide side, Vector3 vertex1, int num1, Vector3? normal1, Vector2 uv1, Vector3 vertex2, int num2, Vector3? normal2, Vector2 uv2, Vector3 vertex3, int num3, Vector3? normal3, Vector2 uv3, bool shareVertices, bool addFirst)
        {
            if (side == MeshSide.Positive)
            {
                AddTrianglesNormalsAndUvs(ref _positiveSideVertices, ref _positiveSideTriangles, ref _positiveSideNormals, ref _positiveSideUvs, vertex1, num1, normal1, uv1, vertex2, num2, normal2, uv2, vertex3, num3, normal3, uv3, shareVertices, addFirst);
            }
            else
            {
                AddTrianglesNormalsAndUvs(ref _negativeSideVertices, ref _negativeSideTriangles, ref _negativeSideNormals, ref _negativeSideUvs, vertex1, num1, normal1, uv1, vertex2, num2, normal2, uv2, vertex3, num3, normal3, uv3, shareVertices, addFirst);
            }
        }


        /// <summary>
        /// Adds the vertices to the mesh sets the triangles in the order that the vertices are provided.
        /// If shared vertices is false vertices will be added to the list even if a matching vertex already exists
        /// Does not compute normals
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="triangles"></param>
        /// <param name="uvs"></param>
        /// <param name="normals"></param>
        /// <param name="vertex1"></param>
        /// <param name="vertex1Uv"></param>
        /// <param name="normal1"></param>
        /// <param name="vertex2"></param>
        /// <param name="vertex2Uv"></param>
        /// <param name="normal2"></param>
        /// <param name="vertex3"></param>
        /// <param name="vertex3Uv"></param>
        /// <param name="normal3"></param>
        /// <param name="shareVertices"></param>
        /// 

        public int count;
        private void AddTrianglesNormalsAndUvs(ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, ref List<Vector2> uvs, Vector3 vertex1, int num1, Vector3? normal1, Vector2 uv1, Vector3 vertex2, int num2, Vector3? normal2, Vector2 uv2, Vector3 vertex3, int num3, Vector3? normal3, Vector2 uv3, bool shareVertices, bool addFirst)
        {
            int tri1Index = vertices.IndexOf(vertex1);

            if (addFirst)
            {
                ShiftTriangleIndeces(ref triangles);
            }

            //If a the vertex already exists we just add a triangle reference to it, if not add the vert to the list and then add the tri index
            if (tri1Index > -1 && shareVertices)
            {                
                triangles.Add(tri1Index);
            }
            else
            {
                if (normal1 == null)
                {
                    normal1 = ComputeNormal(vertex1, vertex2, vertex3);                    
                }

                int? i = null;
                if (addFirst)
                {
                    i = 0;
                }

                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex1, num1, (Vector3)normal1, uv1, i);
            }

            int tri2Index = vertices.IndexOf(vertex2);

            if (tri2Index > -1 && shareVertices)
            {
                triangles.Add(tri2Index);
            }
            else
            {
                if (normal2 == null)
                {
                    normal2 = ComputeNormal(vertex2, vertex3, vertex1);
                }

                int? i = null;
                if (addFirst)
                {
                    i = 1;
                }

                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex2, num2, (Vector3)normal2, uv2, i);
            }

            int tri3Index = vertices.IndexOf(vertex3);

            if (tri3Index > -1 && shareVertices)
            {
                triangles.Add(tri3Index);
            }
            else
            {               
                if (normal3 == null)
                {
                    normal3 = ComputeNormal(vertex3, vertex1, vertex2);
                }

                int? i = null;
                if (addFirst)
                {
                    i = 2;
                }

                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex3, num3, (Vector3)normal3, uv3, i);
            }
        }
        
        private void AddVertNormalUv(ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles, Vector3 vertex, int num, Vector3 normal, Vector2 uv, int? index)
        {
            if (index != null)
            {
                int i = (int)index;
                vertices.Insert(i, vertex);
                uvs.Insert(i, uv);
                normals.Insert(i, normal);
                triangles.Insert(i, i);

                if (vertices == _positiveSideVertices)
                {
                    orderOfVertInOriginalMesh.Insert(i, num);
                }
            }
            else
            {
                vertices.Add(vertex);
                normals.Add(normal);
                uvs.Add(uv);
                triangles.Add(vertices.IndexOf(vertex));

                if (vertices == _positiveSideVertices)
                {
                    orderOfVertInOriginalMesh.Add(num);
                }
            }
        }

        private void ShiftTriangleIndeces(ref List<int> triangles)
        {
            for (int j = 0; j < triangles.Count; j += 3)
            {
                triangles[j] += + 3;
                triangles[j + 1] += 3;
                triangles[j + 2] += 3;
            }
        }

        /// <summary>
        /// Will render the inside of an object
        /// This is heavy as it duplicates all the vertices and creates opposite winding direction
        /// </summary>
        private void AddReverseTriangleWinding()
        {
            int positiveVertsStartIndex = _positiveSideVertices.Count;
            //Duplicate the original vertices
            _positiveSideVertices.AddRange(_positiveSideVertices);
            _positiveSideUvs.AddRange(_positiveSideUvs);
            _positiveSideNormals.AddRange(FlipNormals(_positiveSideNormals));

            int numPositiveTriangles = _positiveSideTriangles.Count;

            //Add reverse windings
            for (int i = 0; i < numPositiveTriangles; i += 3)
            {
                _positiveSideTriangles.Add(positiveVertsStartIndex + _positiveSideTriangles[i]);
                _positiveSideTriangles.Add(positiveVertsStartIndex + _positiveSideTriangles[i + 2]);
                _positiveSideTriangles.Add(positiveVertsStartIndex + _positiveSideTriangles[i + 1]);
            }

            int negativeVertextStartIndex = _negativeSideVertices.Count;
            //Duplicate the original vertices
            _negativeSideVertices.AddRange(_negativeSideVertices);
            _negativeSideUvs.AddRange(_negativeSideUvs);
            _negativeSideNormals.AddRange(FlipNormals(_negativeSideNormals));

            int numNegativeTriangles = _negativeSideTriangles.Count;

            //Add reverse windings
            for (int i = 0; i < numNegativeTriangles; i += 3)
            {
                _negativeSideTriangles.Add(negativeVertextStartIndex + _negativeSideTriangles[i]);
                _negativeSideTriangles.Add(negativeVertextStartIndex + _negativeSideTriangles[i + 2]);
                _negativeSideTriangles.Add(negativeVertextStartIndex + _negativeSideTriangles[i + 1]);
            }
        }

        private void JoinPointsAlongPlane(List<Vector3> connectedPointsAlongPlane)
        {
            Vector3 halfway = GetHalfwayPoint(connectedPointsAlongPlane, out float distance);

            for (int i = 0; i < connectedPointsAlongPlane.Count; i += 2)
            {
                Vector3 firstVertex;
                Vector3 secondVertex;

                firstVertex = connectedPointsAlongPlane[i];
                secondVertex = connectedPointsAlongPlane[i + 1];

                Vector3 normal3 = ComputeNormal(halfway, secondVertex, firstVertex);
                normal3.Normalize();

                var direction = Vector3.Dot(normal3, _plane.normal);
                int boneNum = -1;

                if (direction > 0)
                {
                    AddTrianglesNormalAndUvs(MeshSide.Positive, halfway, boneNum, -normal3, Vector2.zero, firstVertex, boneNum, -normal3, Vector2.zero, secondVertex, boneNum, -normal3, Vector2.zero, false, true);
                    AddTrianglesNormalAndUvs(MeshSide.Negative, halfway, boneNum, normal3, Vector2.zero, secondVertex, boneNum, normal3, Vector2.zero, firstVertex, boneNum, normal3, Vector2.zero, false, true);
                }
                else
                {
                    AddTrianglesNormalAndUvs(MeshSide.Positive, halfway, boneNum, normal3, Vector2.zero, secondVertex, boneNum, normal3, Vector2.zero, firstVertex, boneNum, normal3, Vector2.zero, false, true);
                    AddTrianglesNormalAndUvs(MeshSide.Negative, halfway, boneNum, -normal3, Vector2.zero, firstVertex, boneNum, -normal3, Vector2.zero, secondVertex, boneNum, -normal3, Vector2.zero, false, true);
                }
            }
        }

        /// <summary>
        /// Join the points along the plane to the halfway point
        /// </summary>
        private void JoinPointsAlongPlane()
        {
            Vector3 halfway = GetHalfwayPoint(out float distance);

            for (int i = 0; i < _pointsAlongPlane.Count; i += 2)
            {
                Vector3 firstVertex;
                Vector3 secondVertex;

                firstVertex = _pointsAlongPlane[i];
                secondVertex = _pointsAlongPlane[i + 1];

                Vector3 normal3 = ComputeNormal(halfway, secondVertex, firstVertex);
                normal3.Normalize();

                var direction = Vector3.Dot(normal3, _plane.normal);
                int boneNum = -1;

                if (direction > 0)
                {                                        
                    AddTrianglesNormalAndUvs(MeshSide.Positive, halfway, boneNum, -normal3, Vector2.zero, firstVertex, boneNum, -normal3, Vector2.zero, secondVertex, boneNum, -normal3, Vector2.zero, false, true);
                    AddTrianglesNormalAndUvs(MeshSide.Negative, halfway, boneNum, normal3, Vector2.zero, secondVertex, boneNum, normal3, Vector2.zero, firstVertex, boneNum, normal3, Vector2.zero, false, true);
                }
                else
                {
                    AddTrianglesNormalAndUvs(MeshSide.Positive, halfway, boneNum, normal3, Vector2.zero, secondVertex, boneNum, normal3, Vector2.zero, firstVertex, boneNum, normal3, Vector2.zero, false, true);
                    AddTrianglesNormalAndUvs(MeshSide.Negative, halfway, boneNum, -normal3, Vector2.zero, firstVertex, boneNum, -normal3, Vector2.zero, secondVertex, boneNum, -normal3, Vector2.zero, false, true);
                }               
            }
        }

        private Vector3 GetHalfwayPoint(List<Vector3> vectors, out float distance)
        {
            if (vectors.Count > 0)
            {
                Vector3 firstPoint = vectors[0];
                Vector3 furthestPoint = Vector3.zero;
                distance = 0f;

                foreach (Vector3 point in vectors)
                {
                    float currentDistance = 0f;
                    currentDistance = Vector3.Distance(firstPoint, point);

                    if (currentDistance > distance)
                    {
                        distance = currentDistance;
                        furthestPoint = point;
                    }
                }

                return Vector3.Lerp(firstPoint, furthestPoint, 0.5f);
            }
            else
            {
                distance = 0;
                return Vector3.zero;
            }
        }

        /// <summary>
        /// For all the points added along the plane cut, get the half way between the first and furthest point
        /// </summary>
        /// <returns></returns>
        private Vector3 GetHalfwayPoint(out float distance)
        {
            if(_pointsAlongPlane.Count > 0)
            {
                Vector3 firstPoint = _pointsAlongPlane[0];
                Vector3 furthestPoint = Vector3.zero;
                distance = 0f;

                foreach (Vector3 point in _pointsAlongPlane)
                {
                    float currentDistance = 0f;
                    currentDistance = Vector3.Distance(firstPoint, point);

                    if (currentDistance > distance)
                    {
                        distance = currentDistance;
                        furthestPoint = point;
                    }
                }

                return Vector3.Lerp(firstPoint, furthestPoint, 0.5f);
            }
            else
            {
                distance = 0;
                return Vector3.zero;
            }
        }

        /// <summary>
        /// Setup the mesh object for the specified side
        /// </summary>
        /// <param name="side"></param>
        private void SetMeshData(MeshSide side)
        {
            if (side == MeshSide.Positive)
            {
                _positiveSideMesh.vertices = _positiveSideVertices.ToArray();
                _positiveSideMesh.triangles = _positiveSideTriangles.ToArray();
                _positiveSideMesh.normals = _positiveSideNormals.ToArray();
                _positiveSideMesh.uv = _positiveSideUvs.ToArray();
            }
            else
            {
                _negativeSideMesh.vertices = _negativeSideVertices.ToArray();
                _negativeSideMesh.triangles = _negativeSideTriangles.ToArray();
                _negativeSideMesh.normals = _negativeSideNormals.ToArray();
                _negativeSideMesh.uv = _negativeSideUvs.ToArray();                
            }
        }

        Dictionary<Vector3[], Vector3[]> trianglesOfPlane = new Dictionary<Vector3[], Vector3[]>();

        [BurstCompile(CompileSynchronously = true)]
        struct CalculateSideJob : IJob
        {
            public Plane plane;
            public NativeArray<int> meshTriangles;
            public NativeArray<Vector3> meshVerts;
            public NativeArray<Vector3> meshNormals;
            public NativeArray<Vector2> meshUVs;
            public NativeList<Vector3> trianglesOfPlane;

            public NativeList<Vector3> positiveSideVerts;
            public NativeList<int> positiveSideTriangles;
            public NativeList<Vector3> positiveSideNormals;
            public NativeList<Vector2> positiveSideUVs;

            public NativeList<Vector3> negativeSideVerts;
            public NativeList<int> negativeSideTriangles;
            public NativeList<Vector3> negativeSideNormals;
            public NativeList<Vector2> negativeSideUVs;

            public NativeList<int> orderOfVertInOrigin;

            void AddTrianglesNormalAndUvs(MeshSide side, Vector3 vertex1, int num1, Vector3? normal1, Vector2 uv1, Vector3 vertex2, int num2, Vector3? normal2, Vector2 uv2, Vector3 vertex3, int num3, Vector3? normal3, Vector2 uv3, bool addFirst)
            {
                if (side == MeshSide.Positive)
                {
                    AddTrianglesNormalsAndUvs(ref positiveSideVerts, ref positiveSideTriangles, ref positiveSideNormals, ref positiveSideUVs, vertex1, num1, normal1, uv1, vertex2, num2, normal2, uv2, vertex3, num3, normal3, uv3, addFirst);
                }
                else
                {
                    AddTrianglesNormalsAndUvs(ref negativeSideVerts, ref negativeSideTriangles, ref negativeSideNormals, ref negativeSideUVs, vertex1, num1, normal1, uv1, vertex2, num2, normal2, uv2, vertex3, num3, normal3, uv3, addFirst);
                }
            }

            void AddTrianglesNormalsAndUvs(ref NativeList<Vector3> vertices, ref NativeList<int> triangles, ref NativeList<Vector3> normals, ref NativeList<Vector2> uvs, Vector3 vertex1, int num1, Vector3? normal1, Vector2 uv1, Vector3 vertex2, int num2, Vector3? normal2, Vector2 uv2, Vector3 vertex3, int num3, Vector3? normal3, Vector2 uv3, bool addFirst)
            {
                int tri1Index = vertices.IndexOf(vertex1);

                if (addFirst)
                {
                    JobShiftTriangleIndeces(ref triangles);
                }
                
                //If a the vertex already exists we just add a triangle reference to it, if not add the vert to the list and then add the tri index
                if (tri1Index > -1)
                {
                    triangles.Add(tri1Index);
                }
                else
                {
                    if (normal1 == null)
                    {
                        normal1 = JobComputeNormal(vertex1, vertex2, vertex3);
                    }

                    int? i = null;
                    if (addFirst)
                    {
                        i = 0;
                    }

                    JobAddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex1, num1, (Vector3)normal1, uv1, i);
                }
            }

            void JobShiftTriangleIndeces(ref NativeList<int> triangles)
            {
                for (int j = 0; j < triangles.Length; j += 3)
                {
                    triangles[j] += +3;
                    triangles[j + 1] += 3;
                    triangles[j + 2] += 3;
                }
            }

            Vector3 JobComputeNormal(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
            {
                Vector3 side1 = vertex2 - vertex1;
                Vector3 side2 = vertex3 - vertex1;

                Vector3 normal = Vector3.Cross(side1, side2);

                return normal;
            }

            void JobAddVertNormalUv(ref NativeList<Vector3> vertices, ref NativeList<Vector3> normals, ref NativeList<Vector2> uvs, ref NativeList<int> triangles, Vector3 vertex, int num, Vector3 normal, Vector2 uv, int? index)
            {
                if (index != null)
                {
                    int i = (int)index;
                    vertices.InsertRangeWithBeginEnd(i, i + 1);
                    vertices[i] = vertex;
                    normals.InsertRangeWithBeginEnd(i, i + 1);
                    normals[i] = normal;
                    triangles.InsertRangeWithBeginEnd(i, i + 1);
                    triangles[i] = i;

                    bool notPos = false;
                    for (int j = 0; j < positiveSideVerts.Length; j++)
                    {
                        if (positiveSideVerts[j] != vertices[j])
                        {
                            notPos = true;
                            break;
                        }
                    }

                    if (!notPos)
                    {
                        orderOfVertInOrigin.Add(num);
                    }
                }
                else
                {
                    vertices.Add(vertex);
                    normals.Add(normal);
                    uvs.Add(uv);
                    triangles.Add(vertices.IndexOf(vertex));

                    bool notPos = false;
                    for (int j = 0; j < positiveSideVerts.Length; j++)
                    {
                        if (positiveSideVerts[j] != vertices[j])
                        {
                            notPos = true;
                            break;
                        }
                    }

                    if (!notPos)
                    {
                        orderOfVertInOrigin.Add(num);
                    }
                }
            }

            Vector3 GetRayPlaneIntersectionPointAndUv(Vector3 vertex1, Vector2 vertex1Uv, Vector3 vertex2, Vector2 vertex2Uv, out Vector2 uv)
            {
                float distance = GetDistanceRelativeToPlane(vertex1, vertex2, out Vector3 pointOfIntersection);
                uv = InterpolateUvs(vertex1Uv, vertex2Uv, distance);
                return pointOfIntersection;
            }

            private float GetDistanceRelativeToPlane(Vector3 vertex1, Vector3 vertex2, out Vector3 pointOfintersection)
            {
                Ray ray = new Ray(vertex1, (vertex2 - vertex1));
                plane.Raycast(ray, out float distance);
                pointOfintersection = ray.GetPoint(distance);
                return distance;
            }

            private Vector2 InterpolateUvs(Vector2 uv1, Vector2 uv2, float distance)
            {
                Vector2 uv = Vector2.Lerp(uv1, uv2, distance);
                return uv;
            }

            public void Execute()
            {
                for (int i = 0; i < meshTriangles.Length/3; i++)
                {
                    Vector3 vert1 = meshVerts[meshTriangles[i * 3]];
                    int vert1Index = new int();
                    for (int j = 0; j < meshVerts.Length; j++)
                    {
                        if (vert1 == meshVerts[j])
                        {
                            vert1Index = j;
                            break;
                        }
                    }
                    Vector2 uv1 = meshUVs[vert1Index];
                    Vector3 normal1 = meshNormals[vert1Index];
                    bool vert1Side = plane.GetSide(vert1);

                    Vector3 vert2 = meshVerts[meshTriangles[i * 3 + 1]];
                    int vert2Index = new int();
                    for (int j = 0; j < meshVerts.Length; j++)
                    {
                        if (vert2 == meshVerts[j])
                        {
                            vert2Index = j;
                            break;
                        }
                    }
                    Vector2 uv2 = meshUVs[vert2Index];
                    Vector3 normal2 = meshNormals[vert2Index];
                    bool vert2Side = plane.GetSide(vert2);

                    Vector3 vert3 = meshVerts[meshTriangles[i * 3 + 2]];
                    int vert3Index = new int();
                    for (int j = 0; j < meshVerts.Length; j++)
                    {
                        if (vert3 == meshVerts[j])
                        {
                            vert3Index = j;
                            break;
                        }
                    }
                    Vector3 normal3 = meshNormals[vert3Index];
                    Vector2 uv3 = meshUVs[vert3Index];
                    bool vert3Side = plane.GetSide(vert3);

                    if (vert1Side == vert2Side && vert2Side == vert3Side)
                    {
                        MeshSide side = (vert1Side) ? MeshSide.Positive : MeshSide.Negative;
                        AddTrianglesNormalAndUvs(side, vert1, meshTriangles[i], normal1, uv1, vert2, meshTriangles[i + 1], normal2, uv2, vert3, meshTriangles[i + 2], normal3, uv3, false);
                    }
                    else
                    {
                        Vector3 intersection1;
                        Vector3 intersection2;

                        Vector2 intersection1Uv;
                        Vector2 intersection2Uv;

                        MeshSide side1 = (vert1Side) ? MeshSide.Positive : MeshSide.Negative;
                        MeshSide side2 = (vert1Side) ? MeshSide.Negative : MeshSide.Positive;

                        if (vert1Side == vert2Side)
                        {
                            intersection1 = GetRayPlaneIntersectionPointAndUv(vert2, normal2, vert3, normal3, out intersection1Uv);
                            intersection2 = GetRayPlaneIntersectionPointAndUv(vert1, normal1, vert3, normal3, out intersection2Uv);

                            trianglesOfPlane.Add(vert1);
                            trianglesOfPlane.Add(vert2);
                            trianglesOfPlane.Add(vert3);
                            trianglesOfPlane.Add(intersection1);
                            trianglesOfPlane.Add(intersection2);

                            int boneNum = -1;
                            int boneNum2 = -1;

                            //Add the positive or negative triangles
                            AddTrianglesNormalAndUvs(side1, vert1, meshTriangles[i], null, uv1, vert2, meshTriangles[i + 1], null, uv2, intersection1, boneNum, null, intersection1Uv, false);
                            AddTrianglesNormalAndUvs(side1, vert1, meshTriangles[i], null, uv1, intersection1, boneNum, null, intersection1Uv, intersection2, boneNum2, null, intersection2Uv, false);

                            AddTrianglesNormalAndUvs(side2, intersection1, boneNum, null, intersection1Uv, vert3, meshTriangles[i + 1], null, uv3, intersection2, boneNum2, null, intersection2Uv, false);
                        }
                        else if (vert1Side == vert3Side)
                        {
                            intersection1 = GetRayPlaneIntersectionPointAndUv(vert1, normal1, vert2, normal2, out intersection1Uv);
                            intersection2 = GetRayPlaneIntersectionPointAndUv(vert3, normal3, vert2, normal2, out intersection2Uv);

                            trianglesOfPlane.Add(vert1);
                            trianglesOfPlane.Add(vert2);
                            trianglesOfPlane.Add(vert3);
                            trianglesOfPlane.Add(intersection1);
                            trianglesOfPlane.Add(intersection2);

                            int boneNum = -1;
                            int boneNum2 = -1;

                            AddTrianglesNormalAndUvs(side1, vert1, meshTriangles[i], null, uv1, intersection1, boneNum, null, intersection1Uv, vert3, meshTriangles[i + 2], null, uv3, false);
                            AddTrianglesNormalAndUvs(side1, intersection1, boneNum, null, intersection1Uv, intersection2, boneNum2, null, intersection2Uv, vert3, meshTriangles[i + 2], null, uv3, false);

                            AddTrianglesNormalAndUvs(side2, intersection1, boneNum, null, intersection1Uv, vert2, meshTriangles[i + 2], null, uv2, intersection2, boneNum2, null, intersection2Uv, false);
                        }
                        else
                        {
                            intersection1 = GetRayPlaneIntersectionPointAndUv(vert2, normal2, vert1, normal1, out intersection1Uv);
                            intersection2 = GetRayPlaneIntersectionPointAndUv(vert3, normal3, vert1, normal1, out intersection2Uv);

                            trianglesOfPlane.Add(vert1);
                            trianglesOfPlane.Add(vert2);
                            trianglesOfPlane.Add(vert3);
                            trianglesOfPlane.Add(intersection1);
                            trianglesOfPlane.Add(intersection2);

                            int boneNum = -1;
                            int boneNum2 = -1;

                            AddTrianglesNormalAndUvs(side1, vert1, meshTriangles[i], null, uv1, intersection1, boneNum, null, intersection1Uv, intersection2, boneNum2, null, intersection2Uv, false);

                            AddTrianglesNormalAndUvs(side2, intersection1, boneNum, null, intersection1Uv, vert2, meshTriangles[i + 1], null, uv2, vert3, meshTriangles[i + 2], null, uv3, false);
                            AddTrianglesNormalAndUvs(side2, intersection1, boneNum, null, intersection1Uv, vert3, meshTriangles[i + 2], null, uv3, intersection2, boneNum, null, intersection2Uv, false);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Compute the positive and negative meshes based on the plane and mesh
        /// </summary>
        private void ComputeNewMeshes()
        {
            int[] meshTriangles1 = _mesh.triangles;
            Vector3[] meshVerts1 = _mesh.vertices;
            Vector3[] meshNormals1 = _mesh.normals;
            Vector2[] meshUvs1 = _mesh.uv;

            float time2 = Time.realtimeSinceStartup;
            time2 = Time.realtimeSinceStartup - time2;

            NativeArray<int> meshTrianglesTemp = new NativeArray<int>(meshTriangles1.Length, Allocator.TempJob);
            for (int i = 0; i < meshTriangles1.Length; i++)
            {
                meshTrianglesTemp[i] = meshTriangles1[i];
            }
            
            NativeArray<Vector3> meshVertsTemp = new NativeArray<Vector3>(meshVerts1.Length, Allocator.TempJob);
            for (int i = 0; i < meshVerts1.Length; i++)
            {
                meshVertsTemp[i] = meshVerts1[i];
            }

            NativeArray<Vector3> meshNormalsTemp = new NativeArray<Vector3>(meshNormals1.Length, Allocator.TempJob);
            for (int i = 0; i < meshNormals1.Length; i++)
            {
                meshNormalsTemp[i] = meshNormals1[i];
            }

            NativeArray<Vector2> meshUvsTemp = new NativeArray<Vector2>(meshUvs1.Length, Allocator.TempJob);
            for (int i = 0; i < meshUvs1.Length; i++)
            {
                meshUvsTemp[i] = meshUvs1[i];
            }
            
            CalculateSideJob calculateSideJob = new CalculateSideJob()
            {
                plane = _plane,
                meshTriangles = meshTrianglesTemp,
                meshVerts = meshVertsTemp,
                meshNormals = meshNormalsTemp,
                meshUVs = meshUvsTemp,
                trianglesOfPlane = new NativeList<Vector3>(Allocator.TempJob),
                positiveSideVerts = new NativeList<Vector3>(Allocator.TempJob),
                positiveSideTriangles = new NativeList<int>(Allocator.TempJob),
                positiveSideNormals = new NativeList<Vector3>(Allocator.TempJob),
                positiveSideUVs = new NativeList<Vector2>(Allocator.TempJob),
                negativeSideVerts = new NativeList<Vector3>(Allocator.TempJob),
                negativeSideTriangles = new NativeList<int>(Allocator.TempJob),
                negativeSideNormals = new NativeList<Vector3>(Allocator.TempJob),
                negativeSideUVs = new NativeList<Vector2>(Allocator.TempJob),
                orderOfVertInOrigin = new NativeList<int>(Allocator.TempJob)
            };

            JobHandle handle = calculateSideJob.Schedule();
            
            //If the object is solid, join the new points along the plane otherwise do the reverse winding
            if (_isSolid)
            {
                float time = Time.realtimeSinceStartup;
                List<List<Vector3>> vericesPlane = new List<List<Vector3>>();
                List<Vector3> planeVectors1 = new List<Vector3>();
                List<Vector3> planeVectors2 = new List<Vector3>();
                
                foreach (KeyValuePair<Vector3[], Vector3[]> pair in trianglesOfPlane)
                {
                    foreach (KeyValuePair<Vector3[], Vector3[]> pairCompare in trianglesOfPlane)
                    {
                        if (pairCompare.Key != pair.Key)
                        {
                            Vector3[] fromPair = pair.Value;
                            Vector3[] fromPairCompare = pairCompare.Value;

                            for (int a = 0; a < pair.Value.Length; a++)
                            {
                                for (int b = 0; b < pairCompare.Value.Length; b++)
                                {
                                    if (fromPair[a] == fromPairCompare[b])
                                    {
                                        List<Vector3> vector3s = new List<Vector3>();
                                        vector3s.Add(pair.Value[0]);
                                        vector3s.Add(pair.Value[1]);
                                        vector3s.Add(fromPairCompare[0]);
                                        vector3s.Add(fromPairCompare[1]);
                                        vericesPlane.Add(vector3s);
                                    }
                                }
                            }
                        }
                    }
                }
                
                int lenghtOf = vericesPlane.Count;
                for (int i = 1; i < lenghtOf; i++)
                {
                    bool isConnect = false;
                    for (int a = 0; a < vericesPlane[0].Count; a++)
                    {
                        if (vericesPlane[i].Contains(vericesPlane[0][a]))
                        {
                            isConnect = true;
                            vericesPlane[i].Remove(vericesPlane[0][a]);
                        }
                    }
                    
                    if (isConnect == true)
                    {
                        for (int b = 0; b < vericesPlane[i].Count; b++)
                        {
                            vericesPlane[0].Add(vericesPlane[i][b]);
                        }
                        vericesPlane.Remove(vericesPlane[i]);

                        lenghtOf = vericesPlane.Count;
                        i = 0;
                    }
                }
                
                for (int i = 2; i < lenghtOf; i++)
                {
                    bool isConnect = false;
                    for (int a = 0; a < vericesPlane[1].Count; a++)
                    {
                        if (vericesPlane[i].Contains(vericesPlane[1][a]))
                        {
                            isConnect = true;
                            vericesPlane[i].Remove(vericesPlane[1][a]);
                        }
                    }

                    if (isConnect == true)
                    {
                        for (int b = 0; b < vericesPlane[i].Count; b++)
                        {
                            vericesPlane[1].Add(vericesPlane[i][b]);
                        }
                        vericesPlane.Remove(vericesPlane[i]);
                        
                        lenghtOf = vericesPlane.Count;
                        i = 1;
                    }
                }

                for (int i = 3; i < lenghtOf; i++)
                {
                    bool isConnect = false;
                    for (int a = 0; a < vericesPlane[1].Count; a++)
                    {
                        if (vericesPlane[i].Contains(vericesPlane[1][a]))
                        {
                            isConnect = true;
                            vericesPlane[i].Remove(vericesPlane[1][a]);
                        }
                    }

                    if (isConnect == true)
                    {
                        for (int b = 0; b < vericesPlane[i].Count; b++)
                        {
                            vericesPlane[1].Add(vericesPlane[i][b]);
                        }
                        vericesPlane.Remove(vericesPlane[i]);

                        lenghtOf = vericesPlane.Count;
                        i = 1;
                    }
                }
                
                time = Time.realtimeSinceStartup - time;
                Debug.Log(time);
                if (vericesPlane.Count == 1)
                {
                    JoinPointsAlongPlane();
                }
                else
                {
                    List<Vector3> inOrder1 = new List<Vector3>();
                    for (int i = 0; i < _pointsAlongPlane.Count - 1; i += 2)
                    {
                        if (vericesPlane[0].Contains(_pointsAlongPlane[i]) == true)
                        {
                            inOrder1.Add(_pointsAlongPlane[i]);
                            inOrder1.Add(_pointsAlongPlane[i + 1]);
                        }
                    }
                    Debug.Log(_pointsAlongPlane.Count);
                    Debug.Log(vericesPlane[0].Count);
                    List<Vector3> inOrder2 = new List<Vector3>();
                    for (int i = 0; i < _pointsAlongPlane.Count - 1; i += 2)
                    {
                        if (vericesPlane[1].Contains(_pointsAlongPlane[i]) == true)
                        {
                            inOrder2.Add(_pointsAlongPlane[i]);
                            inOrder2.Add(_pointsAlongPlane[i + 1]);
                        }
                    }
                    inOrder1.Add(_pointsAlongPlane[_pointsAlongPlane.Count - 1]);
                    inOrder1.Add(inOrder1[0]);
                    JoinPointsAlongPlane(inOrder1);
                    JoinPointsAlongPlane(inOrder2);
                }
                
            }
            else if (_createReverseTriangleWindings)
            {
                AddReverseTriangleWinding();
            }

            if (_smoothVertices)
            {
                SmoothVertices();
            }
        }

        struct GetRayIntersectionThread : IJob
        {
            public Plane plane1;
            public Vector3 vectorGetRay;
            public Vector2 uvGetRay;
            public Vector3 vectorGetRay2;
            public Vector2 uvGetRay2;

            public Vector2 uvOut;

            public NativeArray<Vector3> returnedVector;

            Vector3 GetRayPlaneIntersectionPointAndUv(Vector3 vertex1, Vector2 vertex1Uv, Vector3 vertex2, Vector2 vertex2Uv, out Vector2 uv)
            {
                float distance = GetDistanceRelativeToPlane(vertex1, vertex2, out Vector3 pointOfIntersection);
                uv = InterpolateUvs(vertex1Uv, vertex2Uv, distance);
                return pointOfIntersection;
            }

            private float GetDistanceRelativeToPlane(Vector3 vertex1, Vector3 vertex2, out Vector3 pointOfintersection)
            {
                Ray ray = new Ray(vertex1, (vertex2 - vertex1));
                plane1.Raycast(ray, out float distance);
                pointOfintersection = ray.GetPoint(distance);
                return distance;
            }

            private Vector2 InterpolateUvs(Vector2 uv1, Vector2 uv2, float distance)
            {
                Vector2 uv = Vector2.Lerp(uv1, uv2, distance);
                return uv;
            }

            public void Execute()
            {
                returnedVector[0] = GetRayPlaneIntersectionPointAndUv(vectorGetRay, uvGetRay, vectorGetRay2, uvGetRay2, out uvOut);
            }
        };

        /// <summary>
        /// Casts a reay from vertex1 to vertex2 and gets the point of intersection with the plan, calculates the new uv as well.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="vertex1">The vertex1.</param>
        /// <param name="vertex1Uv">The vertex1 uv.</param>
        /// <param name="vertex2">The vertex2.</param>
        /// <param name="vertex2Uv">The vertex2 uv.</param>
        /// <param name="uv">The uv.</param>
        /// <returns>Point of intersection</returns>
        private Vector3 GetRayPlaneIntersectionPointAndUv(Vector3 vertex1, Vector2 vertex1Uv, Vector3 vertex2, Vector2 vertex2Uv, out Vector2 uv)
        {
            float distance = GetDistanceRelativeToPlane(vertex1, vertex2, out Vector3 pointOfIntersection);
            uv = InterpolateUvs(vertex1Uv, vertex2Uv, distance);
            return pointOfIntersection;
        }

        /// <summary>
        /// Computes the distance based on the plane.
        /// </summary>
        /// <param name="vertex1">The vertex1.</param>
        /// <param name="vertex2">The vertex2.</param>
        /// <param name="pointOfintersection">The point ofintersection.</param>
        /// <returns></returns>
        private float GetDistanceRelativeToPlane(Vector3 vertex1, Vector3 vertex2, out Vector3 pointOfintersection)
        {
            Ray ray = new Ray(vertex1, (vertex2 - vertex1));
            _plane.Raycast(ray, out float distance);
            pointOfintersection = ray.GetPoint(distance);
            return distance;
        }

        /// <summary>
        /// Get a uv between the two provided uvs by the distance.
        /// </summary>
        /// <param name="uv1">The uv1.</param>
        /// <param name="uv2">The uv2.</param>
        /// <param name="distance">The distance.</param>
        /// <returns></returns>
        private Vector2 InterpolateUvs(Vector2 uv1, Vector2 uv2, float distance)
        {
            Vector2 uv = Vector2.Lerp(uv1, uv2, distance);
            return uv;
        }

        /// <summary>
        /// Gets the point perpendicular to the face defined by the provided vertices        
        //https://docs.unity3d.com/Manual/ComputingNormalPerpendicularVector.html
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="vertex3"></param>
        /// <returns></returns>
        private Vector3 ComputeNormal(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            Vector3 side1 = vertex2 - vertex1;
            Vector3 side2 = vertex3 - vertex1;

            Vector3 normal = Vector3.Cross(side1, side2);

            return normal;
        }

        /// <summary>
        /// Reverese the normals in a given list
        /// </summary>
        /// <param name="currentNormals"></param>
        /// <returns></returns>
        private List<Vector3> FlipNormals(List<Vector3> currentNormals)
        {
            List<Vector3> flippedNormals = new List<Vector3>();

            foreach (Vector3 normal in currentNormals)
            {
                flippedNormals.Add(-normal);
            }

            return flippedNormals;
        }

        //
        private void SmoothVertices()
        {
            DoSmoothing(ref _positiveSideVertices, ref _positiveSideNormals, ref _positiveSideTriangles);
            DoSmoothing(ref _negativeSideVertices, ref _negativeSideNormals, ref _negativeSideTriangles);
        }

        private void DoSmoothing(ref List<Vector3> vertices, ref List<Vector3> normals, ref List<int> triangles)
        {
            normals.ForEach(x =>
            {
                x = Vector3.zero;
            });

            for (int i = 0; i < triangles.Count; i += 3)
            {
                int vertIndex1 = triangles[i];
                int vertIndex2 = triangles[i + 1];
                int vertIndex3 = triangles[i + 2];

                Vector3 triangleNormal = ComputeNormal(vertices[vertIndex1], vertices[vertIndex2], vertices[vertIndex3]);

                normals[vertIndex1] += triangleNormal;
                normals[vertIndex2] += triangleNormal;
                normals[vertIndex3] += triangleNormal;
            }

            normals.ForEach(x =>
            {
                x.Normalize();
            });
        }
    }
}
