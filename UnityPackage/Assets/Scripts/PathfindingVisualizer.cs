using System.Collections;
using System.Collections.Generic;
using AStar;
using BStar;
using Dijkstra;
using JPS;
using UnityEngine;
using Utility;

public class PathfindingVisualizer : MonoBehaviour
{
    [SerializeField]
    private GameObject mapCubeParent;
    
    [SerializeField]
    private int width = 10;
    [SerializeField]
    private int height = 10;
    
    [SerializeField]
    private int ObstacleCount = 10;
    
    private bool isFirstClick = true;
    
    private Vector2 startPos;
    private Vector2 endPos;
    
    private Dictionary<string, GameObject> mapCubes;
    
    private IPathfindingManager pathfindingManager;

    private bool isEditingObstacle = false; // 是否处于障碍物编辑模式

    private void Start()
    {
        // pathfindingManager = AStarManager.Instance;
        // pathfindingManager = DijkstraManager.Instance;
        // pathfindingManager = BStarManager.Instance;
        pathfindingManager = JPSManager.Instance;
        pathfindingManager.InitMap(width, height, ObstacleCount);
        StartCoroutine(CreateMapCubes());
    }

    IEnumerator CreateMapCubes()
    {
        var nodes = pathfindingManager.nodesMap;
        mapCubes = new Dictionary<string, GameObject>(width * height);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = mapCubeParent.transform;
                cube.transform.position = new Vector3(i + 0.1f * i, 0, j + 0.1f * j);
                cube.name = i + "_" + j;
                mapCubes.Add(cube.name, cube);

                var node = nodes[i, j];
                if (node.nodeType == NodeType.Obstacle)
                {
                    cube.GetComponent<MeshRenderer>().material.color = Color.gray;
                }
                else
                {
                    cube.GetComponent<MeshRenderer>().material.color = Color.white;
                }

                yield return null;
            }
        }
    }

    private void Update()
    {
        // 空格键：清除路径、起点、终点
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ClearPathAndPoints();
            return;
        }
        // R键：重建地图并清空状态
        if (Input.GetKeyDown(KeyCode.R))
        {
            RegenerateMap();
            return;
        }
        // S键：切换障碍物编辑模式
        if (Input.GetKeyDown(KeyCode.S))
        {
            isEditingObstacle = !isEditingObstacle;
            if (isEditingObstacle)
            {
                ClearAllObstacles();
                Debug.Log("进入障碍物编辑模式");
            }
            else
            {
                Debug.Log("退出障碍物编辑模式");
                // 退出编辑模式时，重置起点终点选择状态
                isFirstClick = true;
                startPos = Vector2.zero;
                endPos = Vector2.zero;
            }
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                var go = hit.transform.gameObject;
                var names = go.name.Split('_');
                int x = int.Parse(names[0]);
                int y = int.Parse(names[1]);

                if (isFirstClick)
                {
                    Debug.Log("Start position: " + x + ", " + y);
                    startPos = new Vector2(x, y);
                    isFirstClick = false;

                    string cubeName = (int)startPos.x + "_" + (int)startPos.y;
                    GameObject cube = mapCubes[cubeName];
                    cube.GetComponent<MeshRenderer>().material.color = Color.green;
                }
                else
                {
                    Debug.Log("End position: " + x + ", " + y);
                    endPos = new Vector2(x, y);

                    var path = pathfindingManager.FindPath((int)startPos.x, (int)startPos.y, (int)endPos.x, (int)endPos.y);
                    var closedList = pathfindingManager.closedList;
                    var nodes = pathfindingManager.nodesMap;
                    // 先全部重置
                    for (int i = 0; i < width; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            string cubeName = i + "_" + j;
                            var node = nodes[i, j];
                            var cube = mapCubes[cubeName];
                            if (node.nodeType == NodeType.Obstacle)
                            {
                                cube.GetComponent<MeshRenderer>().material.color = Color.gray;
                            }
                            else if (closedList.Contains(node))
                            {
                                cube.GetComponent<MeshRenderer>().material.color = new Color(0.5f, 0.8f, 1f, 1f); // 淡蓝色
                            }
                            else
                            {
                                cube.GetComponent<MeshRenderer>().material.color = Color.white;
                            }
                        }
                    }
                    // 路径上色
                    if (path != null)
                    {
                        for (int i = 0; i < path.Count; i++)
                        {
                            string cubeName = path[i].x + "_" + path[i].y;
                            GameObject cube = mapCubes[cubeName];
                            cube.GetComponent<MeshRenderer>().material.color = new Color(1f, 0.5f, 0f, 1f); // 橙色
                        }
                        // 起点终点特殊上色
                        string startCubeName = (int)startPos.x + "_" + (int)startPos.y;
                        string endCubeName = (int)endPos.x + "_" + (int)endPos.y;
                        mapCubes[startCubeName].GetComponent<MeshRenderer>().material.color = Color.green;
                        mapCubes[endCubeName].GetComponent<MeshRenderer>().material.color = Color.red;
                    }
                    else
                    {
                        Debug.Log("No path found!");
                    }
                }
            }
        }
        // 编辑障碍物模式下，鼠标左键点击切换障碍物
        if (isEditingObstacle)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    var go = hit.transform.gameObject;
                    var names = go.name.Split('_');
                    int x = int.Parse(names[0]);
                    int y = int.Parse(names[1]);
                    var nodes = pathfindingManager.nodesMap;
                    string cubeName = x + "_" + y;
                    GameObject cube = mapCubes[cubeName];
                    if (nodes[x, y].nodeType == NodeType.Obstacle)
                    {
                        nodes[x, y].nodeType = NodeType.Open;
                        cube.GetComponent<MeshRenderer>().material.color = Color.white;
                    }
                    else
                    {
                        nodes[x, y].nodeType = NodeType.Obstacle;
                        cube.GetComponent<MeshRenderer>().material.color = Color.gray;
                    }
                }
            }
        }
    }

    // 清除路径、起点、终点颜色，障碍物不变
    private void ClearPathAndPoints()
    {
        var nodes = pathfindingManager.nodesMap;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                string cubeName = i + "_" + j;
                if (mapCubes.TryGetValue(cubeName, out GameObject cube))
                {
                    if (nodes[i, j].nodeType == NodeType.Obstacle)
                    {
                        cube.GetComponent<MeshRenderer>().material.color = Color.gray;
                    }
                    else
                    {
                        cube.GetComponent<MeshRenderer>().material.color = Color.white;
                    }
                }
            }
        }
        isFirstClick = true;
        startPos = Vector2.zero;
        endPos = Vector2.zero;
    }

    // 重新生成地图，销毁旧Cube，重建新Cube，重置状态
    private void RegenerateMap()
    {
        // 销毁所有Cube
        if (mapCubes != null)
        {
            foreach (var kv in mapCubes)
            {
                if (kv.Value != null)
                {
                    Destroy(kv.Value);
                }
            }
        }
        mapCubes = null;
        // 重新生成地图数据
        pathfindingManager.InitMap(width, height, ObstacleCount);
        // 重置状态
        isFirstClick = true;
        startPos = Vector2.zero;
        endPos = Vector2.zero;
        // 重新生成Cube
        StartCoroutine(CreateMapCubes());
    }

    // 清空所有障碍物（地图和可视化）
    private void ClearAllObstacles()
    {
        var nodes = pathfindingManager.nodesMap;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                nodes[i, j].nodeType = NodeType.Open;
                string cubeName = i + "_" + j;
                if (mapCubes.TryGetValue(cubeName, out GameObject cube))
                {
                    cube.GetComponent<MeshRenderer>().material.color = Color.white;
                }
            }
        }
    }
}