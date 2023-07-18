using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

public class BoundsNode
{
    public BoundsNode(Bounds bounds) { this.bounds = bounds;  }
    public Bounds bounds;
    public List<BoundsNode> children = null;
    public List<SceneObjectRef> sceneObjectList = null;

    public void AddNode(BoundsNode node)
    {
        if (children == null)
            children = new List<BoundsNode>();

        children.Add(node);
    }

    public void AddSceneObj(SceneObjectRef sceneObjRef)
    {
        if (sceneObjectList == null)
            sceneObjectList = new List<SceneObjectRef>();

        if (sceneObjectList.Contains(sceneObjRef))
            return;

        sceneObjectList.Add(sceneObjRef);
    }

    public void SetSceneObjList(List<SceneObjectRef> sceneObjList)
    {
        sceneObjectList = sceneObjList;
    }

    // Splits this into two bounds and moves children to the appropriate list
    // Not a BVH but could be if that proves useful
    public void Split()
    {
        // Only split if does not already have child bounds
        if (children != null && children.Count > 0)
            return;

        if(sceneObjectList == null || sceneObjectList.Count < 1)
            return;

        if (bounds.min == bounds.max)
            return;

        float largestAxis = bounds.size.x;
        int splitAxis = 0;
        for(int axis = 1; axis < 3; ++axis)
        {
            if (bounds.size[axis] > largestAxis)
                splitAxis = axis;
        }

        Vector3 boundsCenterA = bounds.center;
        Vector3 boundsCenterB = bounds.center;
        Vector3 boundsSize = bounds.size;
        boundsSize[splitAxis] *= 0.5f;
        boundsCenterA[splitAxis] -= boundsSize[splitAxis] * 0.5f;
        boundsCenterB[splitAxis] += boundsSize[splitAxis] * 0.5f;

        Bounds boundsA = new Bounds(boundsCenterA, boundsSize);
        Bounds boundsB = new Bounds(boundsCenterB, boundsSize);

        List<SceneObjectRef> objListA = new List<SceneObjectRef>();
        List<SceneObjectRef> objListB = new List<SceneObjectRef>();

        for (int meshDataIndex = 0; meshDataIndex < sceneObjectList.Count; ++meshDataIndex)
        {
            SceneObjectRef sceneObj = sceneObjectList[meshDataIndex];
            Bounds? objBounds = sceneObj.GetBounds();

            if (objBounds == null)
                continue;

            if (objBounds.Value.Intersects(boundsA))
            {
                objListA.Add(sceneObj);
            }

            if (objBounds.Value.Intersects(boundsB))
            {
                objListB.Add(sceneObj);
            }

            // Has the split improved out bounds checking
            if (objListA.Count >= sceneObjectList.Count || objListB.Count >= sceneObjectList.Count)
                return;

            BoundsNode boundsNodeA = new BoundsNode(boundsA);
            boundsNodeA.SetSceneObjList(objListA);

            BoundsNode boundsNodeB = new BoundsNode(boundsB);
            boundsNodeB.SetSceneObjList(objListB);

            AddNode(boundsNodeA);
            AddNode(boundsNodeB);

            sceneObjectList.Clear();
            sceneObjectList = null;
        }
    }
}

public class BoundsGrid
{
    Vector3 maxCellSize = new Vector3(10, 5, 10);
    Bounds worldBounds = new Bounds();

    Dictionary<Vector3Int, BoundsNode> activeCells = new Dictionary<Vector3Int, BoundsNode>();
    RaycastResult raycastCache = new RaycastResult();

    public BoundsNode AddCell(Vector3Int cell)
    {
        if (activeCells.ContainsKey(cell))
            return activeCells[cell];

        Bounds cellBounds = GetCellBounds(cell);
        BoundsNode newBoundsNode = new BoundsNode(cellBounds);
        activeCells.Add(cell, newBoundsNode);
        worldBounds.Encapsulate(cellBounds);

        DebugExtension.DebugBounds(cellBounds, Color.red, 100, true);

        return newBoundsNode;
    }

    public void AddObject(SceneObjectRef sceneObjRef)
    {
        if (sceneObjRef == null)
            return;

        Bounds? objectBounds = sceneObjRef.GetBounds();

        if (objectBounds == null)
            return;

        List<Vector3Int> cellIndexes = GetCellsFromBounds(objectBounds.Value);

        for(int listIndex = 0; listIndex < cellIndexes.Count; ++listIndex)
        {
            Vector3Int cellIndex = cellIndexes[listIndex];
            BoundsNode cell = AddCell(cellIndex);

            // Add the object bounds to the cell bounds tree
            cell.AddSceneObj(sceneObjRef);
        }
    }

    private List<Vector3Int> GetCellsFromBounds(Bounds bounds)
    {
        Vector3Int minCell = GetCellIndex(bounds.min);
        Vector3Int maxCell = GetCellIndex(bounds.max);

        int xCells = minCell.x < maxCell.x ? 2 : 1;
        int yCells = minCell.y < maxCell.y ? 2 : 1;
        int zCells = minCell.z < maxCell.z ? 2 : 1;
        List<Vector3Int> cellIndexes = new List<Vector3Int>();

        for (int x = 0; x < xCells; ++x)
        {
            for (int y = 0; y < yCells; ++y)
            {
                for (int z = 0; z < zCells; ++z)
                {
                    int cellX = x == 0 ? minCell.x : maxCell.x;
                    int cellY = y == 0 ? minCell.y : maxCell.y;
                    int cellZ = z == 0 ? minCell.z : maxCell.z;
                    cellIndexes.Add(new Vector3Int(cellX, cellY, cellZ));
                }
            }
        }

        return cellIndexes;
    }

    public void AddCellFromPoint(Vector3 point)
    {
        Vector3Int cellIndex = GetCellIndex(point);
        if (activeCells.ContainsKey(cellIndex))
            return;

        AddCell(cellIndex);
    }

    public void AddCellsFromBounds(Bounds bounds)
    {
        List<Vector3Int> cellList = GetCellsFromBounds(bounds);

        for (int listIndex = 0; listIndex < cellList.Count; ++listIndex)
        {
            Vector3Int cellIndex = cellList[listIndex];
            if (activeCells.ContainsKey(cellIndex))
                continue;

            AddCell(cellIndex);
        }
    }

    public bool RaycastFindFace(Ray ray, out SceneObjectRef nearestObj, out FaceData nearestFace, ref Vector3[] vertPositions, ref int triangleIndex, out Vector3 intersection)
    {
        nearestFace = null;
        nearestObj = null;
        intersection = Vector3.zero;

        Vector3 rayInvDir = ray.GetInvDir();
        List<BoundsNode> boundsCells = RaycastCells(ray);

        if (boundsCells == null || boundsCells.Count < 1)
            return false;

        List<FaceIntersectionData> faceIntersectionList = new List<FaceIntersectionData>();
        HashSet<MeshData> objectsFound = new HashSet<MeshData>();

        for(int cellIndex = 0; cellIndex < boundsCells.Count; ++cellIndex)
        {
            BoundsNode boundsNode = boundsCells[cellIndex];
            RaycastBoundsNode(ray, rayInvDir, boundsNode, ref objectsFound, ref faceIntersectionList);
        }

        float sqrDistToRay = 0;

        Vector3[] faceVertPositions = new Vector3[3];
        int faceTriangleIndex = 0;

        for (int objIndex = 0; objIndex < faceIntersectionList.Count; ++objIndex)
        {
            SceneObjectRef sceneObj = faceIntersectionList[objIndex].sceneObj;
            for (int faceIndex = 0; faceIndex < faceIntersectionList[objIndex].faceDataList.Count; ++faceIndex)
            {
                FaceData faceData = faceIntersectionList[objIndex].faceDataList[faceIndex];
                Vector3 faceIntersection = faceIntersectionList[objIndex].faceIntersectionList[faceIndex];

                float faceDistToRay = Vector3.SqrMagnitude(faceIntersection - ray.origin);
                bool isNearestFace = nearestFace == null || faceDistToRay < sqrDistToRay;
                if(!isNearestFace && faceDistToRay == sqrDistToRay)
                {
                    float faceSqrDist = Vector3.SqrMagnitude(sceneObj.TransformPosition(faceData.avgPosition) - ray.origin);
                    float nearFaceSqrDist = Vector3.SqrMagnitude(sceneObj.TransformPosition(nearestFace.avgPosition) - ray.origin);
                    isNearestFace = faceSqrDist < nearFaceSqrDist;
                }

                if (isNearestFace)
                {
                    if (RayUtil.RaycastFace(ray, sceneObj, faceData, ref faceVertPositions, ref faceTriangleIndex, out faceIntersection))
                    {
                        sqrDistToRay = faceDistToRay;

                        nearestObj = sceneObj;
                        nearestFace = faceData;
                        vertPositions = faceVertPositions;
                        triangleIndex = faceTriangleIndex;
                        intersection = faceIntersection;
                    }
                }
            }
        }

        if (nearestFace != null)
            return true;

        return false;
    }

    public RaycastResult RaycastAllObjects(Ray ray)
    {
        Vector3 rayInvDir = ray.GetInvDir();
        bool foundWithCache = raycastCache.Raycast(ray, rayInvDir);

        if (foundWithCache)
        {
            //Debug.Log("Raycast found with cache");
            return raycastCache;
        }

        // Invalidate the cache
        raycastCache.Reset();

        bool foundFace = RaycastFindFace(ray, out raycastCache.sceneObj, out raycastCache.faceData, ref raycastCache.vertPositions, ref raycastCache.triangleIndex, out raycastCache.intersection);

        if (!foundFace)
            return null;

        if (raycastCache.sceneObj == null || raycastCache.faceData == null)
            return null;

        // Cache should now contain valid data from the calls above
       return raycastCache;
    }

    public bool RaycastFindTriangle(Ray ray, Vector3 rayInvDir, SceneObjectRef sceneObj, FaceData faceData, ref Vector3[] vertPositions, ref int triangleIndex, ref Vector3 intersection)
    {
        return RayUtil.RaycastFace(ray, sceneObj, faceData, ref vertPositions, ref triangleIndex, out intersection);
    }

    void RaycastBoundsNode(Ray ray, Vector3 rayInvDir, BoundsNode node, ref HashSet<MeshData> objectsFound, ref List<FaceIntersectionData> faceIntersectionList)
    {
        if (node.children == null && node.sceneObjectList == null)
            return;

        if (faceIntersectionList == null)
            faceIntersectionList = new List<FaceIntersectionData>();

        if(node.children != null && node.children.Count > 0)
        {
            for(int childIndex = 0; childIndex < node.children.Count; ++childIndex)
            {
                RaycastBoundsNode(ray, rayInvDir, node.children[childIndex], ref objectsFound, ref faceIntersectionList);
            }
        }

        if(node.sceneObjectList != null && node.sceneObjectList.Count > 0)
        {
            for (int objIndex = 0; objIndex < node.sceneObjectList.Count; ++objIndex)
            {
                SceneObjectRef sceneObj = node.sceneObjectList[objIndex];
                Bounds? objBounds = sceneObj.GetBounds();
                if (objBounds == null)
                    continue;

                if(objBounds.Value.FastRayIntersection(ray, rayInvDir, out Vector3 enter))
                {
                    RaycastFaces(ray, rayInvDir, sceneObj, ref objectsFound, ref faceIntersectionList);
                }
            }
        }

        return;
    }

    void RaycastFaces(Ray ray, Vector3 rayInvDir, SceneObjectRef sceneObj, ref HashSet<MeshData> objectsFound, ref List<FaceIntersectionData> faceIntersectionList)
    {
        MeshData meshData = sceneObj.GetMeshData();

        if (meshData == null || meshData.faces == null || meshData.faces.Length < 1)
            return;

        if (objectsFound == null)
            objectsFound = new HashSet<MeshData>();

        if (objectsFound.Contains(meshData))
            return;

        if (faceIntersectionList == null)
            faceIntersectionList = new List<FaceIntersectionData>();

        FaceIntersectionData faceIntersectionData = null;

        for (int faceIndex = 0; faceIndex < meshData.faces.Length; ++faceIndex)
        {
            // todo - Update facedata bounds when we transform the object
            // Face data needs to be decoupled from bounds
            // Faces are the same for any mesh but bounds will change with rotations / transforms

            FaceData meshFace = meshData.faces[faceIndex];
            Vector3 faceMin = meshFace.bounds.min;
            Vector3 faceMax = meshFace.bounds.max;
            Bounds bounds = meshFace.bounds.Transform(sceneObj.transform.localToWorldMatrix);

            bool foundIntersection = BoundsUtil.FastRayIntersection(bounds.min, bounds.max, ray, rayInvDir, out Vector3 enter);

            if(foundIntersection)
            {
                if (faceIntersectionData == null)
                    faceIntersectionData = new FaceIntersectionData(sceneObj);

                faceIntersectionData.Add(meshFace, enter);
                objectsFound.Add(meshData);

                //DebugExtension.DebugPoint(enter, Color.cyan, 0.1f, 20.0f, true);
            }
        }

        if (faceIntersectionData != null)
            faceIntersectionList.Add(faceIntersectionData);
    }

    public List<BoundsNode> RaycastCells(Ray ray)
    {
        if (worldBounds.min == worldBounds.max)
            return null;

        Vector3 rayInvDir = ray.GetInvDir();
        bool worldIntersection = BoundsUtil.FastRayIntersection(worldBounds.min, worldBounds.max, ray, rayInvDir, out Vector3 enter);

        if (!worldIntersection)
            return null;

        const int maxBoundsCheck = 10;
        int boundsCheck = 0;
        // We add an offset to the plane intersection points to get inside the next bounding box
        // This has the potential to skip a box if the ray hits a small corner of a box
        Vector3 offset = ray.direction * 0.01f;
        Vector3 insideBounds = enter + offset;
        bool inWorld = worldBounds.Contains(insideBounds);

        List<Vector3Int> foundCellIndex = new List<Vector3Int>();
        List<BoundsNode> foundCells = new List<BoundsNode>();

        while (boundsCheck < maxBoundsCheck)
        {
            Vector3Int cellIndex = GetCellIndex(insideBounds);
            Vector3 center = GetCellCenter(cellIndex);
            inWorld = worldBounds.Contains(center);

            if (!inWorld)
                break;

            if (activeCells.ContainsKey(cellIndex) && !foundCellIndex.Contains(cellIndex))
            {
                //DebugExtension.DebugPoint(enter, Color.green, 0.1f, 20, true);
                //DebugExtension.DebugArrow(enter, ray.direction, Color.green, 20, true);

                foundCellIndex.Add(cellIndex);
                foundCells.Add(activeCells[cellIndex]);
            }
            else
            {
                //DebugExtension.DebugPoint(enter, Color.gray, 0.1f, 20, true);
                //DebugExtension.DebugArrow(enter, ray.direction, Color.gray, 20, true);
            }

            ray.origin = insideBounds;
            Vector3 min = GetCellMin(cellIndex);
            Vector3 max = GetCellMax(cellIndex);

            bool intersection = BoundsUtil.FastRayIntersection(min, max, ray, rayInvDir, out enter);

            if (!intersection)
                break;

            insideBounds = enter + offset;

            ++boundsCheck;
        }

        if (foundCells.Count < 1)
            return null;

        return foundCells;
    }

    Bounds GetCellBounds(Vector3Int cellIndex)
    {
        return new Bounds(GetCellCenter(cellIndex), maxCellSize);
    }

    Vector3Int GetCellIndex(Vector3 point)
    {
        int cellX = Mathf.FloorToInt(point.x / maxCellSize.x);
        int cellY = Mathf.FloorToInt(point.y / maxCellSize.y);
        int cellZ = Mathf.FloorToInt(point.z / maxCellSize.z);

        return new Vector3Int(cellX, cellY, cellZ);
    }

    Vector3 GetCellMin(Vector3Int cellIndex)
    {
        return new Vector3(cellIndex.x * maxCellSize.x,
            cellIndex.y * maxCellSize.y,
            cellIndex.z * maxCellSize.z);
    }

    Vector3 GetCellMax(Vector3Int cellIndex)
    {
        Vector3 cellMin = GetCellMin(cellIndex);
        cellMin += maxCellSize;
        return cellMin;
    }

    Vector3 GetCellCenter(Vector3Int cellIndex)
    {
        Vector3 cellMin = GetCellMin(cellIndex);
        cellMin += maxCellSize / 2;
        return cellMin;
    }
}

public class FaceIntersectionData
{
    public FaceIntersectionData(SceneObjectRef sceneObj) { this.sceneObj = sceneObj; }
    public void Add(FaceData faceData, Vector3 faceIntersection)
    {
        faceDataList.Add(faceData);
        faceIntersectionList.Add(faceIntersection);
    }

    public SceneObjectRef sceneObj = null;
    public List<FaceData> faceDataList = new List<FaceData>();
    public List<Vector3> faceIntersectionList = new List<Vector3>();
}

public class RaycastResult
{
    public SceneObjectRef sceneObj = null;
    public FaceData faceData = null;
    public int triangleIndex = 0;

    public Vector3[] vertPositions = new Vector3[3];
    public Vector3 intersection = new Vector3();

    public void Reset()
    {
        sceneObj = null;
        faceData = null;
    }

    public bool Raycast(Ray ray, Vector3 rayInvDir)
    {
        bool foundTriangle = RaycastCache(ray, rayInvDir);

        return foundTriangle;
    }

    bool RaycastCache(Ray ray, Vector3 rayInvDir)
    {
        intersection = Vector3.zero;

        if (sceneObj == null || faceData == null)
            return false;

        Bounds? bounds = sceneObj.GetBounds();
        if (bounds == null)
            return false;

        bool inBounds = bounds.Value.FastRayIntersection(ray, rayInvDir, out intersection);

        if (!inBounds)
            return false;

        if (!sceneObj.GetTrianglePositions(triangleIndex, ref vertPositions))
            return false;

        if (RayUtil.RaycastTriangle(ray, vertPositions, out intersection))
            return true;

        if (faceData.triangleAdjacency.Count < 1 || !faceData.triangleAdjacency.ContainsKey(triangleIndex))
            return false;

        if (faceData.triangleAdjacency[triangleIndex].Length < 1)
            return false;

        for(int adjacentIndex = 0; adjacentIndex < faceData.triangleAdjacency[triangleIndex].Length; ++adjacentIndex)
        {
            int adjacentTri = faceData.triangleAdjacency[triangleIndex][adjacentIndex];

            if (!sceneObj.GetTrianglePositions(adjacentTri, ref vertPositions))
                continue;

            if (RayUtil.RaycastTriangle(ray, vertPositions, out intersection))
            {
                triangleIndex = adjacentTri;
                return true;
            }
        }

        return false;
    }
}

public static class BoundsUtil
{
    public static float DistanceToExtent(this Bounds bounds, Vector3 point)
    {
        return DistanceToExtent(bounds.min, bounds.max, point);
    }

    public static float DistanceToExtent(Vector3 min, Vector3 max, Vector3 point)
    {
        float xDist = Mathf.Min(point.x - min.x, max.x - point.x);
        float yDist = Mathf.Min(point.y - min.y, max.y - point.y);
        float zDist = Mathf.Min(point.z - min.z, max.z - point.z);

        float distance = Mathf.Sqrt(xDist * xDist + yDist * yDist + zDist * zDist);

        return distance;
    }

    public static bool FastRayIntersection(this Bounds bounds, Ray ray, Vector3 rayInvDir, out Vector3 enter)
    {
        return FastRayIntersection(bounds.min, bounds.max, ray, rayInvDir, out enter);
    }

    public static bool FastRayIntersection(Vector3 min, Vector3 max, Ray ray, Vector3 rayInvDir, out Vector3 enter)
    {
        double t1 = (min[0] - ray.origin[0]) * rayInvDir[0];
        double t2 = (max[0] - ray.origin[0]) * rayInvDir[0];

        double tmin = MathUtil.Min(t1, t2);
        double tmax = MathUtil.Max(t1, t2);

        for (int i = 1; i < 3; ++i)
        {
            t1 = (min[i] - ray.origin[i]) * rayInvDir[i];
            t2 = (max[i] - ray.origin[i]) * rayInvDir[i];

            tmin = MathUtil.Min(MathUtil.Max(t1, tmin), MathUtil.Max(t2, tmin));
            tmax = MathUtil.Max(MathUtil.Min(t1, tmax), MathUtil.Min(t2, tmax));
        }

        //bool isPlane = min.x == max.x || min.y == max.y || min.z == max.z;
        bool foundIntersection = tmin <= tmax;
       
        enter = foundIntersection ? ray.GetPoint((float)(tmin < 0 ? tmax : tmin)) : Vector3.zero;

        return foundIntersection;
    }

    public static void Transform(ref Vector3 min, ref Vector3 max, Matrix4x4 transformMatrix)
    {
        Vector3 transMin = transformMatrix.GetPosition();
        Vector3 transMax = transMin;

        float minVal = 0;
        float maxVal = 0;

        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                minVal = transformMatrix[i, j] * min[j];
                maxVal = transformMatrix[i, j] * max[j];

                transMin[i] += minVal < maxVal ? minVal : maxVal;
                transMax[i] += minVal < maxVal ? maxVal : minVal;
            }
        }

        min = transMin;
        max = transMax;
    }

    public static Bounds Transform(this Bounds bounds, Matrix4x4 transformMatrix)
    {
        // We will need access to the right, up and look vector which are encoded inside the transform matrix
        Vector3 rightAxis = transformMatrix.GetColumn(0);
        Vector3 upAxis = transformMatrix.GetColumn(1);
        Vector3 lookAxis = transformMatrix.GetColumn(2);

        // We will 'imagine' that we want to rotate the bounds' extents vector using the rotation information
        // stored inside the specified transform matrix. We will need these when calculating the new size if
        // the transformed bounds.
        Vector3 rotatedExtentsRight = rightAxis * bounds.extents.x;
        Vector3 rotatedExtentsUp = upAxis * bounds.extents.y;
        Vector3 rotatedExtentsLook = lookAxis * bounds.extents.z;

        // Calculate the new bounds size along each axis. The size on each axis is calculated by summing up the 
        // corresponding vector component values of the rotated extents vectors. We multiply by 2 because we want
        // to get a size and currently we are working with extents which represent half the size.
        float newSizeX = (Mathf.Abs(rotatedExtentsRight.x) + Mathf.Abs(rotatedExtentsUp.x) + Mathf.Abs(rotatedExtentsLook.x)) * 2.0f;
        float newSizeY = (Mathf.Abs(rotatedExtentsRight.y) + Mathf.Abs(rotatedExtentsUp.y) + Mathf.Abs(rotatedExtentsLook.y)) * 2.0f;
        float newSizeZ = (Mathf.Abs(rotatedExtentsRight.z) + Mathf.Abs(rotatedExtentsUp.z) + Mathf.Abs(rotatedExtentsLook.z)) * 2.0f;

        // Construct the transformed 'Bounds' instance
        var transformedBounds = new Bounds();
        transformedBounds.center = transformMatrix.MultiplyPoint(bounds.center);
        transformedBounds.size = new Vector3(newSizeX, newSizeY, newSizeZ);

        // Return the instance to the caller
        return transformedBounds;
    }
}

public static class MathUtil
{
    public static double Abs(double x)
    {
        return (x >= 0) ? x : -x;
    }

    public static void Abs(ref Vector3 v)
    {
        v.x = Mathf.Abs(v.x);
        v.y = Mathf.Abs(v.y);
        v.z = Mathf.Abs(v.z);
    }

    public static double Min(double a, double b)
    {
        return (a < b) ? a : b;
    }

    public static double Max(double a, double b)
    {
        return (a > b) ? a : b;
    }
}

public static class RayUtil
{
    public static Vector3 GetInvDir(this Ray ray)
    {
        float x = 1.0f / ray.direction.x;
        float y = 1.0f / ray.direction.y;
        float z = 1.0f / ray.direction.z;

        return new Vector3(x, y, z);
    }

    public static bool RaycastFace(Ray ray, SceneObjectRef sceneObj, FaceData faceData, ref Vector3[] vertPositions, ref int triangleIndex, out Vector3 enter)
    {
        enter = Vector3.zero;

        if (sceneObj == null)
            return false;

        if (faceData.triangleIndexes.Length < 1)
            return false;

        for(int arrayIndex = 0; arrayIndex < faceData.triangleIndexes.Length; ++arrayIndex)
        {
            triangleIndex = faceData.triangleIndexes[arrayIndex];

            sceneObj.GetTrianglePositions(triangleIndex, ref vertPositions);

            if (RaycastTriangle(ray, vertPositions, out enter))
            {
                return true;
            }
        }

        return false;
    }

    public static bool RaycastTriangle(Ray ray, Vector3[] verts, out Vector3 enter)
    {
        return RaycastTriangle(ray, verts[0], verts[1], verts[2], out enter);
    }

    public static bool RaycastTriangle(Ray ray, Vector3 a, Vector3 b, Vector3 c, out Vector3 enter)
    {
        enter = Vector3.zero;

        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 pvec = Vector3.Cross(ray.direction, ac);
        float determinant = Vector3.Dot(ab, pvec);

        // if the determinant is negative, the triangle is 'back facing.'
        // if the determinant is close to 0, the ray misses the triangle
        if (determinant < float.Epsilon) return false;

        // ray and triangle are parallel if det is close to 0
        if (Mathf.Abs(determinant) < float.Epsilon) return false;

        float invDet = 1.0f / determinant;

        Vector3 tvec = ray.origin - a;

        float u = Vector3.Dot(tvec, pvec) * invDet;
        if (u < 0 || u > 1) return false;

        Vector3 qvec = Vector3.Cross(tvec, ab);
        float v = Vector3.Dot(ray.direction, qvec) * invDet;
        if (v < 0 || u + v > 1) return false;

        float intersection = Vector3.Dot(ac, qvec) * invDet;

        enter = ray.GetPoint(intersection);

        return true;
    }
}