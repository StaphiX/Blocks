using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UICacheData
{
    public UICacheData(int lastAccessedFrame, GameObject gameObject)
    {
        this.lastAccessedFrame = lastAccessedFrame;
        this.gameObject = gameObject;
    }

    public void SetFrame(int lastAccessedFrame)
    {
        this.lastAccessedFrame = lastAccessedFrame;
    }

    int lastAccessedFrame;
    public GameObject gameObject;
}

public class UIManager
{
    GameObject canvas = null;
    static string baseElementsPath = "UIElements" + Path.DirectorySeparatorChar;
    static string baseUIPath = "UI" + Path.DirectorySeparatorChar;
    Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    Dictionary<string, UICacheData> baseObjectCache = new Dictionary<string, UICacheData>();

    public UIWorld uiWorld = null;

    public UIManager()
    {
        canvas = GameObject.Find("Canvas");
        uiWorld = new UIWorld(this);
    }

    public Vector3 GetCanvasPosition(Vector3 worldPosition)
    {
        if (canvas == null)
            return Vector3.zero;

        Camera mainCam = Camera.main;
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPosition);
        screenPos.z = canvas.transform.position.z;

        return screenPos;
    }

    public void AddToCanvas(GameObject uiObject)
    {
        if (canvas == null)
            return;

        uiObject.transform.parent = canvas.transform;
    }

    public GameObject Create(GameObject parent, string baseElement, string imageName)
    {
        GameObject baseObject = null;
        if(baseObjectCache.ContainsKey(baseElement))
        {
            baseObject = baseObjectCache[baseElement].gameObject;
            baseObjectCache[baseElement].SetFrame(Time.frameCount);
        }

        if (baseObject == null)
        {
            baseObject = Resources.Load(baseElementsPath + baseElement) as GameObject;
            UICacheData uICacheData = new UICacheData(Time.frameCount, baseObject);
            baseObjectCache.Add(baseElement, uICacheData);
        }

        if (baseObject == null)
            return null;

        GameObject createdGo = GameObject.Instantiate(baseObject, parent.transform);

        Image image = createdGo.GetComponent<Image>();

        if (image == null)
        {
            GameObject.DestroyImmediate(createdGo);
            return null;
        }

        SetSprite(createdGo, imageName);

        return createdGo;
    }

    public void SetSprite(GameObject go, string imageName)
    {
        Image image = go.GetComponent<Image>();

        if (go == null)
            return;

        Sprite sprite = GetSprite(imageName);
        if (sprite == null)
            return;

        image.sprite = sprite;
    }

    public Sprite GetSprite(string spriteName)
    {
        if (spriteCache.ContainsKey(spriteName))
        {
            return spriteCache[spriteName];
        }

        Sprite loadedSprite = Resources.Load<Sprite>(baseUIPath + spriteName);
        if (loadedSprite == null)
            return null;

        spriteCache.Add(spriteName, loadedSprite);
        return loadedSprite;
    }
}
