namespace YYZ.App
{


using System.Collections.Generic;
using System;

public interface IContainerWeak<out TCC, out TC> where TCC : IEnumerable<TC>
{
    TCC children {get;}
    void OnChildrenUpdated();
    
}

public interface IContainer<out TCC, TC> : IContainerWeak<TCC, TC> where TCC : ICollection<TC>
{
    // TCC children {get;}
    string ToHierarchy();
    void OnChildrenEntered(TC child);
}

public class Child<T, TP, TCC> where T : Child<T, TP, TCC> where TP : IContainer<TCC, T> where TCC : ICollection<T>
{
    T This {get => this as T;}
    public TP parent{get; set;}
    public void MoveTo(TP container)
    {
        var parentOri = parent;
        parent.children.Remove(This);
        _EnterTo(container);

        moved?.Invoke(this, parentOri);
        parentOri.OnChildrenUpdated();
    }
    void _EnterTo(TP container)
    {
        var parentOri = parent;
        parent = container;
        container.children.Add(This);

        // entered?.Invoke(this, parentOri);
        container.OnChildrenUpdated();
    }
    public void EnterTo(TP container)
    {
        _EnterTo(container);
        container.OnChildrenEntered(This);
    }
    /*
    public void TryMoveTo(TP container)
    {
        if(parent.children.Contains(This))
            parent.children.Remove(This);
        _EnterTo(container);
    }
    */
    public void Destroy()
    {
        parent.children.Remove(This);
        isDestroying = true;
        // parent = default(TP); // This attribute may be helpful for some external hookers.

        parent.OnChildrenUpdated();
        destroyed?.Invoke(this, This);
    }
    public bool isDestroying;

    /// <summary>
    /// The event is invoke when moving finished, the parameter is the previous location, the new location can be got from sender.
    /// This event can be also treat as children of parent "updated".
    /// </summary>
    public event EventHandler<TP> moved; 
    // public event EventHandler<TP> entered;
    public event EventHandler<T> destroyed;

    public string ToHierarchy() => $"{this} ∋ {parent}";
}


}