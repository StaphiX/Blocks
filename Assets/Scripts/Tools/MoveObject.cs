using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum ObjectSnapState
{
    None,
    Snap
}

public class ObjectSnap
{
    public ObjectSnapState state = ObjectSnapState.None;
    public float threshold;

    public ObjectSnap(ObjectSnapState state, float threshold)
    {
        this.state = state;
        this.threshold = threshold;
    }
}

public class ToolMoveObject : Tool
{
    ObjectSnap objectSnap = new ObjectSnap(ObjectSnapState.Snap, 1.0f);

    bool hasPressedDown = false;
    GameObject selectedObject = null;
    public ToolMoveObject(GameObject gameObject)
    {
        selectedObject = gameObject;
    }

    public override void Update()
    {
        if (selectedObject == null)
            return;

        if(Input.GetMouseButtonDown(0))
        {
            hasPressedDown = true;
        }

        //if(hasPressedDown && Input.GetMouseButtonUp(0))
        //{
        //    selectedObject = null;
        //    Finish();
        //    return;
        //}

        Vector3? newPosition = MouseToWorldPoint();

        if (newPosition == null)
            return;

        newPosition = GetSnappedPosition(newPosition.Value);
        newPosition = OffsetPosition(newPosition.Value);

        selectedObject.transform.position = newPosition.Value;
    }

    public override void Finish()
    {
        base.Finish();
    }

    public override void UpdateUI()
    {
        if (selectedObject == null)
            return;

        UIWorld uiWorld = Main.UIManager.uiWorld;

        Vector3 worldPos = selectedObject.transform.position;
        Vector3 upOffset = new Vector3(0, 1, 0);
        Vector3 downOffset = new Vector3(0, -1, 0);
        Vector3 leftOffset = new Vector3(-1, 0, 0);
        Vector3 rightOffset = new Vector3(1, 0, 0);
        Vector3 forwardOffset = new Vector3(0, 0, 1);
        Vector3 backOffset = new Vector3(0, 0, -1);

        uiWorld.SetArrow("up", worldPos, upOffset);
        uiWorld.SetArrow("down", worldPos, downOffset);
        uiWorld.SetArrow("left", worldPos, leftOffset);
        uiWorld.SetArrow("right", worldPos, rightOffset);
        uiWorld.SetArrow("forward", worldPos, forwardOffset);
        uiWorld.SetArrow("back", worldPos, backOffset);
    }

    public override void HandleInput()
    {
        Debug.Log("Tool Arrow Selected");
        Debug.Log(EventSystem.current.currentSelectedGameObject);
    }

    public Vector3 GetSnappedPosition(Vector3 position)
    {
        if(objectSnap == null || objectSnap.state == ObjectSnapState.None)
            return position;

        float threshold = objectSnap.threshold;
        float sqrThreshold = threshold * threshold;

        Vector3 snapPos = Vector3.zero;

        if (Vector3.SqrMagnitude(position - snapPos) < sqrThreshold)
            return snapPos;

        return position;
    }

    public Vector3 OffsetPosition(Vector3 position)
    {
        if (selectedObject == null)
            return position;

        SceneObjectRef sceneObjectRef = selectedObject.GetComponent<SceneObjectRef>();

        if (sceneObjectRef == null || sceneObjectRef.GetBounds() == null)
            return position;

        Vector3 transformPos = selectedObject.transform.position;
        position.y += transformPos.y - sceneObjectRef.GetBounds().Value.min.y;

        return position;
    }

    public static Vector3? MouseToWorldPoint()
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
}
