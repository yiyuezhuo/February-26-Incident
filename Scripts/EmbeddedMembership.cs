namespace YYZ.App
{


using System.Collections.Generic;
using System;

public interface IContainerWeak<out TCC, out TC> where TCC : IEnumerable<TC>
{
    TCC children {get;}
}

public interface IContainer<out TCC, out TC> : IContainerWeak<TCC, TC> where TCC : ICollection<TC>
{
    // TCC children {get;}
    string ToHierarchy();
}

public class Child<T, TP, TCC> where T : Child<T, TP, TCC> where TP : IContainer<TCC, T> where TCC : ICollection<T>
{
    T This {get => this as T;}
    public TP parent{get; set;}
    public void MoveTo(TP container)
    {
        parent.children.Remove(This);
        EnterTo(container);

        moved?.Invoke(this, container);
    }
    public void EnterTo(TP container)
    {
        parent = container;
        container.children.Add(This);

        entered?.Invoke(this, container);
    }
    public void TryMoveTo(TP container)
    {
        if(parent.children.Contains(This))
            parent.children.Remove(This);
        EnterTo(container);
    }
    public void Destroy()
    {
        parent.children.Remove(This);
        parent = default(TP);

        destroyed?.Invoke(this, This);
    }
    public bool isDestroyed{get => EqualityComparer<TP>.Default.Equals(parent, default(TP));}
    
    public event EventHandler<TP> moved;
    public event EventHandler<TP> entered;
    public event EventHandler<T> destroyed;

    public string ToHierarchy() => $"{this} ∋ {parent}";
}


}