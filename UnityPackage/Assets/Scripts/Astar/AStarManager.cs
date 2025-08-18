using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace AStar
{
    public class AStarManager : IPathfindingManager
    {
        private static AStarManager instance;

        public static AStarManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AStarManager();
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

        void IPathfindingManager.InitMap(int width, int height, int obstacleCount) => InitMap(width, height, obstacleCount);

        #endregion 
        
        
        private int width;
        private int height;
        
        public AStarNode[,] nodesMap;
        
        // 开启列表和关闭列表
        // public List<AStarNode> openList = new List<AStarNode>();
        // public List<AStarNode> closedList = new List<AStarNode>();
        
        // 更好的开启列表和关闭列表，使用优先队列和哈希表
        public PriorityQueue<AStarNode> openList = new PriorityQueue<AStarNode>();
        public HashSet<AStarNode> closedList = new HashSet<AStarNode>();

        private int[,] fourDirections = new int[,] { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };
        private int[,] eightDirections = new int[,] { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 }, { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };

        public void InitMap(int width, int height, int obstacleCount)
        {
            this.width = width;
            this.height = height;
            nodesMap = new AStarNode[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    nodesMap[i, j] = new AStarNode(i, j, NodeType.Open);
                }
            }
            
            // 随机生成障碍物
            for (int i = 0; i < obstacleCount; i++)
            {
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);
                nodesMap[x, y].nodeType = NodeType.Obstacle;
            }
        
        }

        public bool IsInMap(AStarNode node)
        {
            return IsInMap(node.x, node.y);
        }
        
        public bool IsInMap(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        // ====== 老版本的A*算法 ======
        // public void FindPath(AStarNode startNode, AStarNode endNode)
        // {
        //     FindPath(startNode.x, startNode.y, endNode.x, endNode.y);
        // }
        //
        // public List<AStarNode> FindPath(int startX, int startY, int endX, int endY)
        // {
        //     openList.Clear();
        //     closedList.Clear();
        //     
        //     AStarNode startNode = nodesMap[startX, startY];
        //     AStarNode endNode = nodesMap[endX, endY];
        //
        //     if (startNode.nodeType == NodeType.Obstacle || endNode.nodeType == NodeType.Obstacle)
        //     {
        //         Debug.LogError("起始或终止节点不可为障碍物节点！");
        //         return null;
        //     }
        //
        //     if (!IsInMap(startX, startY) || !IsInMap(endX, endY))
        //     {
        //         Debug.LogError("起始或终止节点超出地图范围！");
        //         return null;
        //     }
        //     
        //     // startNode.nodeType = null;
        //     
        //     // 一定要先将起始节点放入关闭列表，否则会陷入死循环
        //     closedList.Add(startNode);
        //
        //     bool success = FindEndNode(startNode, endNode);
        //     // 若找到终点，则开始回溯路径
        //     if (success)
        //     {
        //         List<AStarNode> path = new List<AStarNode>();
        //         AStarNode currNode = closedList[closedList.Count - 1];
        //         while (currNode.parent != null)
        //         {
        //             // Debug.Log("路径节点：" + currNode.x + " " + currNode.y);
        //             path.Add(currNode);
        //             currNode = currNode.parent;
        //         }
        //
        //         path.Reverse();
        //         return path;
        //     }
        //     else
        //     {
        //         Debug.LogError("没有找到路径！");
        //         return null;
        //     }
        //     
        // }
        //
        // private bool FindEndNode(AStarNode startNode, AStarNode endNode)
        // {
        //     return FindEndNode(startNode.x, startNode.y, endNode.x, endNode.y);
        // }
        //
        // private bool FindEndNode(int startX, int startY, int endX, int endY)
        // {
        //     AStarNode startNode = nodesMap[startX, startY];
        //     AStarNode endNode = nodesMap[endX, endY];
        //     
        //     for (int i = -1; i <= 1; i++)
        //     {
        //         for (int j = -1; j <= 1; j++)
        //         {
        //             // 跳过自己遍历周围8个节点
        //             if (i == 0 && j == 0)
        //             {
        //                 continue;
        //             }
        //             int x = startX + i;
        //             int y = startY + j;
        //
        //             // 跳过超出地图范围的节点
        //             if (!IsInMap(x, y))
        //             {
        //                 continue;
        //             }
        //             
        //             AStarNode currNode = nodesMap[x, y];
        //
        //             // 跳过障碍物节点
        //             if (currNode.nodeType == NodeType.Obstacle)
        //             {
        //                 continue;
        //             }
        //             
        //             // 跳过已经在开启、关闭列表的节点
        //             if (openList.Contains(currNode) || closedList.Contains(currNode))
        //             {
        //                 continue;
        //             }
        //             
        //             // 若当前节点为终点，则找到路径
        //             if (currNode.x == endX && currNode.y == endY)
        //             {
        //                 return true;
        //             }
        //             
        //             // ====== 计算寻路代价 ======
        //             currNode.parent = startNode;
        //             
        //             // 计算当前节点到父节点的距离
        //             float distanceSquare = (currNode.x - currNode.parent.x) * (currNode.x - currNode.parent.x) + (currNode.y - currNode.parent.y) * (currNode.y - currNode.parent.y);
        //             
        //             float distance = Mathf.Sqrt(distanceSquare);
        //
        //             // 计算当前节点的G值
        //             float currGCost = currNode.parent.gCost + distance;
        //             
        //             // 计算当前节点的H值（曼哈顿距离）
        //             float currHCost = Mathf.Abs(currNode.x - endX) + Mathf.Abs(currNode.y - endY);
        //             
        //             // 计算当前节点的F值
        //             float currFCost = currGCost + currHCost;
        //             
        //             currNode.gCost = currGCost;
        //             currNode.hCost = currHCost;
        //             currNode.fCost = currFCost;
        //             
        //             // TODO: 跳过F值大于父节点的F值的节点（是否可以优化、剪枝？）
        //             
        //             // 放入开启列表
        //             openList.Add(currNode);
        //             
        //         }
        //     }
        //     
        //     // 开启列表为空，说明没有找到路径
        //     if (openList.Count == 0)
        //     {
        //         Debug.LogError("没有找到路径！");
        //         return false;
        //     }
        //     
        //     // TODO: 可以用堆排序优化，减少排序时间
        //     // 排序，按照F值升序排列，最小的在最前面
        //     openList.Sort((node1, node2) =>
        //     {
        //         return node1.fCost >= node2.fCost ? 1 : -1;
        //     });
        //
        //     AStarNode minNode = openList[0];
        //             
        //     // 将F值最小的节点从开启列表移到关闭列表
        //     openList.Remove(minNode);
        //     closedList.Add(minNode);
        //             
        //     // 递归寻找，以minNode为起点，寻找终点
        //     return FindEndNode(minNode, endNode);
        // }
        
        public List<AStarNode> FindPath(AStarNode startNode, AStarNode endNode)
        {
            return FindPath(startNode.x, startNode.y, endNode.x, endNode.y);
        }
        
        // ====== 新版本的A*算法 ======
        
        public List<AStarNode> FindPath(int startX, int startY, int endX, int endY)
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
            var endNode = nodesMap[endX, endY];

            if (startNode.nodeType == NodeType.Obstacle || endNode.nodeType == NodeType.Obstacle) return null;
            if (!IsInMap(startX, startY) || !IsInMap(endX, endY)) return null;
            
            // 初始化起始节点
            startNode.gCost = 0;
            startNode.hCost = CalculateDiagonalHCost(startNode, endNode); // 可以斜向移动则使用对角线距离，否则使用曼哈顿距离
            startNode.fCost = startNode.gCost + startNode.hCost;
            startNode.parent = null;
            
            openList.Enqueue(startNode);

            while (openList.Count > 0)
            {
                var currNode = openList.Dequeue();
                closedList.Add(currNode);

                // 找到终点
                if (currNode.x == endX && currNode.y == endY)
                {
                    return ReconstructPath(currNode); // 找到了路径，回溯
                }
                
                // 遍历周围节点（四邻或八邻）
                for (int i = 0; i < fourDirections.GetLength(0); i++)
                {
                    int neighbourX = currNode.x + fourDirections[i, 0];
                    int neighbourY = currNode.y + fourDirections[i, 1];

                    // 跳过超出地图范围的节点
                    if (!IsInMap(neighbourX, neighbourY))
                    {
                        continue;
                    }

                    var neighbourNode = nodesMap[neighbourX, neighbourY];

                    // 跳过障碍物节点
                    if (neighbourNode.nodeType == NodeType.Obstacle)
                    {
                        continue;
                    }

                    // 跳过已经在关闭列表的节点
                    if (closedList.Contains(neighbourNode))
                    {
                        continue;
                    }

                    // 计算当前节点到邻居节点的距离
                    float distanceToNeighbour = Mathf.Sqrt((neighbourX - currNode.x) * (neighbourX - currNode.x) + (neighbourY - currNode.y) * (neighbourY - currNode.y));
                    float newGCost = currNode.gCost + distanceToNeighbour;

                    // 重点：如果新路径更优，或者邻居不在开启列表中
                    // (对于PriorityQueue，我们无法直接判断包含，所以简化逻辑)
                    // 我们可以通过比较G值来判断
                    // 注意：一个更简单的策略是，即使它在开启列表中，也直接添加。
                    // PriorityQueue中可能存在同一个节点的多个副本，但只有F值最小的那个会被先处理。
                    // 当我们处理一个节点时，如果它已经在关闭列表里，直接跳过即可。

                    if (newGCost < neighbourNode.gCost || neighbourNode.parent == null) // parent==null可以作为从未被访问过的标志
                    {
                        neighbourNode.gCost = newGCost;
                        neighbourNode.hCost =
                            // CalculateDiagonalHCost(neighbourNode, endNode); // 计算启发函数，八邻，可以斜向移动，使用对角线距离
                            CalculateManhattanHCost(neighbourNode, endNode); // 计算启发函数，四邻，使用曼哈顿距离
                        
                        neighbourNode.fCost = neighbourNode.gCost + neighbourNode.hCost;
                        neighbourNode.parent = currNode;

                        // 将邻居节点加入开启列表（如果已有更差的路径，这会添加一个更优的副本）
                        openList.Enqueue(neighbourNode);
                    }
                }
            }
            // 开启列表为空，说明没有找到路径
            Debug.LogError("没有找到路径！");
            return null;
        }
        
        // 启发函数 H 必须是可接受的（Admissible），即它永远不会高估（overestimate）到达目标的实际成本。
        // 计算启发函数，若可以斜向移动则使用对角线距离，否则使用曼哈顿距离
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
        
        private float CalculateManhattanHCost(PathNodeBase startNode, PathNodeBase endNode)
        {
            float dx = Mathf.Abs(startNode.x - endNode.x);
            float dy = Mathf.Abs(startNode.y - endNode.y);
            return dx + dy;
        }
        
        // 独立的回溯路径方法
        private List<AStarNode> ReconstructPath(AStarNode endNode)
        {
            List<AStarNode> path = new List<AStarNode>();
            AStarNode currentNode = endNode;
            while (currentNode != null)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent as AStarNode;
            }
            path.Reverse();
            return path;
        }
    }
}