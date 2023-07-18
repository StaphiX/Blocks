using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class DebugScene : MonoBehaviour
{
    public bool applyModToGO = false;

    void Awake()
    {

    }

    void Start()
    {
        BoundsGridTest();
    }

    void Update()
    {
        RotateCamera();
        BoundsGridRay();
    }

    void RotateCamera()
    {
        Camera.main.transform.RotateAround(Vector3.zero, Vector3.up, 0.02f);
    }

    void BoundsGridRay()
    {
        Profiler.BeginSample("Sample Grid Ray");
        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Ray screenRay = Camera.main.ScreenPointToRay(mousePos);
            BoundsGrid grid = Main.SceneManager.GetBoundsGrid();

            RaycastResult result = grid.RaycastAllObjects(screenRay);

            if(result != null)
            {
                Bounds bounds = result.faceData.bounds;
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                BoundsUtil.Transform(ref min, ref max, result.sceneObj.transform.localToWorldMatrix);
                bounds.SetMinMax(min, max);

                //DebugExtension.DebugBounds(bounds, Color.red, 20.0f);
                //Debug.Log("Found Triangle: " + result.vertPositions);
                for(int i = 0; i < result.vertPositions.Length; ++i)
                    DebugExtension.DebugPoint(result.vertPositions[i], Color.magenta, 0.1f, 0.2f, true);

                DebugExtension.DebugPoint(result.intersection, Color.cyan, 0.1f, 0.2f, true);
            }
        }
        Profiler.EndSample();
    }

    void BoundsGridTest()
    {
        BoundsGrid grid = Main.SceneManager.GetBoundsGrid();

        for(int i = 0; i < 1; ++i)
        {
            Vector3 point = new Vector3(9, 4.5f, 9);
            Bounds bounds = new Bounds(point, new Vector3(2, 1, 2));
            DebugExtension.DebugBounds(bounds, Color.cyan, 100, true);
            grid.AddCellsFromBounds(bounds);
        }
    }

    void MirrorModTest()
    {
        //if (applyModToGO)
        //{
        //    MirrorModifier mirrorModifier = new MirrorModifier();

        //    mirrorModifier.Setup(new Plane(Vector3.left, Vector3.zero));

        //    AddModifierToSceneObject(mirrorModifier);
        //}
    }

    public void AddObject(GameObject refObject)
    {
        if (refObject == null)
            return;

        Main.WorldManager.AddObject(refObject);
    }

    void AddModifierToSceneObject(GameObject gameObject, IModifier modifier)
    {
        if (gameObject == null)
            return;

        SceneObjectRef sceneObjectRef = gameObject.GetComponent<SceneObjectRef>();
        Main.SceneManager.AddModifier(modifier, sceneObjectRef);
    }
}
