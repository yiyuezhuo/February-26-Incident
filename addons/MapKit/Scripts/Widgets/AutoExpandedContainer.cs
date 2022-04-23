namespace YYZ.MapKit.Widgets
{


using Godot;
using System.Collections.Generic;

public abstract class AutoExpandedContainer<NodeType, DataType> : Node2D where NodeType : CanvasItem
{
    protected abstract PackedScene GetRefScene();

    public void BindData(IEnumerable<DataType> dataIter)
    {
        var children = GetChildren();
        var prevLen = children.Count;
        var idx = 0;
        foreach(var data in dataIter)
        {
            NodeType child;
            if(idx >= prevLen)
            {
                child = (NodeType)GetRefScene().Instance();
                AddChild(child);
            }
            else
            {
                child = (NodeType)children[idx];
            }
            child.Show(); // Requires `CanvasItem`
            ApplyData(child, data);
            idx++;
        }
        for(var i=idx; i<prevLen; i++)
        {
            var child = (NodeType)children[i];
            child.Hide();
        }
    }

    protected abstract void ApplyData(NodeType child, DataType data);
}


}