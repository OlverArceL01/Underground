using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapNode
{
    public int id;
    public Vector3 position;
    public List<int> connections;
}

public class MineMap : MonoBehaviour
{
    public List<MapNode> nodes = new List<MapNode>();
    private Dictionary<int, MapNode> nodeLookup;

    private void Awake()
    {
        InitializeMap();
        nodeLookup = new Dictionary<int, MapNode>();
        foreach (var node in nodes)
            nodeLookup[node.id] = node;
    }

    private void InitializeMap()
    {
        nodes = new List<MapNode>
        {
            new MapNode { id = 1, position = new Vector3(90.6f, 3.8f, 3.6f), connections = new() { 2 } },
            new MapNode { id = 2, position = new Vector3(-11.8f, 0.79f, 3.16f), connections = new() { 1, 3, 4 } },
            new MapNode { id = 3, position = new Vector3(-21.8f, 22f, 129.4f), connections = new() { 2 } },
            new MapNode { id = 4, position = new Vector3(-318.97f, 1.46f, 0f), connections = new() { 2, 5, 7 } },
            new MapNode { id = 5, position = new Vector3(-352f, 0.7f, -104f), connections = new() { 4, 6 } },
            new MapNode { id = 6, position = new Vector3(-174.7f, -41f, -290.3f), connections = new() { 5 } },
            new MapNode { id = 7, position = new Vector3(-843.18f, 11f, 6.08f), connections = new() { 4, 8, 9 } },
            new MapNode { id = 8, position = new Vector3(-876.5f, 10.6f, -67.7f), connections = new() { 7 } },
            new MapNode { id = 9, position = new Vector3(-1570.7f, 28.7f, 9f), connections = new() { 7, 10, 16 } },
            new MapNode { id = 10, position = new Vector3(-1632.49f, 29.2f, 10.44f), connections = new() { 9, 11, 12 } },
            new MapNode { id = 11, position = new Vector3(-1690.7f, 28.2f, 10.7f), connections = new() { 10 } },
            new MapNode { id = 12, position = new Vector3(-1668.35f, 33.7f, -123.3f), connections = new() { 10, 13 } },
            new MapNode { id = 13, position = new Vector3(-1815.2f, 36.4f, -313f), connections = new() { 12, 14, 15 } },
            new MapNode { id = 14, position = new Vector3(-1890.7f, 51.3f, -296.2f), connections = new() { 13 } },
            new MapNode { id = 15, position = new Vector3(-1831.56f, 36.7f, -401.3f), connections = new() { 13 } },
            new MapNode { id = 16, position = new Vector3(-1578.5f, 25.25f, 256f), connections = new() { 9, 17 } },
            new MapNode { id = 17, position = new Vector3(-1674f, 31f, 394f), connections = new() { 16, 18 } },
            new MapNode { id = 18, position = new Vector3(-1634.1f, 62.7f, 652f), connections = new() { 17, 19 } },
            new MapNode { id = 19, position = new Vector3(-1437.1f, 87.4f, 606.7f), connections = new() { 18, 20 } },
            new MapNode { id = 20, position = new Vector3(-1380.3f, 92.4f, 561.7f), connections = new() { 19, 21, 23 } },
            new MapNode { id = 21, position = new Vector3(-1303.5f, 108.1f, 486f), connections = new() { 20, 22 } },
            new MapNode { id = 22, position = new Vector3(-1318f, 151.63f, 217.4f), connections = new() { 21 } },
            new MapNode { id = 23, position = new Vector3(-1467.6f, 100.9f, 442.9f), connections = new() { 20, 24 } },
            new MapNode { id = 24, position = new Vector3(-1435f, 118.3f, 118.1f), connections = new() { 23 } }
        };   
    }

    public MapNode GetNode(int id)
    {
        return nodeLookup.TryGetValue(id, out var node) ? node : null;
    }
}