namespace YYZ.Data.February26
{


using System.Collections.Generic;


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

    public string ToHierarchy() => $"{this} ∋ {parent}";
}


}