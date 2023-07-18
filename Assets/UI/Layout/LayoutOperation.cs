using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILayoutOperation
{
    void Apply(LayoutNode node);
}

public class LayoutHorizontal : ILayoutOperation
{
    public void Apply(LayoutNode node)
    {
        if (node.Children.Count < 1)
            return;

        LayoutScale.DistributeHorizontal(node, node.Children);
    }
}

public class LayoutVertical : ILayoutOperation
{
    public void Apply(LayoutNode node)
    {
        if (node.Children.Count < 1)
            return;

        LayoutScale.DistributeVertical(node, node.Children);
    }
}

public class LayoutList : ILayoutOperation
{
    public LayoutList(int itemCount, float cellSize, ScaleType cellSizeScale = ScaleType.Relative, LayoutDirection direction = LayoutDirection.Vertical)
    {
        ItemCount = itemCount;
        Direction = direction;
    }

    public void Apply(LayoutNode node)
    {
        if (node.Children.Count < 1)
            return;

        if (Direction == LayoutDirection.Horizontal)
            LayoutScale.DistributeHorizontal(node, node.Children);
        if (Direction == LayoutDirection.Vertical)
            LayoutScale.DistributeVertical(node, node.Children, Pad);
    }

    public LayoutList Padding(float pad, ScaleType padScale = ScaleType.Relative)
    {
        Pad = new LayoutNode(pad, padScale);
        return this;
    }

    private LayoutDirection Direction { get; set; } = LayoutDirection.Vertical;
    private float ItemCount { get; set; } = 0;
    private LayoutNode Pad { get; set; } = null;
}

public class LayoutGrid : ILayoutOperation
{
    public LayoutGrid(int maxRows, int maxCols, float cellWidth, float cellHeight,
        ScaleType cellWidthScale = ScaleType.Relative, ScaleType cellHeightScale = ScaleType.Relative)
    {
        MaxRows = maxRows;
        MaxCols = maxCols;
    }

    public void Apply(LayoutNode node)
    {
        if (node.Children.Count < 1)
            return;

        LayoutScale.DistributeHorizontal(node, node.Children);
        for (int childIndex = 0; childIndex < node.Children.Count; ++childIndex)
        {
            LayoutNode childNode = node.Children[childIndex];
            if (childNode.Children.Count < 1)
                continue;

            LayoutScale.DistributeVertical(childNode, childNode.Children);
        }
    }

    public LayoutGrid Padding(float padRow, float padCol,
        ScaleType padRowScale = ScaleType.Relative, ScaleType padColScale = ScaleType.Relative)
    {
        PadRowNode = new LayoutNode(padRow, padRowScale);
        PadColNode = new LayoutNode(padCol, padColScale);
        return this;
    }

    private float MaxRows { get; set; }
    private float MaxCols { get; set; }
    private LayoutNode PadRowNode { get; set; }
    private LayoutNode PadColNode { get; set; }
}
