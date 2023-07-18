using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneObjectRef : MonoBehaviour
{
    public ShortHash sceneId { get; set; } = null;
    ShortHash meshId = null;
    MeshFilter meshFilter = null;
    MeshRenderer meshRenderer = null;
    MeshData meshData = null;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if(meshFilter != null)
        {
            meshId = Main.WorldData.AddMesh(meshFilter.mesh);

            if(meshId != null)
                meshData = Main.WorldData.GetMeshData(meshId); ;
        }

        SceneObjDebug.SetFaceColors(this);
    }

    void Start()
    {
        Main.SceneManager.AddObject(gameObject);
    }

    void Update()
    {
        
    }

    void LateUpdate()
    {
        if (transform.hasChanged)
        {
            Main.SceneManager.SetSceneObjectDirty(sceneId);

            transform.hasChanged = false;
        }
    }

    public ShortHash GetMeshId()
    {
        return meshId;
    }

    public Bounds? GetBounds()
    {
        if (meshRenderer == null)
            return null;

        return meshRenderer.bounds;
    }

    public MeshData GetMeshData()
    {
        return meshData;
    }

    public Mesh GetMesh()
    {
        if (meshFilter == null)
            return null;

        return meshFilter.mesh;
    }

    public bool GetTrianglePositions(int triangleIndex, ref Vector3[] positions)
    {
        if (meshData == null)
            return false;

        return meshData.GetTrianglePositions(triangleIndex, transform, ref positions);
    }

    public bool GetVertPosition(int vertIndex, ref Vector3 position)
    {
        if (meshData == null)
            return false;

        return meshData.GetVertPosition(vertIndex, transform, ref position);
    }

    public bool GetNormal(int vertIndex, ref Vector3 normal)
    {
        if (meshData == null)
            return false;

        return meshData.GetNormal(vertIndex, ref normal);
    }

    public Vector3 TransformPosition(Vector3 position)
    {
        return transform.TransformPoint(position);
    }

    public bool TransformPosition(Vector3 position, ref Vector3 transformedPosition)
    {
        transformedPosition = transform.TransformPoint(position);

        return true;
    }
}
