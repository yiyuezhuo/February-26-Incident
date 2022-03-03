namespace YYZ.Collections
{


using System.Collections.Generic;

public interface IMembershipDatabase<CT, ET>
{
    IEnumerable<ET> QueryElements(CT container);
    IEnumerable<ET> QueryElements();
    IEnumerable<CT> QueryContainers();
    IEnumerable<KeyValuePair<ET, CT>> QueryElementContainers();
    CT QueryContainer(ET element);
    bool TryQueryContainer(ET element, out CT ret);
    bool ExistsContainer(CT container);
    bool ExistsElement(ET element);
    void TransferMembership(ET element, CT container);
    void CreateMembership(ET element, CT container);
    void CreateMembership(CT container);
    void DeleteElement(ET element);
}


public class MembershipDatabase<CT, ET> : IMembershipDatabase<CT, ET> // Container Type, Element Type
{
    Dictionary<CT, HashSet<ET>> containerToElements = new Dictionary<CT, HashSet<ET>>();
    Dictionary<ET, CT> elementToContainer = new Dictionary<ET, CT>();

    public IEnumerable<ET> QueryElements(CT container)
    {
        foreach(var el in containerToElements[container])
        {
            yield return el;
        }
    }

    public IEnumerable<ET> QueryElements()
    {
        foreach(var el in elementToContainer.Keys)
        {
            yield return el;
        }
    }

    public IEnumerable<CT> QueryContainers()
    {
        foreach(var container in containerToElements.Keys)
        {
            yield return container;
        }
    }

    public IEnumerable<KeyValuePair<ET, CT>> QueryElementContainers()
    {
        // return elementToContainer.GetEnumerator();
        foreach(var KV in elementToContainer)
            yield return KV;
    }

    public CT QueryContainer(ET element)
    {
        //Godot.GD.Print($"element={element}, elementToContainer.Count={elementToContainer.Count}");
        return elementToContainer[element];
    }
    
    public bool TryQueryContainer(ET element, out CT container)
    {
        return elementToContainer.TryGetValue(element, out container);
    }

    public bool ExistsContainer(CT container)
    {
        return containerToElements.ContainsKey(container);
    }

    public bool ExistsElement(ET element)
    {
        return elementToContainer.ContainsKey(element);
    }
    
    public void TransferMembership(ET element, CT container)
    {
        containerToElements[elementToContainer[element]].Remove(element);
        elementToContainer[element] = container;
        containerToElements[container].Add(element);
    }

    public void CreateMembership(ET element, CT container)
    {
        elementToContainer[element] = container;
        containerToElements[container].Add(element);
    }

    public void CreateMembership(CT container)
    {
        containerToElements[container] = new HashSet<ET>();
    }

    public void DeleteElement(ET element)
    {
        var container = elementToContainer[element];
        containerToElements[container].Remove(element);
        elementToContainer.Remove(element);
    }
}

class MembershipDatabaseFog<CT, ET, ICT, IET> : IMembershipDatabase<ICT, IET> where CT : ICT where ET : IET // I don't know how to call this "pattern" exactly
{
    IMembershipDatabase<CT, ET> data;

    public MembershipDatabaseFog(IMembershipDatabase<CT, ET> data)
    {
        this.data = data;
    }

    public IEnumerable<IET> QueryElements(ICT container)
    {
        foreach(var el in data.QueryElements((CT)container))
        {
            yield return el;
        }
    }

    public IEnumerable<IET> QueryElements()
    {
        foreach(var el in data.QueryElements())
        {
            yield return el;
        }
    }

    public IEnumerable<ICT> QueryContainers()
    {
        foreach(var ct in data.QueryContainers())
        {
            yield return ct;
        }
    }

    public IEnumerable<KeyValuePair<IET, ICT>> QueryElementContainers()
    {
        // return elementToContainer.GetEnumerator();
        foreach(var KV in data.QueryElementContainers())
            yield return new KeyValuePair<IET, ICT>(KV.Key, KV.Value); // TODO: Performance issue
    }

    public ICT QueryContainer(IET el) => data.QueryContainer((ET)el);

    public bool TryQueryContainer(IET element, out ICT ret)
    {
        var ok = data.TryQueryContainer((ET)element, out var _ret);
        ret = _ret;
        return ok;
    }

    public bool ExistsContainer(ICT ct)=>data.ExistsContainer((CT)ct);
    public bool ExistsElement(IET et)=>data.ExistsElement((ET)et);

    // public void TransferMembership(IET el, ICT ct) => data.TransferMembership((ET)el, (CT)ct);
    public void TransferMembership(IET el, ICT ct)
    {
        var ETel = (ET)el;
        var CTct = (CT)ct;
        data.TransferMembership(ETel, CTct);
    }

    public void CreateMembership(IET el, ICT ct) => data.CreateMembership((ET)el, (CT)ct);
    public void CreateMembership(ICT ct) => data.CreateMembership((CT)ct);
    public void DeleteElement(IET el) => data.DeleteElement((ET)el);
}

public interface IAdaptor<T>
{
    void SetAdaptee(T obj);
    T GetAdaptee();
}

public class MembershipDatabaseAdaptor<CT, ET, ACT, AET> : IMembershipDatabase<ACT, AET> where ACT : IAdaptor<CT>, new() where AET : IAdaptor<ET>, new() // I don't know how to call this "pattern" exactly
{
    IMembershipDatabase<CT, ET> data;

    public MembershipDatabaseAdaptor(IMembershipDatabase<CT, ET> data)
    {
        this.data = data;
    }

    public IEnumerable<AET> QueryElements(ACT container)
    {
        foreach(var el in data.QueryElements(container.GetAdaptee()))
        {
            var ret = new AET();
            ret.SetAdaptee(el);
            yield return ret;
        }
    }

    public IEnumerable<AET> QueryElements()
    {
        foreach(var el in data.QueryElements())
        {
            var ret = new AET();
            ret.SetAdaptee(el);
            yield return ret;
        }
    }

    public IEnumerable<ACT> QueryContainers()
    {
        foreach(var ct in data.QueryContainers())
        {
            var ret = new ACT();
            ret.SetAdaptee(ct);
            yield return ret;
        }
    }

    public IEnumerable<KeyValuePair<AET, ACT>> QueryElementContainers()
    {
        // return elementToContainer.GetEnumerator();
        foreach(var KV in data.QueryElementContainers())
        {
            var keyRet = new AET();
            keyRet.SetAdaptee(KV.Key);

            var valueRet = new ACT();
            valueRet.SetAdaptee(KV.Value);

            yield return new KeyValuePair<AET, ACT>(keyRet, valueRet); // TODO: performance issue
        }
    }

    public ACT QueryContainer(AET el)
    {
        var et = data.QueryContainer(el.GetAdaptee());
        var ret = new ACT();
        ret.SetAdaptee(et);
        return ret;
    }

    public bool TryQueryContainer(AET element, out ACT ret)
    {
        var ok = data.TryQueryContainer(element.GetAdaptee(), out var _ret);
        ret = new ACT();
        ret.SetAdaptee(_ret);
        return ok;
    }

    public bool ExistsContainer(ACT ct) => data.ExistsContainer(ct.GetAdaptee());
    public bool ExistsElement(AET et)=>data.ExistsElement(et.GetAdaptee());

    public void TransferMembership(AET el, ACT ct) => data.TransferMembership(el.GetAdaptee(), ct.GetAdaptee());

    public void CreateMembership(AET el, ACT ct) => data.CreateMembership(el.GetAdaptee(), ct.GetAdaptee());
    public void CreateMembership(ACT ct) => data.CreateMembership(ct.GetAdaptee());
    public void DeleteElement(AET el) => data.DeleteElement(el.GetAdaptee());
}


}