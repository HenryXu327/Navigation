using UnityEngine;

namespace BStar
{
    public enum ExplorerState
    {
        Free, // 自由状态
        Crawling, // 绕墙爬行状态
    }

    public enum CrawlingDirection
    {
        Left, // 左手扶墙，优先向左转
        Right, // 右手扶墙，优先向右转
    }

    
    
    /// <summary>
    /// BStarExplorer类代表了每一个正在寻路的“触手”或“分支”。
    /// </summary>
    public class BStarExplorer
    {
        public BStarNode currentNode; // 当前所在的地图节点
        public ExplorerState state; // 当前状态（自由/绕爬）
        public CrawlingDirection crawlDirection; // 绕爬方向（仅在Crawling状态下有效）
        public Vector2Int lastMoveDirection; // 上一步移动的方向，用于决定绕墙时的“左”和“右”
        
        public BStarExplorer(BStarNode startNode, ExplorerState startState, Vector2Int initialDirection)
        {
            this.currentNode = startNode;
            this.state = startState;
            this.lastMoveDirection = initialDirection;
        }
    }
}