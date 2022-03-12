using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using UnityEngine;
// using Godot;
using System;

namespace YYZ.PathFinding
{

public interface IGraph<IndexT>
{
    float MoveCost(IndexT src, IndexT dst);
    // Vector3Int: (x, y, 0)
    // src and dst are expected to be neighbor.

    float EstimateCost(IndexT src, IndexT dst);
    // z of src and dst are expected to be 0
    
    IEnumerable<IndexT> Neighbors(IndexT pos);
}

public class PathFinding<IndexT> // where IndexT : IEquatable<IndexT> // TODO: Do we really need this constraint?
{
    IGraph<IndexT> graph;

    public PathFinding(IGraph<IndexT> graph)
    {
        this.graph = graph;
    }

    List<IndexT> ReconstructPath(Dictionary<IndexT, IndexT> cameFrom, IndexT current)
    {
        var total_path = new List<IndexT>{current};
        while(cameFrom.ContainsKey(current)){
            current = cameFrom[current];
            total_path.Add(current);
        }
        total_path.Reverse();
        return total_path;
    }

    float TryGet(Dictionary<IndexT, float> dict, IndexT key)
    {
        if (dict.ContainsKey(key))
        {
            return dict[key];
        }
        return float.PositiveInfinity;
    }

    /// <summary>
    /// Path finding using A* algorithm, if failed it will return empty list.
    /// </summary>
    public List<IndexT> PathFindingAStar(IndexT src, IndexT dst)
    {
        var openSet = new HashSet<IndexT>{src};
        var cameFrom = new Dictionary<IndexT, IndexT>();

        var gScore = new Dictionary<IndexT, float>{{src, 0}}; // default Mathf.Infinity

        // Debug.Log($"graph:{graph}");
        var fScore = new Dictionary<IndexT, float>{{src, graph.EstimateCost(src, dst)}}; // default Mathf.Infiniy

        while(openSet.Count > 0)
        {
            IEnumerator<IndexT> openSetEnumerator = openSet.GetEnumerator();

            openSetEnumerator.MoveNext(); // assert?
            IndexT current = openSetEnumerator.Current;
            float lowest_f_score = fScore[current];

            while(openSetEnumerator.MoveNext())
            {
                IndexT pos = openSetEnumerator.Current;
                if(fScore[pos] < lowest_f_score)
                {
                    lowest_f_score = TryGet(fScore, pos);
                    current = pos;
                }
            }

            // if(EqualityComparer<IndexT>.Default.Equals(current, dst))
            if(current.Equals(dst))
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            foreach(IndexT neighbor in graph.Neighbors(current))
            {
                float tentative_gScore = TryGet(gScore, current) + graph.MoveCost(current, neighbor);
                if(tentative_gScore < TryGet(gScore, neighbor))
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative_gScore;
                    fScore[neighbor] = TryGet(gScore, neighbor) + graph.EstimateCost(neighbor, dst);

                    openSet.Add(neighbor);
                }
            }
        }
        return new List<IndexT>(); // failure
    }

    /*
    public Dictionary<IndexT, Path> GetReachable(IndexT src)
    {
        // First phase, determine true "reachable" nodes using the Dijkstra's algorithm

        var nodeToPath = GetReachable(src, float.PositiveInfinity);

        return nodeToPath;
    }

    public Dictionary<IndexT, Path> GetReachable(IEnumerable<IndexT> srcIter)
    {
        // First phase, determine true "reachable" nodes using the Dijkstra's algorithm

        return GetReachable(srcIter, float.PositiveInfinity);
    }
    */
    
    public Dictionary<IndexT, Path> GetReachable(IndexT src, float budget)
    {
        // First phase, determine true "reachable" nodes using the Dijkstra's algorithm

        var nodeToPath = GetReachable(new IndexT[]{src}, budget);

        return nodeToPath;
    }

    public Dictionary<IndexT, Path> GetReachable(IEnumerable<IndexT> srcIter, float budget)
    {
        // First phase, determine true "reachable" nodes using the Dijkstra's algorithm

        // var firstPath = new Path(){prev = default(IndexT)}; // null-like
        // var firstPath = new Path();
        // var nodeToPath = new Dictionary<IndexT, Path>(){{src, firstPath}};

        var nodeToPath = new Dictionary<IndexT, Path>();
        var openSet = new SortedSet<IndexT>(new PathComparer(nodeToPath));

        foreach(var src in srcIter)
        {
            // GD.Print($"src={src}");
            nodeToPath[src] = new Path();
            // nodeToPath.Add(src, new Path());
            openSet.Add(src);
        }
        
        // var openSet = new SortedSet<IndexT>(new PathComparer(nodeToPath)){src};
        var closeSet = new HashSet<IndexT>();

        // while(openSet.Count > 0)
        for(int i=0; i<20 && openSet.Count > 0; i++)
        {
            // GD.Print($"openSet({openSet.Count}), closeSet({closeSet.Count}). nodeToPath({nodeToPath.Count})");
            // GD.Print($"openSet={string.Join(",", openSet)}");
            // GD.Print($"closeSet={string.Join(",", closeSet)}");
            // var pickedNode = openSet.Min(); 
            var pickedNode = openSet.Min; // `Min()` comes from Linq.IEnumerable, while the `Min` *property* comes from `SortedSet`.
            var pickedPath = nodeToPath[pickedNode];

            openSet.Remove(pickedNode);
            closeSet.Add(pickedNode);

            if(budget - nodeToPath[pickedNode].cost <= 0) // We allows the value to be negative, but it will not be allowed to "propogate".
                continue;

            foreach(var node in graph.Neighbors(pickedNode))
            {
                if(closeSet.Contains(node))
                    continue;

                var pickedCost = pickedPath.cost + graph.MoveCost(pickedNode, node);
                if(nodeToPath.TryGetValue(node, out var path))
                {                    
                    if(pickedCost < path.cost)
                    {
                        path.cost = pickedCost;
                        path.prev = pickedNode;

                        openSet.Remove(node); // resort node
                        openSet.Add(node);
                    }
                }
                else
                {
                    path = new Path(){prev = pickedNode, cost = pickedCost};
                    nodeToPath[node] = path;
                    openSet.Add(node);
                }
                // GD.Print($"node={node}");
            }
            // GD.Print($"{nodeToPath[pickedNode]}, {budget}, {budget - nodeToPath[pickedNode].cost}, ");
            // return nodeToPath;
        }

        return nodeToPath;
    }

    public string PathToString(List<IndexT> path)
    {
        string s = "";
        foreach(IndexT p in path){
            s += p.ToString() + ","; // TODO: Use `Join` or `StringBuilder`
        }
        return $"Path({path.Count}):{s}";
    }

    public class Path
    {
        public IndexT prev;
        public float cost;

        public override string ToString() => $"Path({prev}, {cost})";
    }

    public class PathComparer : IComparer<IndexT>
    {
        Dictionary<IndexT, Path> nodeToPath;
        public PathComparer(Dictionary<IndexT, Path> nodeToPath)
        {
            this.nodeToPath = nodeToPath;
        }
        public int Compare(IndexT x, IndexT y)
        {
            return nodeToPath[x].cost.CompareTo(nodeToPath[y].cost);
        }
    }
}

/*
public class Path<IndexT>
{
    public List<IndexT> trace;
    public float traceCost; // this value can be negative
    public IndexT halfWayDst(){get => traceCost <}
    // public float halfWayReducedCost;
    public float remain;
    public IndexT src => trace[0];
    public IndexT dst => trace[trace.Count-1];
}
*/
}


