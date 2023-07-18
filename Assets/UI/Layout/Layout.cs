using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Metadata;
using static UnityEditor.PlayerSettings;

public enum LayoutDirection
{
	Horizontal,
	Vertical
}

public static class Layout
{
	private static Dictionary<string, LayoutNode> namedNodes = new Dictionary<string, LayoutNode>();
	private static Dictionary<string, ILayoutOperation> layoutOperations = new Dictionary<string, ILayoutOperation>();
	private static LayoutNode _root = null;
	public static LayoutNode Root
	{
		get { if (_root == null) CreateRoot(); return _root; }
		set { _root = value; }
	}

	private static void CreateRoot()
	{
		_root = new LayoutNode("root", new Rect(0, 0, Screen.width, Screen.height));
		namedNodes.Add(_root.Name, _root);

		SetRootScale();
	}

	private static void SetRootScale()
	{
		float width = Screen.width;
		float height = Screen.height;

		Root.Data = new Rect(0, 0, width, height);
	}

	public static LayoutNode Add(string parentName, LayoutNode layoutNode)
    {
		bool hasName = layoutNode.Name != null && layoutNode.Name.Length > 0;

        if (hasName && namedNodes.ContainsKey(layoutNode.Name))
        {
			Debug.AssertFormat(false, "{0} is already in the current Layout", layoutNode.Name);
			return layoutNode;
        }

		LayoutNode parent = null;
		if (parentName == null)
        {
			parent = Root;
		}
		else if(!namedNodes.TryGetValue(parentName, out parent))
        {
			Debug.AssertFormat(false, "Failed to find parent node {0}", parentName);
			return layoutNode;
        }

		if(hasName)
		{
			namedNodes.Add(layoutNode.Name, layoutNode);
		}
        return parent.Add(layoutNode);
    }

	public static void AddRange(string parentName, LayoutNodeList layoutNodeList)
    {
		for(int childIndex = 0; childIndex < layoutNodeList.Count; childIndex++)
		{
			Add(parentName, layoutNodeList[childIndex]);
		}
	}

	public static void AddOperation(string parentName, ILayoutOperation operation)
    {
		if (parentName == null)
			parentName = Root.Name;
		layoutOperations[parentName] = operation;
    }

	public static void Update()
	{
		CalculateLayout();
	}

	public static void CalculateLayout()
	{
		foreach(KeyValuePair<string, ILayoutOperation> keyValue in layoutOperations)
		{
			string parentNodeName = keyValue.Key;
			LayoutNode parentNode = null;
			if (parentNodeName == null)
				parentNode = Root;
			else if (namedNodes.ContainsKey(parentNodeName))
				parentNode = namedNodes[parentNodeName];

			if (parentNode == null)
				continue;

			ILayoutOperation layoutOperation = keyValue.Value;
			if (layoutOperation == null)
				continue;

			LayoutScale.InitScale(parentNode);
            layoutOperation.Apply(parentNode);
		}
	}

	public static LayoutHorizontal Horizontal(string parentName, LayoutNodeList layoutNodeList)
    {
		LayoutHorizontal horizontalOperation = new LayoutHorizontal();
		AddOperation(parentName, horizontalOperation);
		AddRange(parentName, layoutNodeList);

		return horizontalOperation;
    }

	public static LayoutVertical Vertical(string parentName, LayoutNodeList layoutNodeList)
	{
		LayoutVertical verticalOperation = new LayoutVertical();
		AddOperation(parentName, verticalOperation);
		AddRange(parentName, layoutNodeList);

        return verticalOperation;
    }

	public static LayoutGrid Grid(string parentName, int maxRows, int maxCols, float cellWidth, float cellHeight)
	{
		LayoutGrid gridOperation = new LayoutGrid(maxRows, maxCols, cellWidth, cellHeight);
		AddOperation(parentName, gridOperation);

		for(int row = 0; row < maxRows; ++row)
		{
            LayoutNode rowNode = Add(parentName, new LayoutNode(1.0f, ScaleType.Relative));

            for (int col = 0; col < maxCols; ++col)
			{
				rowNode.Add(new LayoutNode(1.0f, ScaleType.Relative));
			}

		}

		return gridOperation;
	}

    public static LayoutList List(string parentName, int itemCount, float cellSize, ScaleType cellSizeScale = ScaleType.Relative, LayoutDirection direction = LayoutDirection.Vertical)
    {
        LayoutList listOperation = new LayoutList(itemCount, cellSize, cellSizeScale, direction);
        AddOperation(parentName, listOperation);

        for (int item = 0; item < itemCount; ++item)
        {
			string childName = String.Format("{0}-list{1}", parentName, item);
			LayoutNode child = new LayoutNode(childName, cellSize, cellSizeScale);
            Add(parentName, child);
        }

        return listOperation;
    }
}

public class LayoutNodeList : List<LayoutNode>
{
	public void Add(float val, ScaleType scaleType)
	{
		Add(new LayoutNode(null, val, scaleType));
	}

	public void Add(string name, float val)
	{
		Add(new LayoutNode(name, val, ScaleType.Relative));
	}

	public void Add(string name, float val, ScaleType scaleType)
	{
		Add(new LayoutNode(name, val, scaleType));
	}

	public void Add(string name, float min, float max, ScaleType scaleType)
	{
		Add(new LayoutNode(name, min, max, scaleType));
	}
}