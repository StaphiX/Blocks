using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SceneObject
{
    public ShortHash MeshId { get; set; }
    public Vector3 Scale { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }

    void Default()
    {
        MeshId = 0;
        Scale = Vector3.one;
        Position = Vector3.zero;
        Rotation = Quaternion.identity;
    }
}

public class ObjectGroup
{
    List<SceneObject> objList;
    Bounds bounds;
    List<IModifier> modifierList;

    ObjectGroup parent;
    List<ObjectGroup> children;
}
