
namespace YYZ.Collections
{

using System.Collections.Generic;


// https://stackoverflow.com/questions/15622622/analogue-of-pythons-defaultdict
public class DefaultDictionary<TK, TV> : Dictionary<TK, TV> where TV : new() // TODO: Consider ConcurrentDictionary 
{
    public new TV this[TK key]
    {
        get
        {
            if(!TryGetValue(key, out var value))
            {
                value = new TV();
                Add(key, value);
            }
            return value;
        }
        set {base[key] = value;}
    }
}


}