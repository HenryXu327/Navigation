using System;

namespace Utility
{
    public enum NodeType
    {
        Open,
        Obstacle,
    }
    
    public abstract class PathNodeBase : IComparable<PathNodeBase>
    {
        public int x;
        public int y;
        public NodeType nodeType;
        public PathNodeBase parent;

        public PathNodeBase(int x, int y, NodeType nodeType)
        {
            this.x = x;
            this.y = y;
            this.nodeType = nodeType;
        }

        // 需要子类实现的比较方法
        public abstract int CompareTo(PathNodeBase other);
    }
} 