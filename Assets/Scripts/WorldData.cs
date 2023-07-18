using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GeneratedObjectPool
{
    SortedList<ShortHash, List<ShortHash>> generatedObjectReference;
    SortedList<ShortHash, SceneObject> generatedObjects;
    SortedList<ShortHash, SceneObject> sceneObjList;

    public bool TryGetGeneratedObjectList(ShortHash key, out List<ShortHash> objList)
    {
        return generatedObjectReference.TryGetValue(key, out objList);
    }

    //public SceneObject DuplicateSceneObject(SceneObject sceneObject)
    //{
    //    SceneObject duplicate = sceneObject;
    //    sceneObject.MeshId;
    //    duplicate.MeshId = 0;

    //    if(TryGetGeneratedObjectList())

    //    AddSceneObject(duplicate);

    //    return duplicate;
    //}
}

public class SceneObjectPool
{
    SortedList<ShortHash, SceneObject> sceneObjectList = new SortedList<ShortHash, SceneObject>();

    public void AddSceneObject(SceneObject sceneObject)
    {
        //ShortHash sceneObject.hash;
    }

    public SceneObject Duplicate(SceneObject sceneObject)
    {
        SceneObject duplicate = sceneObject;
        duplicate.MeshId = 0;

        AddSceneObject(duplicate);

        return duplicate;
    }
}

public class WorldData
{
    public SceneObjectPool SceneObjects { get; set; } = new SceneObjectPool();
    public GeneratedObjectPool GeneratedObjects { get; set; } = new GeneratedObjectPool();
    SortedList<string, ShortHash> meshLookup = new SortedList<string, ShortHash>();
    SortedList<ShortHash, MeshData> meshList = new SortedList<ShortHash, MeshData>();
    SortedList<ShortHash, IModifier> modifierList = new SortedList<ShortHash, IModifier>();

    public ShortHash CalculateHash(string hashString)
    {
        ShortHash hash = ShortHash.CalculateHash(hashString);
        int loopCount = 0;
        const int maxLoopCount = 500;
        while(HashExists(hash) && maxLoopCount < 500)
        {
            hash += 1;
            ++loopCount;
        }

        if(loopCount >= maxLoopCount)
        {
            throw new Exception(String.Format("Failed to generate new hash after {0} attempts", maxLoopCount));
        }

        return hash;
    }

    public bool HashExists(ShortHash hash)
    {
        if (meshList.ContainsKey(hash))
            return true;

        if (modifierList.ContainsKey(hash))
            return true;

        return false;
    }

    public ShortHash AddMesh(Mesh mesh)
    {
        if (mesh == null)
            return null;

        string name = mesh.name;

        if (name == null)
            return null;
        
        if (meshLookup.ContainsKey(name))
        {
            ShortHash lookupMeshId = meshLookup[name];
            if (meshList.ContainsKey(lookupMeshId))
                return lookupMeshId;
        }

        ShortHash meshId = meshLookup.ContainsKey(name) ? meshLookup[name] : CalculateHash(name);
        MeshData meshData = new MeshData(mesh);

        meshLookup.Add(name, meshId);
        meshList.Add(meshId, meshData);
        return meshId;
    }

    public MeshData LoadMesh(string path, string name)
    {
        if(meshLookup.ContainsKey(name))
        {
            ShortHash lookupMeshId = meshLookup[name];
            if (meshList.ContainsKey(lookupMeshId))
                return meshList[lookupMeshId];
        }

        Mesh mesh = Resources.Load(path) as Mesh;

        if(mesh == null)
        {
            Debug.LogAssertion(String.Format("Failed to load mesh: {0}", path));
            return null;
        }

        ShortHash meshId = AddMesh(mesh);

        return meshList[meshId];
    }

    public MeshData GetMeshData(ShortHash meshId)
    {
        if (meshId == null)
            return null;

        if (!meshList.ContainsKey(meshId))
            return null;

        return meshList[meshId];
    }

    public ShortHash AddSceneObject(SceneObjectRef sceneObj)
    {
        Mesh mesh = sceneObj.GetMesh();
        if (mesh == null)
            return null;

        return AddMesh(mesh);
    }

    public SceneObject DuplicateSceneObject(SceneObject sceneObject)
    {
        SceneObject duplicate = sceneObject;
        duplicate.MeshId = 0;

        return duplicate;
    }
}
