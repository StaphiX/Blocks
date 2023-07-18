using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager
{
    public GameObject selectedObject = null;
    public ActionManager actionManager = new ActionManager();

    public Tool currentTool = null;

    public void Update()
    {
        if (currentTool != null)
            currentTool.Update();

        UpdateWorldUI();
    }

    public void AddObject(GameObject baseObject)
    {
        ActionAddObject action = new ActionAddObject(baseObject);
        actionManager.AddAction(action);
    }

    public void UpdateTool()
    {

    }

    public void UpdateWorldUI()
    {
        if (currentTool != null)
            currentTool.UpdateUI();
    }

    public void HandleInput()
    {
        if (currentTool != null)
            currentTool.HandleInput();
    }
}
