namespace YYZ.Data.February26
{


using System.Collections.Generic;

/*
public class EmbeddedMembership<TPC, TCC, TP, TC> where TPC : ICollection<TP> where TCC : ICollection<TC> // Type of parent container, type of children container, type of parent, type of children
{
    public TP parent;
    public TCC children;

    public void Move(TC child, EmbeddedMembership<TPC, TCC, TP, TC> another)
    {
        children.Remove(child);
        another.children.Add(child);
    }

    public void MoveTo(TPC parent)
    {
        parent.Remove(this);
        this.parent = parent;
    }
}
*/

/*
interface IHalfNode<TCC, TC> where TCC : ICollection<TC>
{
    TCC children{get; set;}
}

public class TreeNode<TCC, TC> : IHalfNode<TCC, TC> where TCC : ICollection<TC>
{
    public TreeNode<TCC, TC> parent;
    public TCC children{get; set;}

    public void Move(TC child, l)
}
*/

/*
public interface IHalfDownNode<T, TCC, TC> where TCC : ICollection<TC> where T : IHalfDownNode<T, TCC, TC>
{
    TCC children{get; set;}
    void MoveChildTo(TC child, T other);
}

public interface IHalfUpNode<TP>
{
    TP parent{get; set;}
    void Move(TP parent);
}

public class TreeNode<T, TPC, TP, TCC, TC> where T : TreeNode<T, TPC, TP, TCC, TC> where TCC : ICollection<TC> where TP : IHalfDownNode<TP, TPC, T> where TPC : ICollection<T>
{
    public TP parent;
    public TCC children{get; set;}

    public void MoveChildTo(TC child, T other)
    {
        children.Remove(child);
        other.children.Add(child);
    }

    public void MoveTo(TP otherParent)
    {
        parent.children.Remove(this);
        parent = otherParent;
    }
}
*/

/*
public interface IParent<TP>
{
    TP parent{get; set;}
}

public class Container<T, TC, TCC> where T : Container<T, TC, TCC> where TCC : ICollection<TC>, new() where TC : IParent<T>
{
    public TCC children = new TCC();
    public void MoveChildTo(TC child, T other)
    {
        child.parent = other;
        children.Remove(child);
    }
}
*/

public interface IContainerWeak<out TCC, out TC> where TCC : IEnumerable<TC>
{
    TCC children {get;}
}

public interface IContainer<out TCC, out TC> : IContainerWeak<TCC, TC> where TCC : ICollection<TC>
{
    // TCC children {get;}
}

public class Child<T, TP, TCC> where T : Child<T, TP, TCC> where TP : IContainer<TCC, T> where TCC : ICollection<T>
{
    T This {get => this as T;}
    public TP parent{get; set;}
    public void MoveTo(TP container)
    {
        parent.children.Remove(This);
        EnterTo(container);
    }
    public void EnterTo(TP container)
    {
        parent = container;
        container.children.Add(This);
    }
    public void TryMoveTo(TP container)
    {
        if(parent.children.Contains(This))
            parent.children.Remove(This);
        EnterTo(container);
    }
}


}