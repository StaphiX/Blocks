using UnityEditor.Rendering;
using UnityEngine;

public class ActionAddObject : IAction
{
    private GameObject baseObject = null;
    private GameObject sceneObject = null;

    public ActionAddObject(GameObject baseObject)
    {
        this.baseObject = baseObject;
    }

    public void Run()
    {
        sceneObject = GameObject.Instantiate(baseObject, baseObject.transform.parent);
        Main.WorldManager.toolMove = new ToolMoveObject(sceneObject);
    }

    public void Undo()
    {
        GameObject.Destroy(sceneObject);
        sceneObject = null;
    }
}
