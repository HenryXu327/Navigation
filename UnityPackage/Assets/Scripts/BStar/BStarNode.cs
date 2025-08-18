using Utility;

namespace BStar
{
    public class BStarNode : PathNodeBase
    {
        public BStarNode(int x, int y, NodeType nodeType) 
            : base(x, y, nodeType)
        {
            
        }

        public override int CompareTo(PathNodeBase other)
        {
            return 0;
        }
    }
}