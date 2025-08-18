using System;
using Utility;

namespace Dijkstra
{
    public class DijkstraNode : PathNodeBase
    {
        // 对于Dijkstra算法，只关心从起点到此节点的实际代价。
        public float gCost;
        
        public DijkstraNode(int x, int y, NodeType nodeType) 
            : base(x, y, nodeType)
        {
            // 初始化G值为无穷大，方便后续比较
            this.gCost = float.MaxValue;
        }


        public override int CompareTo(PathNodeBase other)
        {
            var otherNode = other as DijkstraNode;
            if (otherNode == null) throw new ArgumentException("other is not a DijkstraNode");
            
            // 直接比较两个节点的gCost，值越小，优先级越高。
            return this.gCost.CompareTo(otherNode.gCost);
        }
    }
}