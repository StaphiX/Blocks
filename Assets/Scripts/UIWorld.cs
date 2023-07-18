using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class UIWorld
{
    GameObject parent;
    Dictionary<string, GameObject> worldObjects = new Dictionary<string, GameObject>();

    public UIWorld(UIManager uIManager)
    {
        parent = new GameObject("World UI");
        uIManager.AddToCanvas(parent);
    }

    public GameObject SetArrow(string name, Vector3 position, Vector3 offsetVector)
    {
        GameObject arrow = null;

        if (worldObjects.ContainsKey(name))
        {
            arrow = worldObjects[name];
        }
        else
        {
            arrow = Main.UIManager.Create(parent, "WorldImage", "arrow-up-xxl");
            arrow.AddComponent<UIWorldArrow>();
            arrow.name = name;
            worldObjects.Add(name, arrow);
        }

        if (arrow == null)
            return null;

        Vector3 uiPos = Main.UIManager.GetCanvasPosition(position + offsetVector);
        arrow.transform.position = uiPos;

        Camera camera = Camera.main;
        Vector3 upCross = Vector3.Cross(offsetVector, Vector3.up);
        Vector3 forwardCross = Vector3.Cross(offsetVector, Vector3.forward);
        Vector3 forward = upCross.sqrMagnitude > 0 && upCross.sqrMagnitude > forwardCross.sqrMagnitude ? upCross : forwardCross;

        Quaternion targetRotation = Quaternion.LookRotation(-camera.transform.forward, offsetVector);

        arrow.transform.rotation = Quaternion.Inverse(camera.transform.rotation) * targetRotation;

        return arrow;
    }

    public void Update()
    {
        HandleInput();
    }

    public void HandleInput()
    {
        if(EventSystem.current.currentSelectedGameObject != null)
        {
            Main.WorldManager.HandleInput();
        }
    }
}
