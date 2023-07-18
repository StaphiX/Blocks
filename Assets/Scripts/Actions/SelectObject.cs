using UnityEngine;

public class ActionSelectObject : IAction
{
    GameObject selectedObject = null;
    
    public ActionSelectObject(GameObject selectedObject)
    {
        this.selectedObject = selectedObject;
    }

    public void Run()
    {
        
    }

    public void Undo()
    {
        
    }
}

