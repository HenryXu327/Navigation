using System.Collections.Generic;

namespace Utility
{
    public interface IPathfindingManager
    {
        PathNodeBase[,] nodesMap { get; }
        HashSet<PathNodeBase> closedList { get; }
        List<PathNodeBase> FindPath(int startX, int startY, int endX, int endY);
        void InitMap(int width, int height, int obstacleCount);
    }
} 