using UnityEngine;
using UnityEngine.UIElements;

public class MainView : MonoBehaviour
{
    void OnEnable()
    {
        // The UXML is already instantiated by the UIDocument component
        UIDocument uiDocument = GetComponent<UIDocument>();
        UIStyle.SetCanvasScaleFactor(uiDocument.panelSettings);
        uiDocument.rootVisualElement.Add(new EditorScreenView());
    }
}

public static class UIStyle
{
    public static float canvasScale = 1.0f;

    public static void SetCanvasScaleFactor(UnityEngine.UIElements.PanelSettings panelSettings)
    {
        canvasScale = ((float)Screen.width / panelSettings.referenceResolution.x) * (1 - panelSettings.match) + ((float)Screen.height / panelSettings.referenceResolution.y) * (panelSettings.match);
    }

    public static VisualElement BorderRadius(this VisualElement element, float radius)
    {
        element.style.borderTopLeftRadius = radius;
        element.style.borderTopRightRadius = radius;
        element.style.borderBottomLeftRadius = radius;
        element.style.borderBottomRightRadius = radius;
        return element;
    }

    public static VisualElement BorderRadius(this VisualElement element, float tl, float tr, float bl, float br)
    {
        element.style.borderTopLeftRadius = tl;
        element.style.borderTopRightRadius = tr;
        element.style.borderBottomLeftRadius = bl;
        element.style.borderBottomRightRadius = br;
        return element;
    }

    public static VisualElement SetPosition(this VisualElement element, float x, float y)
    {
        element.style.position = Position.Absolute;
        Vector2 elementPos = element.LocalToWorld(new Vector2(x, y));
        element.transform.position = elementPos;
        return element;
    }

    public static VisualElement SetSize(this VisualElement element, float width, float height)
    {
        element.style.width = width;
        element.style.height = height;
        return element;
    }

    public static VisualElement SetBGColor(this VisualElement element, Color color)
    {
        element.style.backgroundColor = color;
        return element;
    }
}

public class ScreenView : VisualElement
{
    public virtual void Init() { SetupLayout(); }
    public virtual void SetupLayout() { }
}

public class EditorScreenView : ScreenView
{ 
    Box background = new Box();

    public EditorScreenView()
    {
        Init();
        //background.BorderRadius(20.0f).SetSize(Screen.width/2, Screen.height/2);
        //Vector2 imageOffset = background.LocalToWorld(new Vector2(Screen.width/2, 50));
        //background.transform.position = imageOffset;
        //Add(background);
    }

    public override void SetupLayout()
    {
        CodeUtilsDebug.Run();

        //Left menu
        Layout.Horizontal(null, new LayoutNodeList { { "LeftMenu", 100.0f, ScaleType.Pixel }, { "ScreenCenter", 1.0f }, { "RightMenu", 100.0f, ScaleType.Pixel } });

        Layout.Vertical("ScreenCenter", new LayoutNodeList { { "HeaderMenu", 50.0f, ScaleType.Pixel }, { "3DArea", 1.0f }, { "FooterMenu", 50.0f, ScaleType.Pixel} });
        Layout.List("LeftMenu", 10, 1.0f).Padding(0.1f);

        Layout.Update();
        LayoutDebug.CreateDebugLayout(this, Layout.Root);
    }
}