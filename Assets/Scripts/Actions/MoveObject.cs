using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionMoveObject : IAction
{
    private GameObject sceneObject = null;
    private Vector3 moveVector = Vector3.zero;

    public ActionMoveObject(GameObject sceneObject, Vector3 moveTo)
    {
        if (sceneObject == null)
            return;

        this.sceneObject = sceneObject;
        moveVector = moveTo - this.sceneObject.transform.position;
    }

    public void Run()
    {
        if (sceneObject == null)
            return;

        sceneObject.transform.position += moveVector;
    }

    public void Undo()
    {
        if (sceneObject == null)
            return;

        sceneObject.transform.position -= moveVector;
    }
}

public class MoveObject
{
    public static Vector3? ScreenToWorldPoint()
    {
        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        bool hasHit = groundPlane.Raycast(cameraRay, out float enter);
        if (hasHit)
        {
            Vector3 hit = cameraRay.GetPoint(enter);
            return hit;
        }

        return null;
    }

    Vector3? GetNearestPoint(GameObject sceneObject, GameObject selectedObject)
    {
        MeshFilter meshFilter = sceneObject.GetComponent<MeshFilter>();

        if (meshFilter == null)
            return null;

        Vector3 selectedPosition = selectedObject.transform.position;
        Vector3 sceneObjPosition = sceneObject.transform.position;

        Vector3 translationDirection = selectedPosition - sceneObjPosition;
        Vector3 inverseDirection = sceneObjPosition - selectedPosition;

        Mesh mesh = meshFilter.mesh;
        Vector3[] normals = mesh.normals;
        Vector3[] verts = mesh.vertices;
        int normalCount = normals.Length;
        int[] triangles = mesh.triangles;

        Vector3? nearestVert = null;
        float nearestSqrDist = 0;

        float angleThreshold = 5;
        for(int normIndex = 0; normIndex < normalCount; ++normIndex)
        {
            float angle = Vector3.Angle(inverseDirection, normals[normIndex]);
            if(angle < angleThreshold)
            {
                Vector3 vert = verts[normIndex];
                float sqrDistance = Vector3.SqrMagnitude(selectedPosition - vert);
                if(nearestVert == null || sqrDistance < nearestSqrDist)
                {
                    nearestVert = vert;
                    nearestSqrDist = sqrDistance;
                }
            }
        }

        return nearestVert;
    }

    public Vector3 ProjectPointOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
    {
        float distance;
        Vector3 translationVector;

        //First calculate the distance from the point to the plane:
        distance = SignedDistancePlanePoint(planeNormal, planePoint, point);

        //Reverse the sign of the distance
        distance *= -1;

        //Get a translation vector
        translationVector = SetVectorLength(planeNormal, distance);

        //Translate the point to form a projection
        return point + translationVector;
    }



    //Get the shortest distance between a point and a plane. The output is signed so it holds information
    //as to which side of the plane normal the point is.
    public float SignedDistancePlanePoint(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
    {
        return Vector3.Dot(planeNormal, (point - planePoint));
    }

    //create a vector of direction "vector" with length "size"
    public static Vector3 SetVectorLength(Vector3 vector, float size)
    {

        //normalize the vector
        Vector3 vectorNormalized = Vector3.Normalize(vector);

        //scale the vector
        return vectorNormalized *= size;
    }
}
