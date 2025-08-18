using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace BStar
{
    public class BStarManager : IPathfindingManager
    {
        private static BStarManager instance;
        public static BStarManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BStarManager();
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
        
        private BStarNode[,] nodesMap;
        
        // TODO: 来个关闭列表用来观察算法过程
        public HashSet<BStarNode> closedList = new HashSet<BStarNode>();
        
        public void InitMap(int width, int height, int obstacleCount)
        {
            this.width = width;
            this.height = height;
            nodesMap = new BStarNode[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    nodesMap[i, j] = new BStarNode(i, j, NodeType.Open);
                }
            }
            
            for (int i = 0; i < obstacleCount; i++)
            {
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);
                if (nodesMap[x,y] != null) nodesMap[x, y].nodeType = NodeType.Obstacle;
            }
        }
        
        public bool IsInMap(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public List<BStarNode> FindPath(int startX, int startY, int endX, int endY)
        {
            // 存放已完全探索过的节点，防止重复和死循环
            closedList.Clear();
            
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    var node = nodesMap[i, j];
                    node.parent = null;
                }
            }
            
            var startNode = nodesMap[startX, startY];
            var endNode = nodesMap[endX, endY];
        
            if (startNode.nodeType == NodeType.Obstacle || endNode.nodeType == NodeType.Obstacle) return null;
            if (!IsInMap(startX, startY) || !IsInMap(endX, endY)) return null;
            
            // 存放所有活跃的探索者
            var activeExplorers = new List<BStarExplorer>();
            
            // 步骤1: 起始，探索节点为自由节点
            Vector2Int initialDirection = new Vector2Int(endX - startX, endY - startY);
            BStarExplorer initialExplorer = new BStarExplorer(startNode, ExplorerState.Free, GetPrimaryDirection(initialDirection));
            activeExplorers.Add(initialExplorer);
            
            // 设置父节点为空，方便回溯时判断起点
            startNode.parent = null;
            
            int maxIterations = width * height; // 防止死循环的保险措施
            
            for (int i = 0; i < maxIterations && activeExplorers.Count > 0; i++)
            {
                // 这里应该是开放列表 (Open List)，不然最多同时两个explorer
                var nextGenerationExplorers = new List<BStarExplorer>();
        
                foreach (var explorer in activeExplorers)
                {
                    // 步骤4: 判断是否到达目标
                    if (explorer.currentNode == endNode)
                    {
                        return ReconstructPath(endNode);
                    }
                    
                    closedList.Add(explorer.currentNode);
        
                    if (explorer.state == ExplorerState.Free)
                    {
                        // 步骤2: 自由节点，探索前方的节点，判断是否为障碍
                        Vector2Int preferredDirection = GetPrimaryDirection(new Vector2Int(endX - explorer.currentNode.x, endY - explorer.currentNode.y));
                        BStarNode preferredNode = GetNodeInDirection(explorer.currentNode, preferredDirection);
        
                        if (preferredNode != null && preferredNode.nodeType != NodeType.Obstacle)
                        {
                            // 情况a: 不是障碍，继续前进
                            if (!closedList.Contains(preferredNode))
                            {
                                preferredNode.parent = explorer.currentNode;
                                explorer.currentNode = preferredNode;
                                explorer.lastMoveDirection = preferredDirection;
                                nextGenerationExplorers.Add(explorer);
                            }
                        }
                        else
                        {
                            // 情况b: 是障碍，分裂成两个绕墙爬行节点
                            // 计算出左转和右转的方向
                            // Vector2Int leftTurnDirection = GetLeftPerpendicular(explorer.lastMoveDirection);
                            // Vector2Int rightTurnDirection = GetRightPerpendicular(explorer.lastMoveDirection);
                            Vector2Int leftTurnDirection = GetLeftPerpendicular(preferredDirection);
                            Vector2Int rightTurnDirection = GetRightPerpendicular(preferredDirection);
        
                            // 左边的探索者是右手扶墙
                            var leftExplorer =
                                new BStarExplorer(explorer.currentNode, ExplorerState.Crawling,
                                    leftTurnDirection) { crawlDirection = CrawlingDirection.Right };
                            // 右边的探索者是左手扶墙
                            var rightExplorer =
                                new BStarExplorer(explorer.currentNode, ExplorerState.Crawling,
                                    rightTurnDirection) { crawlDirection = CrawlingDirection.Left };
                            
                            nextGenerationExplorers.Add(leftExplorer);
                            
                            // 如果左右方向不同，才添加右探索者，避免在角落里生成两个方向一样的探索者
                            if (leftTurnDirection != rightTurnDirection) 
                            {
                                nextGenerationExplorers.Add(rightExplorer);
                            }
                        }
                    }
                    else if (explorer.state == ExplorerState.Crawling)
                    {
                        // 步骤3: 绕墙爬行
                        var nextMoveNode = FindCrawlingMoveNodeFourDirections(explorer, endX, endY);
        
                        if (nextMoveNode != null)
                        {
                            nextMoveNode.parent = explorer.currentNode;
                            explorer.currentNode = nextMoveNode;
                            
                            closedList.Add(nextMoveNode); // 立即加入closed，防止其他分支马上又走回来
                            
                            // 判断是否可以变回自由节点，只有当“实际选择的方向”就是“新位置上的主方向”时，才变回自由节点
                            Vector2Int preferredDirection = GetPrimaryDirection(new Vector2Int(endX - explorer.currentNode.x, endY - explorer.currentNode.y));
                            
                            if (explorer.lastMoveDirection == preferredDirection)
                            {
                                explorer.state = ExplorerState.Free;
                            }
                            // 否则，保持 Crawling 状态，继续绕行
                            
                            nextGenerationExplorers.Add(explorer);
                        }
                        // 如果没有找到可移动的路(FindCrawlingMove返回null)，则该探索者分支死亡
                    }
                }
                
                activeExplorers = nextGenerationExplorers;
            }
            
            // 步骤5: 探索节点没有了，寻路结束
            Debug.LogError("没有找到路径！");
            return null;
        }
        
        // public List<BStarNode> FindPath(int startX, int startY, int endX, int endY)
        // {
        //     // --- 数据初始化 (保持不变) ---
        //     closedList.Clear();
        //     for (int i = 0; i < width; i++)
        //     {
        //         for (int j = 0; j < height; j++)
        //         {
        //             nodesMap[i, j].parent = null;
        //         }
        //     }
        //
        //     var startNode = nodesMap[startX, startY];
        //     var endNode = nodesMap[endX, endY];
        //
        //     if (startNode.nodeType == NodeType.Obstacle || endNode.nodeType == NodeType.Obstacle) return null;
        //     if (!IsInMap(startX, startY) || !IsInMap(endX, endY)) return null;
        //
        //     // --- 核心修改：从 List<T> 的“代际模型” 变为 Queue<T> 的“开放列表模型” ---
        //
        //     // 1. 使用队列（Queue）作为我们的“开放列表”，存放所有待处理的探索者
        //     var openList = new Queue<BStarExplorer>();
        //
        //     // 2. 创建并加入第一个探索者
        //     Vector2Int initialDirection = GetPrimaryDirection(new Vector2Int(endX - startX, endY - startY));
        //     BStarExplorer initialExplorer = new BStarExplorer(startNode, ExplorerState.Free, initialDirection);
        //     openList.Enqueue(initialExplorer); // 入队
        //
        //     startNode.parent = null;
        //     closedList.Add(startNode); // 起点一开始就视为已访问
        //
        //     int maxIterations = width * height * 2; // 安全措施
        //     int currentIter = 0;
        //
        //     // 3. 循环条件变为“只要开放列表里还有待处理的探索者”
        //     while (openList.Count > 0 && currentIter < maxIterations)
        //     {
        //         currentIter++;
        //
        //         // 4. 从队列中取出下一个要处理的探索者
        //         var explorer = openList.Dequeue(); // 出队
        //
        //         // 判断是否到达目标 (在处理前就判断，效率更高)
        //         if (explorer.currentNode == endNode)
        //         {
        //             Debug.Log("寻路成功！迭代次数: " + currentIter);
        //             return ReconstructPath(endNode);
        //         }
        //
        //         // --- 探索者状态处理逻辑 (与之前类似，但结果是加入到同一个开放列表中) ---
        //
        //         if (explorer.state == ExplorerState.Free)
        //         {
        //             Vector2Int preferredDirection = GetPrimaryDirection(new Vector2Int(endX - explorer.currentNode.x, endY - explorer.currentNode.y));
        //             BStarNode preferredNode = GetNodeInDirection(explorer.currentNode, preferredDirection);
        //
        //             if (preferredNode != null && preferredNode.nodeType != NodeType.Obstacle && !closedList.Contains(preferredNode))
        //             {
        //                 // a: 不是障碍，继续前进
        //                 preferredNode.parent = explorer.currentNode;
        //                 closedList.Add(preferredNode);
        //         
        //                 explorer.currentNode = preferredNode;
        //                 explorer.lastMoveDirection = preferredDirection;
        //         
        //                 // 5a. 把更新后的自己再放回队列，等待下一轮处理
        //                 openList.Enqueue(explorer); 
        //             }
        //             else
        //             {
        //                 // b: 是障碍，分裂
        //                 Vector2Int leftTurnDirection = GetLeftPerpendicular(preferredDirection);
        //                 Vector2Int rightTurnDirection = GetRightPerpendicular(preferredDirection);
        //
        //                 var leftExplorer = new BStarExplorer(explorer.currentNode, ExplorerState.Crawling, leftTurnDirection) { crawlDirection = CrawlingDirection.Right };
        //                 var rightExplorer = new BStarExplorer(explorer.currentNode, ExplorerState.Crawling, rightTurnDirection) { crawlDirection = CrawlingDirection.Left };
        //         
        //                 // 5b. 把分裂出的新探索者加入队列
        //                 openList.Enqueue(leftExplorer);
        //                 if (leftTurnDirection != rightTurnDirection)
        //                 {
        //                     openList.Enqueue(rightExplorer);
        //                 }
        //             }
        //         }
        //         else // explorer.state == ExplorerState.Crawling
        //         {
        //             var nextMoveNode = FindCrawlingMoveNodeFourDirections(explorer, endX, endY);
        //
        //             if (nextMoveNode != null)
        //             {
        //                 nextMoveNode.parent = explorer.currentNode;
        //                 closedList.Add(nextMoveNode);
        //
        //                 explorer.currentNode = nextMoveNode;
        //         
        //                 Vector2Int mainDirectionFromNewPos = GetPrimaryDirection(new Vector2Int(endX - explorer.currentNode.x, endY - explorer.currentNode.y));
        //                 if (explorer.lastMoveDirection == mainDirectionFromNewPos)
        //                 {
        //                     explorer.state = ExplorerState.Free;
        //                 }
        //         
        //                 // 5c. 把更新后的自己再放回队列
        //                 openList.Enqueue(explorer);
        //             }
        //             // 如果分裂出的探索者无路可走，它就不会被重新加入队列，从而自然消亡
        //         }
        //     }
        //
        //     Debug.LogError("没有找到路径或达到最大迭代次数！");
        //     return null;
        // }
        
        // "沿墙"逻辑: 严格遵循“左手/右手”原则，找到下一个可移动的节点
        private BStarNode FindCrawlingMoveNodeFourDirections(BStarExplorer explorer, int endX, int endY)
        {
            // 优先级1：尝试变回“自由节点”
            // 在绕行时，我们随时检查是否可以直接朝向目标
            Vector2Int mainDirection = GetPrimaryDirection(new Vector2Int(endX - explorer.currentNode.x, endY - explorer.currentNode.y));
            var mainDirectionNode = GetNodeInDirection(explorer.currentNode, mainDirection);
            if (mainDirectionNode != null && mainDirectionNode.nodeType != NodeType.Obstacle && mainDirectionNode != explorer.currentNode.parent)
            {
                // 如果主方向可行，则优先选择主方向，这是脱离绕墙状态的最好机会
                return mainDirectionNode;
            }
    
            // 优先级2：如果不能变回自由，则严格执行“沿墙法则”
            
            // 定义相对于 lastMoveDirection 的4个方向的旋转顺序
            // 按逆时针顺序：上, 左, 下, 右
            Vector2Int[] directions = {
                new Vector2Int(0, 1), 
                new Vector2Int(-1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(1, 0)
            };
            
            int lastDirectionIndex = System.Array.FindIndex(directions, d => d == explorer.lastMoveDirection);
            if (lastDirectionIndex == -1) lastDirectionIndex = 0; // 安全保护
            
            // 构建一个包含4个方向的优先级列表，顺序由绕行方向决定
            var prioritizedTurns = new List<Vector2Int>();
            
            if (explorer.crawlDirection == CrawlingDirection.Left) // 左手扶墙，优先向左转
            {
                // 尝试顺序: 左转 -> 直行 -> 右转 -> 掉头 (逆时针)
                for (int i = 0; i < 4; i++)
                {
                    prioritizedTurns.Add(directions[(lastDirectionIndex + 1 - i + 4) % 4]);
                }
            }
            else // 右手扶墙，优先向右转
            {
                // 尝试顺序: 右转 -> 直行 -> 左转 -> 掉头 (顺时针)
                for (int i = 0; i < 4; i++)
                {
                    prioritizedTurns.Add(directions[(lastDirectionIndex - 1 + i + 4) % 4]);
                }
            }
    
            // --- 遍历优先级列表，找到第一个可行的移动方案 ---
            foreach (var moveDirection in prioritizedTurns)
            {
                // 忽略无效的移动方向 (例如，当与目标在同一条直线上时，第二主方向可能为(0,0))
                if (moveDirection == Vector2Int.zero) continue;

                var nextNode = GetNodeInDirection(explorer.currentNode, moveDirection);
        
                // 检查下一个节点是否有效（存在、不是障碍、不是刚走过的回头路，也不是已经遍历过的节点）
                if (nextNode != null 
                    && nextNode.nodeType != NodeType.Obstacle 
                    && nextNode != explorer.currentNode.parent 
                    && !closedList.Contains(nextNode))
                {
                    // 找到了可行的移动！
                    // 更新探索者的最后移动方向
                    explorer.lastMoveDirection = moveDirection;
                    // 返回找到的节点
                    return nextNode;
                }
            }
    
            // 如果所有优先尝试都失败了，说明被困住，无路可走
            return null;
        }
        
        // 获取主方向（贪心）
        private Vector2Int GetPrimaryDirection(Vector2Int direction)
        {
            // 选择绝对值最大的方向作为主方向
            // System.Math.Sign返回值的符号，即正负号
            return Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? 
                new Vector2Int(System.Math.Sign(direction.x), 0) : 
                new Vector2Int(0, System.Math.Sign(direction.y));
        }
        
        // 获取第二主方向（即沿较短的轴移动的方向）
        private Vector2Int GetSecondaryDirection(Vector2Int vector)
        {
            // 注意，这里的判断条件与 GetPrimaryDirection 相反
            return Mathf.Abs(vector.x) > Mathf.Abs(vector.y) ?
                // 如果X轴距离更长，则第二主方向是Y轴方向
                new Vector2Int(0, System.Math.Sign(vector.y)) :
                // 如果Y轴距离更长（或相等），则第二主方向是X轴方向
                new Vector2Int(System.Math.Sign(vector.x), 0);
        }
        
        // 获取给定方向左侧90度的垂直方向
        private Vector2Int GetLeftPerpendicular(Vector2Int direction)
        {
            // (x, y) 的左垂直方向是 (-y, x)
            return new Vector2Int(-direction.y, direction.x);
        }

        // 获取给定方向右侧90度的垂直方向
        private Vector2Int GetRightPerpendicular(Vector2Int direction)
        {
            // (x, y) 的右垂直方向是 (y, -x)
            return new Vector2Int(direction.y, -direction.x);
        }
        
        // 获取一个方向的反方向
        private Vector2Int GetReverseDirection(Vector2Int direction)
        {
            return new Vector2Int(-direction.x, -direction.y);
        }
        
        // 获取一个节点某方向的邻居节点（此此方向应该为单位向量）
        private BStarNode GetNodeInDirection(BStarNode node, Vector2Int direction)
        {
            int x = node.x + direction.x;
            int y = node.y + direction.y;
            if (IsInMap(x, y))
                return nodesMap[x, y];
            
            return null;
        }
        
        private List<BStarNode> ReconstructPath(BStarNode endNode)
        {
            List<BStarNode> path = new List<BStarNode>();
            BStarNode currentNode = endNode;
            while (currentNode != null)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent as BStarNode;
            }
            path.Reverse();
            return path;
        }
    }
}