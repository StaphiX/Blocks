using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ScaleType
{
    Relative,
    RelativeMinMax,
    Pixel,
    PixelMinMax,
    ContentScale,
}

public static class LayoutScale
{
    public static void InitScale(LayoutNode parent)
    {
        if (parent.Children == null || parent.Children.Count < 1)
            return;

        for (int childIndex = 0; childIndex < parent.Children.Count; ++childIndex)
        {
            LayoutNode node = parent.Children[childIndex];
            if (node.Value == 0.0f)
                continue;

            node.Init();
        }
    }

    public static void DistributeHorizontal(LayoutNode parent, List<LayoutNode> children)
    {
        float parentX = parent.Data.x;
        float availableSpace = parent.Data.width;
        float fixedW = 0.0f;
        float relativeW = 0.0f;

        for (int childIndex = 0; childIndex < children.Count; ++childIndex)
        {
            LayoutNode node = children[childIndex];

            if (IsFixedValue(node))
            {
                fixedW += node.Value;
            }
            else
            {
                relativeW += node.Value;
            }
        }

        availableSpace -= fixedW;

        float xPosition = parentX;
        for (int childIndex = 0; childIndex < children.Count; ++childIndex)
        {
            LayoutNode node = children[childIndex];
            float width = node.CalculateScale(availableSpace, relativeW);

            node.SetPosition(xPosition, null);
            node.SetWidth(width);

            xPosition += width;
        }
    }

    public static void DistributeVertical(LayoutNode parent, List<LayoutNode> children, LayoutNode padding = null)
    {
        float parentY = parent.Data.y;
        float availableSpace = parent.Data.height;
        float fixedH = 0.0f;
        float relativeH = 0.0f;

        for (int childIndex = 0; childIndex < children.Count; ++childIndex)
        {
            LayoutNode node = children[childIndex];

            if (IsFixedValue(node))
            {
                fixedH += node.Value;
            }
            else
            {
                relativeH += node.Value;
            }

            if (padding != null && childIndex < (children.Count-1))
            {
                if (IsFixedValue(padding))
                {
                    fixedH += padding.Value;
                }
                else
                {
                    relativeH += padding.Value;
                }
            }
        }

        availableSpace -= fixedH;

        float yPosition = parentY;
        for (int childIndex = 0; childIndex < children.Count; ++childIndex)
        {
            LayoutNode node = children[childIndex];
            float height = node.CalculateScale(availableSpace, relativeH);

            node.SetHeight(height);
            node.SetPosition(null, yPosition);

            yPosition += height;

            if(padding != null)
            {
                yPosition += CalculateScale(padding, availableSpace, relativeH);
            }
        }
    }

    public static void SetPosition(this LayoutNode node, float? x, float? y)
    {
        Rect nodeRect = node.Data;
        nodeRect.x = x.HasValue ? x.Value : nodeRect.x;
        nodeRect.y = y.HasValue ? y.Value : nodeRect.y;
        node.Data = nodeRect;
    }

    public static void SetWidth(this LayoutNode node, float w)
    {
        Rect nodeRect = node.Data;
        nodeRect.width = w;
        node.Data = nodeRect;
    }

    public static void SetHeight(this LayoutNode node, float h)
    {
        Rect nodeRect = node.Data;
        nodeRect.height = h;
        node.Data = nodeRect;
    }

    public static void SetRect(this LayoutNode node, Rect rect)
    {
        node.Data = rect;
    }

    public static void SetDimensions(this LayoutNode node, float w, float h)
    {
        Rect nodeRect = node.Data;
        nodeRect.width = w;
        nodeRect.height = h;
        node.Data = nodeRect;
    }

    public static bool IsFixedValue(LayoutNode node)
    {
        if (node.TypeVal == ScaleType.Relative)
            return false;

        return true;
    }

    public static float CalculateScale(this LayoutNode node, float availableSpace, float relativeTotal)
    {
        switch (node.TypeVal)
        {
            case ScaleType.Relative:
            case ScaleType.RelativeMinMax:
                return Relative(node.Value, availableSpace, relativeTotal);
            case ScaleType.Pixel:
            case ScaleType.PixelMinMax:
                return Pixel(node.Value);
            case ScaleType.ContentScale:
                return ContentScale(node.Value, availableSpace);
            default:
                return Relative(node.Value, availableSpace, relativeTotal);
        }
    }

    public static float CalculateScale(float value, ScaleType scaleType, float availableSpace, float relativeTotal)
    {
        switch (scaleType)
        {
            case ScaleType.Relative:
            case ScaleType.RelativeMinMax:
                return Relative(value, availableSpace, relativeTotal);
            case ScaleType.Pixel:
            case ScaleType.PixelMinMax:
                return Pixel(value);
            case ScaleType.ContentScale:
                return ContentScale(value, availableSpace);
            default:
                return Relative(value, availableSpace, relativeTotal);
        }
    }

    private static float Relative(float value, float fixedSpace, float relativeSpace)
    {
        return (value / relativeSpace) * fixedSpace;
    }

    private static float ContentScale(float value, float fixedSpace)
    {
        return Pixel(value);
    }

    private static float Pixel(float value)
    {
        return value;
    }
}
