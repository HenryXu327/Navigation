#define ONLY_STRAIGHT // 仅可直线移动，在无障碍时可以斜向移动

using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace JPS
{
    public class JPSManager : IPathfindingManager
    {
        private static JPSManager instance;

        public static JPSManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new JPSManager();
                }

                return instance;
            }
        }

        #region IPathfindingManager接口实现

        PathNodeBase[,] IPathfindingManager.nodesMap => nodesMap;
        HashSet<PathNodeBase> IPathfindingManager.closedList => new HashSet<PathNodeBase>(closedList);

        List<PathNodeBase> IPathfindingManager.FindPath(int startX, int startY, int endX, int endY)
        {
            var result = FindPath(startX, startY, endX, endY);
            return result == null ? null : new List<PathNodeBase>(result);
        }

        void IPathfindingManager.InitMap(int width, int height, int obstacleCount) =>
            InitMap(width, height, obstacleCount);

        #endregion

        private int width;
        private int height;

        public JPSNode[,] nodesMap;
        
        private JPSNode endNode;

        public PriorityQueue<JPSNode> openList = new PriorityQueue<JPSNode>();
        public HashSet<JPSNode> closedList = new HashSet<JPSNode>();

        public void InitMap(int width, int height, int obstacleCount)
        {
            this.width = width;
            this.height = height;
            nodesMap = new JPSNode[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    nodesMap[i, j] = new JPSNode(i, j, NodeType.Open);
                }
            }

            for (int i = 0; i < obstacleCount; i++)
            {
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);
                nodesMap[x, y].nodeType = NodeType.Obstacle;
            }
        }
        
        public bool IsInMap(JPSNode node)
        {
            return IsInMap(node.x, node.y);
        }
        
        public bool IsInMap(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
        
        public List<JPSNode> FindPath(JPSNode startNode, JPSNode endNode)
        {
            return FindPath(startNode.x, startNode.y, endNode.x, endNode.y);
        }

        public List<JPSNode> FindPath(int startX, int startY, int endX, int endY)
        {
            openList.Clear();
            closedList.Clear();

            // 新增：重置所有节点的gCost、hCost、fCost和parent，防止多次寻路状态残留
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    var node = nodesMap[i, j];
                    node.gCost = float.MaxValue;
                    node.hCost = 0;
                    node.fCost = 0;
                    node.parent = null;
                }
            }
            
            var startNode = nodesMap[startX, startY];
            this.endNode = nodesMap[endX, endY];

            if (startNode.nodeType == NodeType.Obstacle || endNode.nodeType == NodeType.Obstacle) return null;
            if (!IsInMap(startX, startY) || !IsInMap(endX, endY)) return null;
            
            startNode.gCost = 0;
            startNode.hCost = CalculateDiagonalHCost(startNode, endNode);
            startNode.fCost = startNode.gCost + startNode.hCost;
            startNode.parent = null;
            
            openList.Enqueue(startNode);

            while (openList.Count > 0)
            {
                var currentNode = openList.Dequeue();
                closedList.Add(currentNode);

                if (currentNode == endNode)
                {
                    return ReconstructPath(endNode);
                }
                
                FindJumpPoints(currentNode);
            }
            
            return null;
        }

        /// <summary>
        /// 识别跳点，并将其加入openList
        /// </summary>
        /// <param name="currentNode"></param>
        private void FindJumpPoints(JPSNode currentNode)
        {
#if ONLY_STRAIGHT
            var explorationDirections = GetExplorationDirectionsOnlyStraight(currentNode);
#else
            var explorationDirections = GetExplorationDirectionsCanDiagonal(currentNode);
#endif
            foreach (var direction in explorationDirections)
            {
                // 从当前节点，沿着特定方向跳跃，寻找跳点
                JPSNode jumpPoint = Jump(currentNode, direction.x, direction.y);

                if (jumpPoint != null)
                {
                    // 跳过已经在关闭列表的节点（A*流程）
                    if (closedList.Contains(jumpPoint))
                        continue;
                    
                    // G值是两跳点间的直线距离
                    float newGCost = currentNode.gCost + GetDistance(currentNode, jumpPoint);
                    if (newGCost < jumpPoint.gCost)
                    {
                        jumpPoint.gCost = newGCost;
                        jumpPoint.hCost = CalculateDiagonalHCost(jumpPoint, endNode);
                        jumpPoint.fCost = jumpPoint.gCost + jumpPoint.hCost;
                        jumpPoint.parent = currentNode;
                        
                        openList.Enqueue(jumpPoint);
                    }
                    
                }
            }
        }
        
        private float GetDistance(JPSNode a, JPSNode b) // JPS用对角线距离更优
        {
            return Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.y - b.y, 2));
        }

        /// <summary>
        /// 跳跃函数
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        private JPSNode Jump(JPSNode currentNode, int dx, int dy)
        {
            int nextX = currentNode.x + dx;
            int nextY = currentNode.y + dy;

            // 越界或撞墙，此方向跳跃失败
            if (!IsInMap(nextX, nextY) || nodesMap[nextX, nextY].nodeType == NodeType.Obstacle)
            {
                return null;
            }
            
            // 这个注释里的是之前的错误认知，是多余的
// #if ONLY_STRAIGHT
//             // 不能穿过斜向的障碍
//             if (!CanWalkTo(currentNode.x, currentNode.y, nextX, nextY))
//             {
//                 return null;
//             }
// #endif
            
            // 斜向不可穿过两个障碍中间
            if (dx != 0 && dy != 0)
            {
                if (!IsWalkable(currentNode.x + dx, currentNode.y) && !IsWalkable(currentNode.x, currentNode.y + dy))
                {
                    return null;
                }
            }

            JPSNode nextNode = nodesMap[nextX, nextY];

            // 1. 如果是终点，则找到了一个跳点
            if (nextNode == endNode)
            {
                return nextNode;
            }

#if ONLY_STRAIGHT
            // 2. 检查强迫邻居，如果存在，则当前节点是一个跳点
            if (HasForcedNeighborOnlyStraight(nextNode, dx, dy))
            {
                return nextNode;
            }
#else
            if (HasForcedNeighborCanDiagonal(nextNode, dx, dy))
            {
                return nextNode;
            }
#endif

            // 3. 对角线移动的额外规则
            if (dx != 0 && dy != 0)
            {
                // 从对角线节点出发，进行水平和垂直的跳跃
                // 如果能找到跳点，则当前对角线节点也是跳点
                if (Jump(nextNode, dx, 0) != null || Jump(nextNode, 0, dy) != null)
                {
                    return nextNode;
                }
            }

            // 如果以上都不是，则继续沿着当前方向递归跳跃
            return Jump(nextNode, dx, dy);
        }
        
        
        
        /// <summary>
        /// 根据父节点（上一步）怎么到达当前节点，剪枝邻居，获取需要的探索方向
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private List<Vector2Int> GetExplorationDirectionsCanDiagonal(JPSNode node)
        {
            var explorationDirections = new List<Vector2Int>();
            
            // 如果是起点，8个方向都要探索
            if (node.parent == null)
            {
                // 先直线，再对角线
                explorationDirections.Add(new Vector2Int(0, 1));
                explorationDirections.Add(new Vector2Int(1, 0));
                explorationDirections.Add(new Vector2Int(0, -1));
                explorationDirections.Add(new Vector2Int(-1, 0));
                
                explorationDirections.Add(new Vector2Int(1, 1));
                explorationDirections.Add(new Vector2Int(1, -1));
                explorationDirections.Add(new Vector2Int(-1, -1));
                explorationDirections.Add(new Vector2Int(-1, 1));

                return explorationDirections;
            }
            
            // 如果不是起点，则根据父节点的位置，剪枝邻居
            var parent = node.parent;
            
            int dx = Mathf.Clamp(node.x - parent.x, -1, 1);
            int dy = Mathf.Clamp(node.y - parent.y, -1, 1);
            
            // 若为直线移动
            if (dx == 0 || dy == 0)
            {
                // 继续探索刚刚的直线方向
                if (IsWalkable(node.x + dx, node.y + dy))
                    explorationDirections.Add(new Vector2Int(dx, dy));
                
                // 检查产生了强迫邻居的方向
                if (!IsWalkable(node.x, node.y + 1) && IsWalkable(node.x + dx, node.y + 1) && dy == 0) 
                    explorationDirections.Add(new Vector2Int(dx, 1));
                if (!IsWalkable(node.x, node.y - 1) && IsWalkable(node.x + dx, node.y - 1) && dy == 0) 
                    explorationDirections.Add(new Vector2Int(dx, -1));
                if (!IsWalkable(node.x + 1, node.y) && IsWalkable(node.x + 1, node.y + dy) && dx == 0)
                    explorationDirections.Add(new Vector2Int(1, dy));
                if (!IsWalkable(node.x - 1, node.y) && IsWalkable(node.x - 1, node.y + dy) && dx == 0) 
                    explorationDirections.Add(new Vector2Int(-1, dy));
            }
            // 若为对角线移动，添加三个方向
            else
            {
                // 探索对角线的x分量方向
                if (IsWalkable(node.x + dx, node.y))
                    explorationDirections.Add(new Vector2Int(dx, 0));
                // 探索对角线的y分量方向
                if (IsWalkable(node.x, node.y + dy))
                    explorationDirections.Add(new Vector2Int(0, dy));
                
                // 继续探索对角线方向
                if (IsWalkable(node.x + dx, node.y + dy)) 
                    explorationDirections.Add(new Vector2Int(dx, dy));
                
                if (!IsWalkable(node.x - dx, node.y) && IsWalkable(node.x - dx, node.y + dy))
                    explorationDirections.Add(new Vector2Int(-dx, dy));
                if (!IsWalkable(node.x, node.y - dy) && IsWalkable(node.x + dx, node.y - dy))
                    explorationDirections.Add(new Vector2Int(dx, -dy));
            }
            
            return explorationDirections;
        }
        
        private List<Vector2Int> GetExplorationDirectionsOnlyStraight(JPSNode node)
        {
            var explorationDirections = new List<Vector2Int>();
            
            // 如果是起点，8个方向都要探索
            if (node.parent == null)
            {
                // 先直线，再对角线
                explorationDirections.Add(new Vector2Int(0, 1));
                explorationDirections.Add(new Vector2Int(1, 0));
                explorationDirections.Add(new Vector2Int(0, -1));
                explorationDirections.Add(new Vector2Int(-1, 0));
                
                if (CanWalkTo(node.x, node.y, node.x + 1, node.y + 1))
                    explorationDirections.Add(new Vector2Int(1, 1));
                if (CanWalkTo(node.x, node.y, node.x + 1, node.y - 1))
                    explorationDirections.Add(new Vector2Int(1, -1));
                if (CanWalkTo(node.x, node.y, node.x - 1, node.y - 1))
                    explorationDirections.Add(new Vector2Int(-1, -1));
                if (CanWalkTo(node.x, node.y, node.x - 1, node.y + 1))
                    explorationDirections.Add(new Vector2Int(-1, 1));

                return explorationDirections;
            }
            
            // 如果不是起点，则根据父节点的位置，剪枝邻居
            var parent = node.parent;
            
            int dx = Mathf.Clamp(node.x - parent.x, -1, 1);
            int dy = Mathf.Clamp(node.y - parent.y, -1, 1);
            
            // 若为直线移动
            if (dx == 0 || dy == 0)
            {
                // 继续探索刚刚的直线方向
                if (IsWalkable(node.x + dx, node.y + dy))
                    explorationDirections.Add(new Vector2Int(dx, dy));
                
                // 检查产生了强迫邻居的方向
                // 水平移动
                if (!IsWalkable(node.x - dx, node.y + 1) && IsWalkable(node.x, node.y + 1) && dy == 0)
                {
                    explorationDirections.Add(new Vector2Int(0, 1));
                    if (CanWalkTo(node.x, node.y, node.x + dx, node.y + 1))
                        explorationDirections.Add(new Vector2Int(dx, 1));
                }
                if (!IsWalkable(node.x - dx, node.y - 1) && IsWalkable(node.x, node.y - 1) && dy == 0)
                {
                    explorationDirections.Add(new Vector2Int(0, -1));
                    if (CanWalkTo(node.x, node.y, node.x + dx, node.y - 1))
                        explorationDirections.Add(new Vector2Int(dx, -1));
                }
                // 竖直移动
                if (!IsWalkable(node.x + 1, node.y - dy) && IsWalkable(node.x + 1, node.y) && dx == 0)
                {
                    explorationDirections.Add(new Vector2Int(1, 0));
                    if (CanWalkTo(node.x, node.y, node.x + 1, node.y + dy))
                        explorationDirections.Add(new Vector2Int(1, dy));
                }
                if (!IsWalkable(node.x - 1, node.y - dy) && IsWalkable(node.x - 1, node.y) && dx == 0)
                {
                    explorationDirections.Add(new Vector2Int(-1, 0));
                    if (CanWalkTo(node.x, node.y, node.x - 1, node.y + dy))
                        explorationDirections.Add(new Vector2Int(-1, dy));
                }
            }
            // 若为对角线移动，添加三个方向
            else
            {
                // 探索对角线的x分量方向
                if (IsWalkable(node.x + dx, node.y))
                    explorationDirections.Add(new Vector2Int(dx, 0));
                // 探索对角线的y分量方向
                if (IsWalkable(node.x, node.y + dy))
                    explorationDirections.Add(new Vector2Int(0, dy));
                
                // 继续探索对角线方向
                if (IsWalkable(node.x, node.y + dy) || IsWalkable(node.x + dx, node.y)) {
                    if (CanWalkTo(node.x, node.y, node.x + dx, node.y + dy)) 
                        explorationDirections.Add(new Vector2Int(dx, dy));
                }
            }
            
            return explorationDirections;
        }

        private bool HasForcedNeighborCanDiagonal(JPSNode node, int dx, int dy)
        {
            // 对角线移动
            if (dx != 0 && dy != 0)
            {
                // 检查条件：左边有墙，且左上可走 or 下边有墙，且右下可走
                if (!IsWalkable(node.x - dx, node.y) && IsWalkable(node.x - dx, node.y + dy)) return true;
                if (!IsWalkable(node.x, node.y - dy) && IsWalkable(node.x + dx, node.y - dy)) return true;
            }
            // 直线移动
            else
            {
                // 水平移动
                if (dx != 0)
                {
                    // 上方有墙，且右/左上可走
                    if (!IsWalkable(node.x, node.y + 1) && IsWalkable(node.x + dx, node.y + 1)) return true;
                    // 下方有墙，且右/左下可走
                    if (!IsWalkable(node.x, node.y - 1) && IsWalkable(node.x + dx, node.y - 1)) return true;
                }
                // 竖直移动
                else
                {
                    // 右边有墙，且右上/下可走
                    if (!IsWalkable(node.x + 1, node.y) && IsWalkable(node.x + 1, node.y + dy)) return true;
                    // 左边有墙，且左上/下可走
                    if (!IsWalkable(node.x - 1, node.y) && IsWalkable(node.x - 1, node.y + dy)) return true;
                }
            }
            
            return false;
        }

        private bool HasForcedNeighborOnlyStraight(JPSNode node, int dx, int dy)
        {
            // 对角线移动
            if (dx != 0 && dy != 0)
            {
                return false;
            }
            // 直线移动
            else
            {
                // 水平移动
                if (dx != 0)
                {
                    // 左下有墙，且下可走
                    if (!IsWalkable(node.x - dx, node.y - 1) && IsWalkable(node.x, node.y - 1)) return true;
                    // 左上有墙，且上可走
                    if (!IsWalkable(node.x - dx, node.y + 1) && IsWalkable(node.x, node.y + 1)) return true;
                }
                // 竖直移动
                else
                {
                    // 右下有墙，且右可走
                    if (!IsWalkable(node.x + 1, node.y - dy) && IsWalkable(node.x + 1, node.y)) return true;
                    // 左下有墙，且左可走
                    if (!IsWalkable(node.x - 1, node.y - dy) && IsWalkable(node.x - 1, node.y)) return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// JPS 必须使用特殊的路径回溯方法
        /// </summary>
        /// <param name="endNode"></param>
        /// <returns></returns>
        private List<JPSNode> ReconstructPath(JPSNode endNode)
        {
            var path = new List<JPSNode>();
            var currentNode = endNode;
            while (currentNode != null)
            {
                var parent = currentNode.parent as JPSNode;
                if (parent != null)
                {
                    // 在父子跳点之间，填补中间的直线路径
                    int dx = Mathf.Clamp(currentNode.x - parent.x, -1, 1);
                    int dy = Mathf.Clamp(currentNode.y - parent.y, -1, 1);
                    int currentX = currentNode.x;
                    int currentY = currentNode.y;

                    while (currentX != parent.x || currentY != parent.y)
                    {
                        path.Add(nodesMap[currentX, currentY]);
                        currentX -= dx;
                        currentY -= dy;
                    }
                }
                
                path.Add(currentNode); // 加入自己
                
                currentNode = parent;
            }
            
            path.Reverse();

            return path;
        }

        private bool IsWalkable(int x, int y)
        {
            return IsInMap(x, y) && nodesMap[x, y].nodeType != NodeType.Obstacle;
        }

        private bool CanWalkTo(int curX, int curY, int nextX, int nextY)
        {
            if (!IsWalkable(nextX, nextY))
                return false;
            
            int dx = nextX - curX;
            int dy = nextY - curY;
            if (dx != 0 && dy != 0)
            {
                // 必须检查两个拐角方向的节点是否都可走
                if (!IsWalkable(curX + dx, curY) || !IsWalkable(curX, curY + dy))
                {
                    return false;
                }
            }
            return true;
        }
        
        private float CalculateDiagonalHCost(PathNodeBase startNode, PathNodeBase endNode)
        {
            float dx = Mathf.Abs(startNode.x - endNode.x);
            float dy = Mathf.Abs(startNode.y - endNode.y);
            float D = 1f; // 直线移动代价
            float D2 = Mathf.Sqrt(2); // 对角线移动代价，约1.414
           
            // 公式：D * (dx + dy) + (D2 - 2 * D) * min(dx, dy)
            // 简化后也可以是：D * (max(dx, dy) - min(dx, dy)) + D2 * min(dx, dy)
            return D * Mathf.Abs(dx - dy) + D2 * Mathf.Min(dx, dy);
        }
    }
}