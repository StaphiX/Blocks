using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LayoutDebugList
{
    public Dictionary<string, Box> boxDict = new Dictionary<string, Box>();

    public void Add(LayoutNode node)
    {
        Rect rect = node.Data;
        if (rect.width == 0 || rect.height == 0)
            return;

        Box box = new Box();
        box.SetPosition(rect.x, rect.y);
        box.SetSize(rect.width, rect.height);
        box.SetBGColor(GetDebugColor());

        Add(node.Name, box);
    }

    public void Add(string name, Box box)
    {
        if (boxDict.ContainsKey(name))
            Remove(name);

        boxDict.Add(name, box);
    }
    public void Remove(string name)
    {
        boxDict.Remove(name);
    }
    public void Clear()
    {
        boxDict.Clear();
    }

    public void UpdateFromLayoutNode(LayoutNode node)
    {
        Add(node);

        if (node.Children == null)
            return;

        for (int childIndex = 0; childIndex < node.Children.Count; ++childIndex)
        {
            UpdateFromLayoutNode(node.Children[childIndex]);
        }
    }

    public Color GetDebugColor()
    {
        Color color = UnityEngine.Random.ColorHSV(0.0f, 1.0f, 1.0f, 1.0f, 0.5f, 0.5f);
        return color;
    }

}

public static class LayoutDebug
{
    public static Dictionary<LayoutNode, LayoutDebugList> debugDict = new Dictionary<LayoutNode, LayoutDebugList>();

    public static void CreateDebugLayout(ScreenView screenView, LayoutNode root)
    {
        LayoutDebugList layoutDebugList = null;

        ClearView(screenView);

        if (debugDict.ContainsKey(root))
        {
            debugDict[root].Clear();
            layoutDebugList = debugDict[root];
        }
        else
        {
            layoutDebugList = new LayoutDebugList();
            debugDict.Add(root, layoutDebugList);
        }

        layoutDebugList.UpdateFromLayoutNode(root);

        UpdateView(screenView);
    }

    private static void ClearView(ScreenView screenView)
    {
        foreach (KeyValuePair<LayoutNode, LayoutDebugList> debugItem in debugDict)
        {
            LayoutDebugList layoutDebugList = debugItem.Value;

            foreach (KeyValuePair<string, Box> layoutDebugItem in layoutDebugList.boxDict)
            {
                Box box = layoutDebugItem.Value;

                screenView.Remove(box);
            }
        }
    }

    private static void UpdateView(ScreenView screenView)
    {
        foreach (KeyValuePair<LayoutNode, LayoutDebugList> debugItem in debugDict)
        {
            LayoutDebugList layoutDebugList = debugItem.Value;

            foreach (KeyValuePair<string, Box> layoutDebugItem in layoutDebugList.boxDict)
            {
                Box box = layoutDebugItem.Value;

                screenView.Add(box);
            }
        }
    }
}
