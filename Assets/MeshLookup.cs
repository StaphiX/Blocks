using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum EMirror
{
    NONE,
    FLIPX,
    FLIPY,
    FLIPZ,
    FLIPXY,
    FLIPYZ
}

public class GeneratedMeshData
{
    Vector3 flip;
    Transform transform;
}

public static class GeneratedMeshUtil
{
    public static Mesh Clone(Mesh mesh)
    {
        Mesh newMesh = new Mesh()
        {
            name = mesh.name,
            vertices = mesh.vertices,
            triangles = mesh.triangles,
            normals = mesh.normals,
            tangents = mesh.tangents,
            bounds = mesh.bounds,
            uv = mesh.uv,
            uv2 = mesh.uv2,
            uv3 = mesh.uv3,
            uv4 = mesh.uv4
        };

        return newMesh;
    }

    public static void FlipMeshX(ref Mesh mesh, Vector3? nullablePivot = null)
    {
        Vector3 pivot = Vector3.zero;
        if (nullablePivot != null)
            pivot = nullablePivot.Value;

        int[] triangles = mesh.triangles;
        Vector3[] verticies = mesh.vertices;
        Array.Reverse(triangles);

        for (int vertIndex = 0; vertIndex < verticies.Length; ++vertIndex)
        {
            float xDist = pivot.x - verticies[vertIndex].x;
            verticies[vertIndex].x = pivot.x + xDist;
        }

        mesh.vertices = verticies;
        mesh.triangles = triangles;
    }

    public static void FlipMesh(Vector3 direction, Vector3 pivot, ref Mesh mesh)
    {
        Plane flipPlane = new Plane(direction, pivot);
        int[] triangles = mesh.triangles;
        Vector3[] verticies = mesh.vertices;
        Array.Reverse(triangles);

        for(int vertIndex = 0; vertIndex < verticies.Length; ++vertIndex)
        {
            Vector3 planeDist = flipPlane.ClosestPointOnPlane(verticies[vertIndex]);
            verticies[vertIndex] += planeDist*2;
        }

        mesh.vertices = verticies;
        mesh.triangles = triangles;
    }
}

public class MeshLookup
{
    SceneManager sceneManager = null;

    SortedList<string, ShortHash> meshLookup = new SortedList<string, ShortHash>();
    SortedList<ShortHash, Mesh> meshList = new SortedList<ShortHash, Mesh>();
    SortedList<ShortHash, List<Mesh>> generatedMeshLookup = new SortedList<ShortHash, List<Mesh>>();

    MeshLookup(SceneManager sceneManager)
    {
        this.sceneManager = sceneManager;
    }

    public Mesh LoadMesh(string path)
    {
        if (meshLookup.ContainsKey(path))
        {
            ShortHash lookupHash = meshLookup[path];
            if (meshList.ContainsKey(lookupHash))
                return meshList[lookupHash];
        }

        Mesh mesh = Resources.Load(path) as Mesh;

        if (mesh == null)
        {
            Debug.LogAssertion(String.Format("Failed to load mesh: {0}", path));
            return null;
        }

        ShortHash hash = meshLookup.ContainsKey(path) ? meshLookup[path] : sceneManager.CalculateHash(path);
        meshList.Add(hash, mesh);

        return mesh;
    }

    //public void AddGeneratedMesh(ShortHash meshId, GeneratedMeshData generatedMeshData, out int meshIndex)
    //{
    //    List<Mesh> generatedMeshList;
    //    if (!generatedMeshLookup.TryGetValue(meshId, out generatedMeshList))
    //        generatedMeshList = new List<Mesh>();



    //}

    //public bool UpdateGeneratedMesh(ShortHash meshId, ref int meshIndex, GeneratedMeshData generatedMeshData)
    //{
    //    if(!meshList.ContainsKey(meshId))
    //    {
    //        // LOAD MESH
    //        return false;
    //    }

    //    AddGeneratedMesh(meshId, generatedMeshData);

    //    return true;
    //}
}
