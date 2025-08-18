using System;
using Utility;

namespace AStar
{
    public class AStarNode : PathNodeBase
    {
        public float fCost;
        public float gCost;
        public float hCost;

        public AStarNode(int x, int y, NodeType nodeType)
            : base(x, y, nodeType)
        {
            // 初始化G值为无穷大，方便后续比较
            this.gCost = float.MaxValue;
        }

        // 实现 CompareTo 方法，用于比较两个节点的F值
        public override int CompareTo(PathNodeBase other)
        {
            var otherNode = other as AStarNode;
            if (otherNode == null) throw new ArgumentException("other is not AStarNode");
            int compare = fCost.CompareTo(otherNode.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(otherNode.hCost);
            }
            return compare;
        }
    }
}