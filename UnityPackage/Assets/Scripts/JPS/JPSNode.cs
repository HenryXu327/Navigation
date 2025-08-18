using System;
using Utility;

namespace JPS
{
    public class JPSNode : PathNodeBase
    {
        public float fCost;
        public float gCost;
        public float hCost;

        public JPSNode(int x, int y, NodeType nodeType)
            : base(x, y, nodeType)
        {
            this.gCost = float.MaxValue;
        }

        public override int CompareTo(PathNodeBase other)
        {
            var otherNode = other as JPSNode;
            if (otherNode == null) throw new ArgumentException("other is not JPSNode");
            int compare = fCost.CompareTo(otherNode.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(otherNode.hCost);
            }
            return compare;
        }
    }
}