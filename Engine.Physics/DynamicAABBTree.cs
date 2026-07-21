
namespace Engine.Physics;

/// <summary>
/// Defines a contract for zero-allocation query callbacks during spatial partition searches.
/// </summary>

public interface IQueryCallback
{
    /// <summary>
    /// Invoked when a leaf node's bounding volume overlaps the query area.
    /// </summary>
    /// <param name="userData">The user payload ID (e.g., entity or collider ID) associated with the leaf node.</param>
    void OnQueryMatch(int userData);
}

/// <summary>
/// A dynamic bounding volume hierarchy (BVH) implemented as an array-backed dynamic binary AABB tree.
/// Provides $O(\logN)$ spatial partitioning, insertion, and bounding box queries without managed allocations.
/// </summary>
public class DynamicAABBTree
{
    private const int NullNode = -1;

    private Node[] _nodes;
    private int _root = NullNode;
    private int _freeList;
    private int _nodeCapacity = 16;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicAABBTree"/> class with an initial node capacity.
    /// </summary>
    public DynamicAABBTree()
    {
        _nodes = new Node[_nodeCapacity];

        // Build free-list pool linking unused nodes sequentially
        for (int i = 0; i < _nodeCapacity - 1; i++)
        {
            _nodes[i].Child1 = i + 1;
        }
        _nodes[_nodeCapacity - 1].Child1 = NullNode;
    }

    /// <summary>
    /// Inserts a new leaf node into the tree using the Surface Area Heuristic (SAH) to minimize tree cost.
    /// </summary>
    /// <param name="tightAABB">The tight bounding box enclosing the object.</param>
    /// <param name="userData">An integer identifier or index pointing to the underlying physics entity or collider.</param>
    /// <param name="fatMargin">The inflation margin added to create a "Fat AABB", reducing re-insertion frequency during object motion.</param>
    /// <returns>The unique node ID within the internal pool assigned to the newly created leaf.</returns>
    public int InsertLeaf(in AABB tightAABB, int userData, float fatMargin = 0.1f)
    {
        int nodeId = AllocateNode();

        // Inflate tight AABB to "Fat AABB"
        Vector3 margin = new Vector3(fatMargin);
        _nodes[nodeId].AABB = new AABB(tightAABB.Min - margin, tightAABB.Max + margin);
        _nodes[nodeId].UserData = userData;
        _nodes[nodeId].Child1 = NullNode;
        _nodes[nodeId].Child2 = NullNode;

        // Base case: tree is empty
        if (_root == NullNode)
        {
            _root = nodeId;
            _nodes[_root].Parent = NullNode;
            return nodeId;
        }

        // Find the best sibling for the new leaf using Surface Area Heuristic (SAH)
        AABB leafAABB = _nodes[nodeId].AABB;
        int index = _root;
        while (!_nodes[index].IsLeaf)
        {
            int child1 = _nodes[index].Child1;
            int child2 = _nodes[index].Child2;

            float area = _nodes[index].AABB.SurfaceArea;
            AABB combinedAABB = AABB.Union(_nodes[index].AABB, leafAABB);
            float combinedArea = combinedAABB.SurfaceArea;

            // Cost of creating a new parent for this node and the new leaf
            float cost = 2.0f * combinedArea;
            float inheritanceCost = 2.0f * (combinedArea - area);

            // Cost of descending into child 1
            float cost1 = inheritanceCost + (AABB.Union(leafAABB, _nodes[child1].AABB).SurfaceArea - (_nodes[child1].IsLeaf ? 0 : _nodes[child1].AABB.SurfaceArea));
            // Cost of descending into child 2
            float cost2 = inheritanceCost + (AABB.Union(leafAABB, _nodes[child2].AABB).SurfaceArea - (_nodes[child2].IsLeaf ? 0 : _nodes[child2].AABB.SurfaceArea));

            // Descend according to the minimum cost
            if (cost < cost1 && cost < cost2) break;
            index = (cost1 < cost2) ? child1 : child2;
        }

        int sibling = index;

        // Create a new branch parent
        int oldParent = _nodes[sibling].Parent;
        int newParent = AllocateNode();
        _nodes[newParent].Parent = oldParent;
        _nodes[newParent].UserData = NullNode;
        _nodes[newParent].AABB = AABB.Union(leafAABB, _nodes[sibling].AABB);

        if (oldParent != NullNode)
        {
            if (_nodes[oldParent].Child1 == sibling) _nodes[oldParent].Child1 = newParent;
            else _nodes[oldParent].Child2 = newParent;

            _nodes[newParent].Child1 = sibling;
            _nodes[newParent].Child2 = nodeId;
            _nodes[sibling].Parent = newParent;
            _nodes[nodeId].Parent = newParent;
        }
        else
        {
            _nodes[newParent].Child1 = sibling;
            _nodes[newParent].Child2 = nodeId;
            _nodes[sibling].Parent = newParent;
            _nodes[nodeId].Parent = newParent;
            _root = newParent;
        }

        // Walk back up the tree refitting bounding volumes
        index = _nodes[nodeId].Parent;
        while (index != NullNode)
        {
            int child1 = _nodes[index].Child1;
            int child2 = _nodes[index].Child2;

            _nodes[index].AABB = AABB.Union(_nodes[child1].AABB, _nodes[child2].AABB);
            index = _nodes[index].Parent;
        }

        return nodeId;
    }

    /// <summary>
    /// Performs an iterative depth-first traversal to find all leaf nodes overlapping the specified bounding box.
    /// Operates with zero heap allocations by using stack-allocated memory and a generic visitor callback.
    /// </summary>
    /// <typeparam name="TCallback">A user-defined struct type implementing <see cref="IQueryCallback"/>.</typeparam>
    /// <param name="queryAABB">The target bounding box to test for intersections.</param>
    /// <param name="callback">A reference to the callback struct invoked upon encountering an overlapping leaf.</param>

    public void QueryOverlaps<TCallback>(in AABB queryAABB, ref TCallback callback)
        where TCallback : struct, IQueryCallback
    {
        if (_root == NullNode) return;

        // Stack-allocated traversal buffer to prevent GC heap allocation
        Span<int> stack = stackalloc int[256];
        int stackCount = 0;
        stack[stackCount++] = _root;

        while (stackCount > 0)
        {
            int nodeId = stack[--stackCount];
            ref readonly Node node = ref _nodes[nodeId];

            if (node.AABB.Overlaps(queryAABB))
            {
                if (node.IsLeaf)
                {
                    callback.OnQueryMatch(node.UserData);
                }
                else if (stackCount + 2 < stack.Length)
                {
                    stack[stackCount++] = node.Child1;
                    stack[stackCount++] = node.Child2;
                }
            }
        }
    }

    /// <summary>
    /// Fetches an available node from the internal free-list, automatically doubling internal array capacity if exhausted.
    /// </summary>
    /// <returns>The array index of the allocated node.</returns>
    private int AllocateNode()
    {
        if (_freeList == NullNode)
        {
            // Expand pool capacity
            int oldCapacity = _nodeCapacity;
            _nodeCapacity *= 2;
            Array.Resize(ref _nodes, _nodeCapacity);

            for (int i = oldCapacity; i < _nodeCapacity - 1; i++)
            {
                _nodes[i].Child1 = i + 1;
            }
            _nodes[_nodeCapacity - 1].Child1 = NullNode;
            _freeList = oldCapacity;
        }

        int nodeId = _freeList;
        _freeList = _nodes[nodeId].Child1;
        return nodeId;
    }
}