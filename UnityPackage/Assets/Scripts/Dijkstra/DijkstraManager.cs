using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Dijkstra
{
    public class DijkstraManager : IPathfindingManager
    {
        private static DijkstraManager instance;

        public static DijkstraManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DijkstraManager();
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

        public DijkstraNode[,] nodesMap;
        // 更好的开启列表和关闭列表，使用优先队列和哈希表
        public PriorityQueue<DijkstraNode> openList = new PriorityQueue<DijkstraNode>();
        public HashSet<DijkstraNode> closedList = new HashSet<DijkstraNode>();
        
        private int[,] fourDirections = new int[,] { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };
        private int[,] eightDirections = new int[,] { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 }, { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };
        
        public void InitMap(int width, int height, int obstacleCount)
        {
            this.width = width;
            this.height = height;
            
            nodesMap = new DijkstraNode[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    nodesMap[i, j] = new DijkstraNode(i, j, NodeType.Open);
                }
            }
            
            // 随机生成障碍物逻辑不变
            for (int i = 0; i < obstacleCount; i++)
            {
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);
                nodesMap[x, y].nodeType = NodeType.Obstacle;
            }
        }
        
        public bool IsInMap(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public List<DijkstraNode> FindPath(int startX, int startY, int endX, int endY)
        {
            openList.Clear();
            closedList.Clear();
            
            // 重置所有节点的gCost和parent
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    var node = nodesMap[i, j];
                    node.gCost = float.MaxValue;
                    node.parent = null;
                }
            }
            
            var startNode = nodesMap[startX, startY];
            var endNode = nodesMap[endX, endY];
            
            if (startNode.nodeType == NodeType.Obstacle || endNode.nodeType == NodeType.Obstacle) return null;
            if (!IsInMap(startX, startY) || !IsInMap(endX, endY)) return null;
            
            // 初始化起始节点
            startNode.gCost = 0;
            startNode.parent = null;
            
            openList.Enqueue(startNode);

            while (openList.Count > 0)
            {
                var currNode = openList.Dequeue();
                closedList.Add(currNode);

                if (currNode == endNode)
                {
                    return ReconstructPath(currNode);
                }

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

                    // 计算新路径的gCost
                    float distanceToNeighbour = Mathf.Sqrt((neighbourX - currNode.x) * (neighbourX - currNode.x) + (neighbourY - currNode.y) * (neighbourY - currNode.y));
                    float newGCost = currNode.gCost + distanceToNeighbour;
                    
                    // --- 与A*算法不同，Dijkstra算法不使用启发式函数，只使用实际距离 ---
                    // 这里没有 H 和 F 的概念了，直接比较 gCost
                    if (newGCost < neighbourNode.gCost)
                    {
                        neighbourNode.gCost = newGCost;
                        neighbourNode.parent = currNode;
                        
                        openList.Enqueue(neighbourNode);
                    }
                }
            }
            // 开启列表为空，说明没有找到路径
            Debug.LogError("没有找到路径！");
            return null;
        }

        private List<DijkstraNode> ReconstructPath(DijkstraNode endNode)
        {
            List<DijkstraNode> path = new List<DijkstraNode>();
            DijkstraNode currentNode = endNode;
            while (currentNode != null)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent as DijkstraNode;
            }

            path.Reverse();
            return path;
        }

    
    }
}