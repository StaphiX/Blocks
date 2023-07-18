using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IModifier
{
    void Apply(Transform objTransform);
}

public class MirrorModifier : IModifier
{
    Plane plane;
    GameObject mirrorObject = null;

    public void Setup(Plane plane)
    {
        this.plane = plane;
    }

    public void Apply(Transform objTransform)
    {
        float planeDistance = plane.GetDistanceToPoint(objTransform.position);
        Vector3 mirrorTranslation = plane.normal * -planeDistance * 2;
        Vector3 mirrorPosition = objTransform.position + mirrorTranslation;

        if (mirrorObject == null)
        {
            mirrorObject = GameObject.Instantiate(objTransform.gameObject, mirrorPosition, Quaternion.identity, objTransform.parent);
            GameObject.Destroy(mirrorObject.GetComponent<SceneObjectRef>());

            mirrorObject.name = objTransform.name + "_flipX";

            MeshFilter instanceMeshFilter = mirrorObject.GetComponent<MeshFilter>();
            if (instanceMeshFilter == null)
                instanceMeshFilter = mirrorObject.GetComponentInChildren<MeshFilter>();

            Mesh mirrorMesh = GeneratedMeshUtil.Clone(instanceMeshFilter.sharedMesh);
            mirrorMesh.name = instanceMeshFilter.sharedMesh.name + "_flipX";

            GeneratedMeshUtil.FlipMeshX(ref mirrorMesh);
            instanceMeshFilter.mesh = mirrorMesh;
        }

        mirrorObject.transform.position = mirrorPosition;
        mirrorObject.transform.rotation = ReflectRotation(objTransform.rotation, plane);
    }

    private Quaternion ReflectRotation(Quaternion baseRotation, Plane plane)
    {
        return Quaternion.LookRotation(Vector3.Reflect(baseRotation * Vector3.forward, plane.normal), Vector3.Reflect(baseRotation * Vector3.up, plane.normal));
    }
}
