using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayoutNode
{
    public LayoutNode(float val, ScaleType typeVal)
    {
        Name = null;
        Value = val;
        TypeVal = typeVal;
    }

    public LayoutNode(string name, Rect rect)
    {
        Name = name;
        Data = rect;
    }

    public LayoutNode(string name, float val, ScaleType typeVal)
    {
        Name = name;
        Value = val;
        TypeVal = typeVal;
    }

    public LayoutNode(string name, float min, float max, ScaleType typeVal)
    {
        Name = name;
        Value = min;
        TypeVal = typeVal;
        Min = min;
        Max = max;
    }

    public void Init()
    {
        if (Initialised)
            return;

        Initialised = true;

        Rect parentRect = new Rect(0, 0, Screen.width, Screen.height);
        if (Parent != null)
            parentRect = Parent.Data;

        this.SetRect(parentRect);
    }

    public LayoutNode Add(LayoutNode layoutNode)
    {
        if (Children == null)
            Children = new List<LayoutNode>();

        Children.Add(layoutNode);
        layoutNode.Parent = this;

        return layoutNode;
    }

    public void Remove(LayoutNode layoutNode)
    {
        Children.Remove(layoutNode);
        layoutNode.Parent = null;
    }

    public void SetParent(LayoutNode newParent)
    {
        if (Parent != null)
        {
            Parent.Remove(this);
        }

        newParent.Add(this);
    }

    public bool Initialised { get; set; } = false;
    public ScaleType TypeVal { get; set; } = ScaleType.Pixel;
    public string Name { get; set; } = "";
    public float Value { get; set; } = 0.0f;
    public float Min { get; set; } = 0.0f;
    public float Max { get; set; } = 0.0f;
    public Rect Data { get; set; } = new Rect(0, 0, 0, 0);

    public LayoutNode Parent { get; private set; } = null;
    public List<LayoutNode> Children { get; private set; } = null;

    public static implicit operator LayoutNode(float value)
    {
        return new LayoutNode(value, ScaleType.Relative);
    }
}
