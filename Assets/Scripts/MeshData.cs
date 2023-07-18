using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using static Unity.Burst.Intrinsics.X86;
using static UnityEngine.Mesh;
using Microsoft.SqlServer.Server;
using UnityEngine.Playables;
using static UnityEngine.GraphicsBuffer;

public class FaceSpline
{
    public List<Vector3> points = new List<Vector3>();
}

public class FaceAxisLines
{
    Plane axisPlane;

    Dictionary<Vector3, Vector3> connectionOne = new Dictionary<Vector3, Vector3>(new Vector3ApproxComparer());
    Dictionary<Vector3, Vector3> connectionTwo = new Dictionary<Vector3, Vector3>(new Vector3ApproxComparer());
    HashSet<Vector3> singleConnections = new HashSet<Vector3>(new Vector3ApproxComparer());

    public List<FaceSpline> GetFaceSplines()
    {
        if (connectionOne.Count < 1)
            return null;

        List<FaceSpline> faceSplines = new List<FaceSpline>();

        Vector3 firstKey = Vector3.zero;

        if (singleConnections.Count == 0)
            firstKey = connectionOne.Keys.First();
        else
            firstKey = singleConnections.First();

        Vector3 currentPoint = firstKey;
        Vector3 nextPoint = Vector3.zero;
        Vector3 prevPoint = currentPoint;

        while (firstKey != null)
        {
            FaceSpline faceSpline = new FaceSpline();
            while (currentPoint != null)
            {
                if (singleConnections.Contains(currentPoint))
                    singleConnections.Remove(currentPoint);

                faceSpline.points.Add(currentPoint);

                bool hasC1 = connectionOne.ContainsKey(currentPoint);
                bool hasC2 = connectionTwo.ContainsKey(currentPoint);

                if (!hasC1)
                {
                    break;
                }

                nextPoint = connectionOne[currentPoint];

                if (nextPoint == prevPoint)
                {
                    if (!hasC2)
                        break;

                    nextPoint = connectionTwo[currentPoint];
                }

                prevPoint = currentPoint;
                currentPoint = nextPoint;

                if (currentPoint == firstKey)
                {
                    faceSpline.points.Add(currentPoint);
                    break;
                }
  
            }

            faceSplines.Add(faceSpline);

            if (singleConnections.Count > 0)
            {
                firstKey = singleConnections.First();
                currentPoint = firstKey;
                nextPoint = Vector3.zero;
                prevPoint = currentPoint;
            }
            else
            {
                break;
            }
        }

        if (faceSplines.Count < 1)
            return null;

        return faceSplines;
    }

    public FaceAxisLines(Vector3 faceCenter, Vector3 planeNormal)
    {
        axisPlane = new Plane(planeNormal, faceCenter);
    }

    public void CheckTriangle(Vector3[] verts)
    {
        bool trianglePlaneIntersection = axisPlane.TriangleIntersection(verts[0], verts[1], verts[2], out Vector3 hitA, out Vector3 hitB);

        if(trianglePlaneIntersection)
        {
            if (Vector3ApproxComparer.ApproxEqual(hitA, hitB))
            {
                //MeshDataDebug.debugPoints.Add(hitA);
                return;
            }

            AddLine(hitA, hitB);
        }
    }

    public void AddLine(Vector3 pointA, Vector3 pointB)
    {
        if(connectionOne.ContainsKey(pointA))
        {
            if (Vector3ApproxComparer.ApproxEqual(connectionOne[pointA], pointB))
                return;

            if (!connectionTwo.ContainsKey(pointA))
            {
                connectionTwo.Add(pointA, pointB);
                if (singleConnections.Contains(pointA))
                    singleConnections.Remove(pointA);
            }
        }
        else
        {
            connectionOne.Add(pointA, pointB);
            if (!singleConnections.Contains(pointA))
                singleConnections.Add(pointA);
        }

        if (connectionOne.ContainsKey(pointB))
        {
            if (Vector3ApproxComparer.ApproxEqual(connectionOne[pointB], pointA))
                return;

            if (!connectionTwo.ContainsKey(pointB))
            {
                connectionTwo.Add(pointB, pointA);
                if (singleConnections.Contains(pointB))
                    singleConnections.Remove(pointB);
            }
        }
        else
        {
            connectionOne.Add(pointB, pointA);
            if (!singleConnections.Contains(pointB))
                singleConnections.Add(pointB);
        }
    }
}

public class FaceData
{
    public Bounds bounds;
    public Vector3 avgPosition;
    public Vector3 avgNormal;
    public int[] vertIndexes;
    public int[] triangleIndexes;
    public SortedList<int, int[]> triangleAdjacency;
}

public class FacePoints
{
    public Vector3[] positions;
    public Vector3[] normals;
}

public class FaceCollection
{
    public SortedSet<int> triangles = new SortedSet<int>();
    public Dictionary<int, HashSet<int>> triangleAdjacency = new Dictionary<int, HashSet<int>>();
    public SortedSet<int> verts = new SortedSet<int>();
    public Dictionary<int, Vector3> positionLookup = new Dictionary<int, Vector3>();
    public Dictionary<int, Vector3> normalLookup = new Dictionary<int, Vector3>();
    public Vector3 avgVertPosition = Vector3.zero;
    public Vector3 avgNormal = Vector3.zero;
    public Bounds bounds = new Bounds();

    public bool Contains(int vert)
    {
        return verts.Contains(vert);
    }

    public void Add(int triangle, int[] verts, Vector3[] positions, Vector3[] normals)
    {
        triangles.Add(triangle);
        for(int vertIndex = 0; vertIndex < verts.Length; ++vertIndex)
        {
            int vert = verts[vertIndex];
            bool addedVert = this.verts.Add(vert);

            if(addedVert)
            {
                if(!this.positionLookup.ContainsKey(vert))
                    this.positionLookup.Add(vert, positions[vertIndex]);
                if (!this.normalLookup.ContainsKey(vert))
                    this.normalLookup.Add(vert, normals[vertIndex]);

                UpdateAverages(vert, this.verts.Count);
                UpdateBounds(positionLookup[vert], this.verts.Count);
            }
        }
    }

    public void AddRange(FaceCollection other)
    {
        triangles.AddRange(other.triangles);

        foreach(int vert in other.verts)
        {
            bool addedVert = this.verts.Add(vert);

            if (addedVert)
            {
                Vector3 otherPos = other.positionLookup[vert];
                Vector3 otherNormal = other.normalLookup[vert];
                if (!this.positionLookup.ContainsKey(vert))
                    this.positionLookup.Add(vert, otherPos);
                if (!this.normalLookup.ContainsKey(vert))
                    this.normalLookup.Add(vert, otherNormal);

                UpdateAverages(vert, this.verts.Count);
                UpdateBounds(positionLookup[vert], this.verts.Count);
            }
        }
    }

    void UpdateAverages(int vert, int vertCount)
    {
        if (positionLookup.ContainsKey(vert))
            avgVertPosition += (positionLookup[vert] - avgVertPosition) / vertCount;
        if (normalLookup.ContainsKey(vert))
            avgNormal += (normalLookup[vert] - avgNormal) / vertCount;
    }

    void UpdateBounds(Vector3 vert, int vertCount)
    {
        if (vertCount == 1)
            bounds.center = vert;

        bounds.Encapsulate(vert);
    }

    public void Finalise()
    {
        if (avgVertPosition.sqrMagnitude < 0.001f)
            avgVertPosition = Vector3.zero;

        if (avgNormal.sqrMagnitude < 0.001f)
            avgNormal = Vector3.up;

        avgNormal.Normalize();
    }

    public FaceData ToFaceData()
    {
        Finalise();

        FaceData faceData = new FaceData();
        faceData.avgNormal = avgNormal;
        faceData.avgPosition = avgVertPosition;
        faceData.vertIndexes = verts.ToArray();
        faceData.triangleIndexes = triangles.ToArray();
        faceData.bounds = bounds;

        faceData.triangleAdjacency = new SortedList<int, int[]>();
        foreach(int triangle in triangleAdjacency.Keys)
        {
            int[] adjacent = triangleAdjacency[triangle].ToArray();
            faceData.triangleAdjacency.Add(triangle, adjacent);
        }

        return faceData;
    }

    public Bounds GetBounds()
    {
        return bounds;
    }
}

public class FaceSplineBuilder
{
    List<List<FaceAxisLines>> faceSplineCollections = new List<List<FaceAxisLines>>();

    public List<List<FaceAxisLines>> GetFaceSplineCollection()
    {
        return faceSplineCollections;
    }

    public void BuildSplinesFromFaceCollections(MeshData meshData, List<FaceCollection> faceCollections)
    {
        for(int faceIndex = 0; faceIndex < faceCollections.Count; ++faceIndex)
        {
            List<FaceAxisLines> faceAxisLines = new List<FaceAxisLines>();
            faceSplineCollections.Add(faceAxisLines);
            FaceAxisLines tangentLines = null;
            FaceAxisLines binormalLines = null;

            FaceCollection faceCollection = faceCollections[faceIndex];
            Vector3 avgPos = faceCollection.avgVertPosition;
            Vector3 avgNormal = faceCollection.avgNormal;

            Vector3 tangent = Vector3.zero;
            Vector3 binormal = Vector3.zero;

            Vector3 forwardCross = Vector3.Cross(avgNormal, Vector3.forward);
            Vector3 upCross = Vector3.Cross(avgNormal, Vector3.up);
            if (forwardCross.sqrMagnitude > upCross.sqrMagnitude)
            {
                tangent = forwardCross;
                binormal = Vector3.Cross(tangent, avgNormal);
            }
            else
            {
                tangent = upCross;
                binormal = Vector3.Cross(tangent, avgNormal);
            }

            if(faceIndex == 0)
            {
                SceneObjDebug.debugPlanes.Add(new PlaneDebug() { position = avgPos, normal = tangent });
                SceneObjDebug.debugPlanes.Add(new PlaneDebug() { position = avgPos, normal = binormal });
            }

            tangentLines = new FaceAxisLines(avgPos, tangent);
            binormalLines = new FaceAxisLines(avgPos, binormal);

            faceAxisLines.Add(tangentLines);
            faceAxisLines.Add(binormalLines);

            foreach (int triangleIndex in faceCollection.triangles)
            {
                int[] verts = { meshData.triangles[triangleIndex], meshData.triangles[triangleIndex + 1], meshData.triangles[triangleIndex + 2] };
                Vector3[] positions = { meshData.vertices[verts[0]], meshData.vertices[verts[1]], meshData.vertices[verts[2]] };

                tangentLines.CheckTriangle(positions);
                binormalLines.CheckTriangle(positions);
            }
        }
    }
}

public class FaceBuilder
{
    List<FaceCollection> faceCollections = new List<FaceCollection>();
    Dictionary<int, List<int>> triangleLookup = new Dictionary<int, List<int>>();
    Dictionary<Vector3, List<int>> vertexLookup = new Dictionary<Vector3, List<int>>();
    Dictionary<int, Vector3> normalLookup = new Dictionary<int, Vector3>();

    public int Count()
    {
        return faceCollections.Count;
    }

    public List<FaceCollection> GetCollections()
    {
        return faceCollections;
    }

    public FaceData[] ToArray()
    {
        FaceData[] faceDataArray = new FaceData[faceCollections.Count];
        for(int listIndex = 0; listIndex < faceCollections.Count; ++listIndex)
        {
            faceDataArray[listIndex] = faceCollections[listIndex].ToFaceData();
        }

        return faceDataArray;
    }

    public void AddTriangle(int triangleIndex, int[] verts, Vector3[] positions, Vector3[] normals)
    {
        FaceCollection baseCollection = null;
        for (int vertIndex = 0; vertIndex < verts.Length; ++vertIndex)
        {
            int vert = verts[vertIndex];
            // Find any collections this vert already belongs to find shared verts
            FaceCollection vertCollection = FindList(vert);

            // Check fror unshared but duplicate verts - verts that are the same position and a similar normal
            if (vertCollection == null)
                vertCollection = CheckForDuplicateVert(vert, positions[vertIndex], normals[vertIndex]);

            // Set the collection to update as the first existing collection we find
            if (baseCollection == null && vertCollection != null)
            {
                baseCollection = vertCollection;
            }

            // If we have two conflicting vert collections then merge them
            if(baseCollection != null && vertCollection != null && baseCollection != vertCollection)
            {
                baseCollection.AddRange(vertCollection);
                faceCollections.Remove(vertCollection);
            }

            UpdateTriangleAdjacency(baseCollection, positions[vertIndex], triangleIndex);
            AddToTriangleLookup(vert, triangleIndex);
            AddToVertLookup(vert, positions[vertIndex], normals[vertIndex]);
        }

        // There is no exisitng face connected to this triangle - so create one
        if (baseCollection == null)
        {
            baseCollection = new FaceCollection();
            faceCollections.Add(baseCollection);
        }

        baseCollection.Add(triangleIndex, verts, positions, normals);
    }

    void AddToTriangleLookup(int vert, int triangleIndex)
    {
        if (!triangleLookup.ContainsKey(vert))
        {
            triangleLookup.Add(vert, new List<int>());
        }

        if (triangleLookup[vert].Contains(triangleIndex))
            return;

        triangleLookup[vert].Add(triangleIndex);
    }

    void UpdateTriangleAdjacency(FaceCollection baseCollection, Vector3 position, int triangleIndex)
    {
        // Do we have an existing face for this triangle?
        if (baseCollection == null)
            return;

        if (!vertexLookup.ContainsKey(position))
            return;

        List<int> vertList = vertexLookup[position];
        for (int listIndex = 0; listIndex < vertList.Count; ++listIndex)
        {
            int vertIndex = vertList[listIndex];

            if (!triangleLookup.ContainsKey(vertIndex))
                continue;

            List<int> foundTriangleList = triangleLookup[vertIndex];
            for (int triangleListIndex = 0; triangleListIndex < foundTriangleList.Count; ++triangleListIndex)
            {
                int foundTriangleIndex = foundTriangleList[triangleListIndex];
                if (foundTriangleIndex == triangleIndex)
                    continue;

                // Found triangle is not in this existing face
                if (!baseCollection.triangles.Contains(foundTriangleIndex))
                    continue;

                Dictionary<int, HashSet<int>> triangleAdjacency = baseCollection.triangleAdjacency;

                if (!triangleAdjacency.ContainsKey(triangleIndex))
                    triangleAdjacency.Add(triangleIndex, new HashSet<int>());

                if (!triangleAdjacency.ContainsKey(foundTriangleIndex))
                    triangleAdjacency.Add(foundTriangleIndex, new HashSet<int>());

                if(!triangleAdjacency[triangleIndex].Contains(foundTriangleIndex))
                    triangleAdjacency[triangleIndex].Add(foundTriangleIndex);

                if(!triangleAdjacency[foundTriangleIndex].Contains(triangleIndex))
                    triangleAdjacency[foundTriangleIndex].Add(triangleIndex);
            }
        }
    }

    void AddToVertLookup(int vert, Vector3 position, Vector3 normal)
    {
        if (normalLookup.ContainsKey(vert))
            return;

        if (vertexLookup.ContainsKey(position))
            vertexLookup[position].Add(vert);
        else
            vertexLookup.Add(position, new List<int>{vert});

        normalLookup.Add(vert, normal);
    }

    FaceCollection CheckForDuplicateVert(int vert, Vector3 position, Vector3 normal)
    {
        const float normalThreshold = 30.0f;
        if (!vertexLookup.ContainsKey(position))
            return null;

        List<int> vertList = vertexLookup[position];
        for (int listIndex = 0; listIndex < vertList.Count; ++listIndex)
        {
            int duplicateVert = vertList[listIndex];

            if (duplicateVert != vert)
            {
                if (Vector3.Angle(normalLookup[duplicateVert], normal) <= normalThreshold)
                {
                    return FindList(duplicateVert);
                }
            }
        }

        return null;
    }

    public FaceCollection FindList(int vert)
    {
        for(int fcIndex = 0; fcIndex < faceCollections.Count; ++fcIndex)
        {
            if (faceCollections[fcIndex].Contains(vert))
                return faceCollections[fcIndex];
        }

        return null;
    }

    public void Finalise()
    {
        for (int fcIndex = 0; fcIndex < faceCollections.Count; ++fcIndex)
        {
            faceCollections[fcIndex].Finalise();
        }
    }
}

public class MeshData
{
    Mesh mesh = null;
    public Vector3[] vertices;
    public int[] triangles;
    public Vector3[] normals;
    public FaceData[] faces;
    public FacePoints[] facePoints;

    public MeshData(Mesh mesh)
    {
        this.mesh = mesh;
        Build();
    }

    void Build()
    {
        if (mesh == null)
        {
            Debug.LogAssertion("Not mesh attached to mesh data");
            return;
        }

        vertices = mesh.vertices;
        triangles = mesh.triangles;
        normals = mesh.normals;

        GetFacesAndNormals();
    }

    public string GetName()
    {
        if (mesh == null)
            return null;

       return mesh.name;
    }

    public bool GetTrianglePositions(int triangleIndex, Transform transform, ref Vector3[] positions)
    {
        if (vertices.Length < 1 || triangles.Length < 1 || positions.Length < 3)
            return false;

        positions[0] = transform.TransformPoint(vertices[triangles[triangleIndex]]);
        positions[1] = transform.TransformPoint(vertices[triangles[triangleIndex + 1]]);
        positions[2] = transform.TransformPoint(vertices[triangles[triangleIndex + 2]]);

        return true;
    }

    public bool GetVertPosition(int vertIndex, Transform transform, ref Vector3 position)
    {
        if (vertices.Length < 1)
            return false;

        position = transform.TransformPoint(vertices[vertIndex]);

        return true;
    }

    public bool GetNormal(int vertIndex, ref Vector3 normal)
    {
        if (normals.Length < 1)
            return false;

        normal = normals[vertIndex];

        return true;
    }

    public void GetFacesAndNormals(float normalAngleMax = 31.0f)
    {
        FaceBuilder faceBuilder = new FaceBuilder();

        for(int triangleIndex = 0; triangleIndex < triangles.Length-2; triangleIndex+=3)
        {
            int[] vertIndexes = { triangles[triangleIndex], triangles[triangleIndex + 1], triangles[triangleIndex + 2]};
            Vector3[] verts = { vertices[vertIndexes[0]], vertices[vertIndexes[1]], vertices[vertIndexes[2]]};
            Vector3[] vertNormals = { normals[vertIndexes[0]], normals[vertIndexes[1]], normals[vertIndexes[2]]};

            Vector3 avgNormal = (vertNormals[0] + vertNormals[1] + vertNormals[2])/3;

            faceBuilder.AddTriangle(triangleIndex, vertIndexes, verts, vertNormals);
        }

        faceBuilder.Finalise();

        if(faceBuilder.Count() > 0)
        {
            faces = faceBuilder.ToArray();

            FaceSplineBuilder faceSplineBuilder = new FaceSplineBuilder();
            faceSplineBuilder.BuildSplinesFromFaceCollections(this, faceBuilder.GetCollections());
            SceneObjDebug.AddSplinesDebug(faceSplineBuilder);
        }
    }
}

public class PlaneDebug
{
    public Vector3 position;
    public Vector3 normal;
}

public static class SceneObjDebug
{
    public static List<int> debugVerts = new List<int>();
    public static List<Vector3> debugPoints = new List<Vector3>();
    public static List<FaceSpline> debugSplines = new List<FaceSpline>();
    public static List<PlaneDebug> debugPlanes = new List<PlaneDebug>();

    public static void AddSplinesDebug(FaceSplineBuilder faceSplineBuilder)
    {
        List<List<FaceAxisLines>> faceAxisLines = faceSplineBuilder.GetFaceSplineCollection();

        if (faceAxisLines == null || faceAxisLines.Count < 1)
            return;

        for (int faceIndex = 0; faceIndex < faceAxisLines.Count; ++faceIndex)
        {
            for (int splineIndex = 0; splineIndex < faceAxisLines[faceIndex].Count; ++splineIndex)
            {
                List<FaceSpline> faceSplines = faceAxisLines[faceIndex][splineIndex].GetFaceSplines();
                if(faceSplines != null)
                    debugSplines.AddRange(faceSplines);
            }
        }
    }

    public static void Draw(MeshData meshData)
    {
        //DrawFacePoints(meshData);
        //DrawDebugPlanes();
        //DrawDebugPoints();
        //DrawDebugSplines();
    }

    public static void DrawDebugSplines()
    {
        for (int debugIndex = 0; debugIndex < debugSplines.Count; ++debugIndex)
        {
            for (int pointIndex = 1; pointIndex < debugSplines[debugIndex].points.Count; ++pointIndex)
            {
                Vector3 prevPos = debugSplines[debugIndex].points[pointIndex-1];
                Vector3 nextPos = debugSplines[debugIndex].points[pointIndex];

                Color arrowCol = Color.cyan;
                Debug.DrawLine(prevPos, nextPos, arrowCol);
            }
        }
    }

    public static void DrawDebugPoints()
    {
        for (int debugIndex = 0; debugIndex < debugPoints.Count; ++debugIndex)
        {
            Vector3 pointPos = debugPoints[debugIndex];
            DebugExtension.DrawPoint(pointPos, Color.cyan, 0.1f);
        }
    }

    public static void DrawDebugVerts(SceneObjectRef sceneObj)
    {
        Vector3 vertPos = Vector3.zero;
        Vector3 normal = Vector3.zero;

        for (int debugIndex = 0; debugIndex < debugVerts.Count; ++debugIndex)
        {
            sceneObj.GetVertPosition(debugVerts[debugIndex], ref vertPos);
            sceneObj.GetNormal(debugVerts[debugIndex], ref normal);

            Color arrowCol = debugIndex % 2 == 0 ? Color.cyan : Color.green;
            DebugExtension.DrawArrow(vertPos, normal, arrowCol);
        }
    }

    public static void DrawFacePoints(SceneObjectRef sceneObj)
    {
        if (!Application.isPlaying)
            return;

        MeshData meshData = sceneObj.GetMeshData();

        if (meshData == null || meshData.faces == null || meshData.faces.Length < 1)
            return;

        Vector3 vertPos = Vector3.zero;

        for (int faceIndex = 0; faceIndex < meshData.faces.Length; ++faceIndex)
        {
            Vector3 normal = meshData.faces[faceIndex].avgNormal;

            sceneObj.TransformPosition(meshData.faces[faceIndex].avgPosition, ref vertPos);

            DebugExtension.DrawArrow(vertPos, normal, Color.red);
        }
    }

    public static void DrawDebugPlanes()
    {
        for (int planeIndex = 0; planeIndex < debugPlanes.Count; ++planeIndex)
        {
            PlaneDebug debugPlane = debugPlanes[planeIndex];
            float cornerOffset = 1.0f;
            Vector3 rightCross = Vector3.Cross(Vector3.right, debugPlane.normal);
            Vector3 forwardCross = Vector3.Cross(Vector3.back, debugPlane.normal);

            Vector3 tangent = rightCross.sqrMagnitude > forwardCross.sqrMagnitude ? rightCross : forwardCross;

            Vector3 right = Vector3.Cross(debugPlane.normal, tangent);

            Vector3 tl = debugPlane.position + (tangent * cornerOffset) + (right * -cornerOffset);
            Vector3 tr = debugPlane.position + (tangent * cornerOffset) + (right * cornerOffset);
            Vector3 br = debugPlane.position + (tangent * -cornerOffset) + (right * cornerOffset);
            Vector3 bl = debugPlane.position + (tangent * -cornerOffset) + (right * -cornerOffset);

            Color color = planeIndex % 2 == 0 ? Color.magenta : Color.green;

            Debug.DrawLine(tl, tr, color, 0, true);
            Debug.DrawLine(tr, br, color, 0, true);
            Debug.DrawLine(br, bl, color, 0, true);
            Debug.DrawLine(bl, tl, color, 0, true);

            //DebugExtension.DrawArrow(debugPlane.position, tangent, Color.cyan);
            //DebugExtension.DrawArrow(debugPlane.position, right, Color.yellow);
            //DebugExtension.DrawArrow(debugPlane.position, right, Color.red);
            DebugExtension.DrawArrow(debugPlane.position, debugPlane.normal, color);
        }
    }

    public static void SetFaceColors(SceneObjectRef sceneObj)
    {
        MeshData meshData = sceneObj.GetMeshData();
        if (meshData == null)
            return;

        Mesh mesh = sceneObj.GetMesh();
        if (mesh == null)
            return;

        Color[] meshColors = new Color[mesh.vertexCount];
        for (int faceIndex = 0; faceIndex < meshData.faces.Length; ++faceIndex)
        {
            Color faceColor = RandomUtil.RandomColor();
            for (int faceVert = 0; faceVert < meshData.faces[faceIndex].vertIndexes.Length; ++faceVert)
            {
                int vertIndex = meshData.faces[faceIndex].vertIndexes[faceVert];
                meshColors[vertIndex] = faceColor;
            }
        }

        mesh.colors = meshColors;
    }
}

public class Vector3ApproxComparer : EqualityComparer<Vector3>
{
    const float multiplier = 10 * 1;

    public override bool Equals(Vector3 x, Vector3 y)
    {
        return ApproxEqual(x, y);
    }

    public override int GetHashCode(Vector3 v) => RoundedVector(v).GetHashCode();

    public static bool ApproxEqual(Vector3 x, Vector3 y)
    {
        return (RoundedVector(x).Equals(RoundedVector(y)));
    }

    public static Vector3 RoundedVector(Vector3 v)
    {  
        v.x = Mathf.Round(v.x * multiplier) / multiplier;
        v.y = Mathf.Round(v.y * multiplier) / multiplier;
        v.z = Mathf.Round(v.z * multiplier) / multiplier;

        return v;
    }
}

public static class PlaneUtils
{
    public static bool TriangleIntersection(this Plane plane, Vector3 a, Vector3 b, Vector3 c, out Vector3 hitA, out Vector3 hitB)
    {
        float aDist = plane.GetDistanceToPoint(a);
        float bDist = plane.GetDistanceToPoint(b);
        float cDist = plane.GetDistanceToPoint(c);

        hitA = Vector3.zero;
        hitB = Vector3.zero;

        int intersection = 0;
        if (Mathf.Abs(aDist) < Vector3.kEpsilon)
        {
            hitA = a;
            ++intersection;
        }

        if (Mathf.Abs(bDist) < Vector3.kEpsilon)
        {
            if (intersection > 0)
            {
                hitB = b;
                return true;
            }

            hitA = b;
            ++intersection;
        }

        if (Mathf.Abs(cDist) < Vector3.kEpsilon)
        {
            if (intersection > 0)
            {
                hitB = c;
                return true;
            }
            hitA = c;
            ++intersection;
        }

        if (aDist * bDist <= Vector3.kEpsilon)
        {
            float lineDistance = GetLineDistance(aDist, bDist);

            if (intersection > 0)
            {
                GetPoint(a, b, lineDistance, out hitB);
                return true;
            }

            GetPoint(a, b, lineDistance, out hitA);
            ++intersection;
        }

        if (aDist * cDist <= Vector3.kEpsilon)
        {
            float lineDistance = GetLineDistance(aDist, cDist);

            if (intersection > 0)
            {
                GetPoint(a, c, lineDistance, out hitB);
                return true;
            }

            GetPoint(a, c, lineDistance, out hitA);
            ++intersection;
        }

        // We dont bother checking for a single intersection
         if(intersection < 1)
            return false;

        if (bDist * cDist <= Vector3.kEpsilon)
        {
            float lineDistance = GetLineDistance(bDist, cDist);

            if (intersection > 0)
            {
                GetPoint(b, c, lineDistance, out hitB);
                return true;
            }

            GetPoint(b, c, lineDistance, out hitA);
            ++intersection;
        }

        if (intersection > 1)
            return true;

        return false;
    }
    
    static float GetLineDistance(float aDist, float bDist)
    {
        return aDist / (aDist - bDist);
    }

    static void GetPoint(Vector3 a, Vector3 b, float distance, out Vector3 point)
    {
        point = a + distance * (b - a);
    }
}