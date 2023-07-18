using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneObjectList
{
    SortedList<ShortHash, SceneObject> sceneObjectList = new SortedList<ShortHash, SceneObject>();

    public bool ContainsKey(ShortHash id)
    {
        return sceneObjectList.ContainsKey(id);
    }

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

public class ModifierList
{
    SortedList<ShortHash, List<IModifier>> objectModifiers = new SortedList<ShortHash, List<IModifier>>();

    public void AddModifier(ShortHash sceneObjectId, IModifier modifier)
    {
        List<IModifier> modifierList;
        if (objectModifiers.TryGetValue(sceneObjectId, out modifierList))
        {
            if (modifierList.Contains(modifier))
                return;

            modifierList.Add(modifier);
            return;
        }

        modifierList = new List<IModifier>();
        modifierList.Add(modifier);
        objectModifiers.Add(sceneObjectId, modifierList);
    }

    public void UpdateModifiers(ShortHash sceneObjectId, Transform sceneObjectTransform)
    {
        if(objectModifiers.ContainsKey(sceneObjectId))
        {
            List<IModifier> modifierList = objectModifiers[sceneObjectId];
            if (modifierList == null)
                return;

            for(int modifierIndex = 0; modifierIndex < modifierList.Count; ++modifierIndex)
            {
                IModifier modifier = modifierList[modifierIndex];
                
                modifier.Apply(sceneObjectTransform);
            }
        }
    }
}

public class SceneManager
{
    BoundsGrid boundsGrid = new BoundsGrid();
    SceneObjectList sceneObjectList = new SceneObjectList();
    SortedList<ShortHash, GameObject> gameObjectRefList = new SortedList<ShortHash, GameObject>();
    ModifierList modiferList = new ModifierList();
    Queue<ShortHash> updateQueue = new Queue<ShortHash>();

    public BoundsGrid GetBoundsGrid()
    {
        return boundsGrid;
    }

    public void Update()
    {
        ProcessUpdateQueue();
    }

    public void ProcessUpdateQueue()
    {
        while (updateQueue.Count > 0)
        {
            ShortHash sceneObjectId = updateQueue.Dequeue();
            if(gameObjectRefList.TryGetValue(sceneObjectId, out GameObject gameObject))
            {
                modiferList.UpdateModifiers(sceneObjectId, gameObject.transform);
            }
        }
    }

    public void AddObject(GameObject gameObject)
    {
        SceneObjectRef sceneObjectRef = RegisterSceneObject(gameObject);

        BoundsGrid boundsGrid = GetBoundsGrid();
        boundsGrid.AddObject(sceneObjectRef);
    }

    public ShortHash GetSceneID(GameObject gameObject)
    {
       return CalculateHash(gameObject.GetHashCode().ToString());
    }

    public ShortHash CalculateHash(string hashString)
    {
        ShortHash hash = ShortHash.CalculateHash(hashString);
        int loopCount = 0;
        const int maxLoopCount = 500;
        while (HashExists(hash) && maxLoopCount < 500)
        {
            hash += 1;
            ++loopCount;
        }

        if (loopCount >= maxLoopCount)
        {
            throw new Exception(String.Format("Failed to generate new hash after {0} attempts", maxLoopCount));
        }

        return hash;
    }

    public bool HashExists(ShortHash hash)
    {
        if (sceneObjectList.ContainsKey(hash))
            return true;

        return false;
    }

    private SceneObjectRef RegisterSceneObject(GameObject gameObject)
    {
        SceneObjectRef sceneObjectRef = gameObject.GetComponent<SceneObjectRef>();

        if(sceneObjectRef == null)
        {
            sceneObjectRef = gameObject.AddComponent<SceneObjectRef>();
        }

        if (sceneObjectRef.sceneId == null)
            sceneObjectRef.sceneId = GetSceneID(gameObject);


        ShortHash sceneId = sceneObjectRef.sceneId;
        if (gameObjectRefList.ContainsKey(sceneId))
            return sceneObjectRef;

        gameObjectRefList.Add(sceneId, gameObject);
        SetSceneObjectDirty(sceneId);

        return sceneObjectRef;
    }

    public void AddModifier(IModifier modifier, SceneObjectRef sceneObjectRef)
    {
        modiferList.AddModifier(sceneObjectRef.sceneId, modifier);
        SetSceneObjectDirty(sceneObjectRef.sceneId);
    }

    public void SetSceneObjectDirty(ShortHash sceneObjectId)
    {
        if (sceneObjectId == null)
        {
            Debug.Assert(false, "Trying to set dirty on null");
            return;
        }

        if (updateQueue.Contains(sceneObjectId))
            return;

        updateQueue.Enqueue(sceneObjectId);
    }
}
