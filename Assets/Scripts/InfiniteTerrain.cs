using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour {
    public const float maxViewDist = 300;
    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    static MapGenerator mapGenerator;

    int chunkSize;
    int chunksVisibleInViewDist; // number of chunks around viewer to instantiate

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> visibleChunks = new List<TerrainChunk>();

    void Start() {
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist) / chunkSize;
        mapGenerator = FindObjectOfType<MapGenerator>();
    }

    private void Update() {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks() {
        for (int i = 0; i < visibleChunks.Count; i++) {
            visibleChunks[i].SetVisible(false);
        }
        visibleChunks.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();

                    if (terrainChunkDictionary[viewedChunkCoord].IsVisible()) {
                        visibleChunks.Add(terrainChunkDictionary[viewedChunkCoord]);
                    }
                } else {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk {

        GameObject meshObject;
        Vector2 pos;
        Bounds bounds;

        MapData mapData;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        public TerrainChunk(Vector2 coord, int size, Transform parent, Material material) {
            pos = coord * size;
            Vector3 posV3 = new Vector3(pos.x, 0, pos.y);
            bounds = new Bounds(pos, Vector2.one * size);


            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshFilter = meshObject.AddComponent<MeshFilter>();

            meshObject.transform.position = posV3;
            meshObject.transform.parent = parent;
            SetVisible(false); // default disabled

            mapGenerator.RequestMapData(OnMapDataReceived);
        }

        void OnMapDataReceived(MapData data) {
            mapGenerator.RequestMeshData(data, OnMeshDataReceived);
        }

        void OnMeshDataReceived(MeshData data) {
            meshFilter.mesh = data.CreateMesh();
        }

        public void UpdateTerrainChunk() {
            // find the point on perimeter closest to viewers pos
            // find distance between point and viewer
            // if distance is <= maxViewDist -> enable meshObject
            // if distance is > maxViewDist -> disable meshObject

            float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistFromNearestEdge <= maxViewDist;
            SetVisible(visible);
        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }
    }
}
