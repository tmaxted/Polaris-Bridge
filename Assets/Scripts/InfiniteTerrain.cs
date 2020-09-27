using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour {

    const float viewerUpdateThreshold = 25f;
    const float sqrViewerUpdateThreshold = viewerUpdateThreshold* viewerUpdateThreshold;

    public LODInfo[] detailLevels;
    public static float maxViewDist;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;

    int chunkSize;
    int chunksVisibleInViewDist; // number of chunks around viewer to instantiate

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> visibleChunks = new List<TerrainChunk>();

    void Start() {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;

        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist) / chunkSize;

        UpdateVisibleChunks();
    }

    private void Update() {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerUpdateThreshold) {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
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
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk {

        GameObject meshObject;
        Vector2 pos;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool mapDataReceived;
        int prevLODInd = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
            this.detailLevels = detailLevels;

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

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++) {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(pos, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData data) {
            this.mapData = data;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk() {
            // find the point on perimeter closest to viewers pos
            // find distance between point and viewer
            // if distance is <= maxViewDist -> enable meshObject
            // if distance is > maxViewDist -> disable meshObject

            if (mapDataReceived) {
                float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDistFromNearestEdge <= maxViewDist;

                if (visible) {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++) {
                        if (viewerDistFromNearestEdge > detailLevels[i].visibleDistanceThreshold) {
                            lodIndex = i + 1;
                        } else {
                            break;
                        }
                    }

                    if (lodIndex != prevLODInd) {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh) {
                            meshFilter.mesh = lodMesh.mesh;
                        } else if (!lodMesh.requestedMesh) {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                }

                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }
    }

    class LODMesh {
        // fetch mesh from map generator

        public Mesh mesh;
        public bool requestedMesh;
        public bool hasMesh;

        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback) {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData data) {
            if (!requestedMesh || !hasMesh) {
                requestedMesh = true;
                mapGenerator.RequestMeshData(data, lod, OnMeshDataReceived);
            }
        }
    }

    [System.Serializable]
    public struct LODInfo {
        public int lod;
        public float visibleDistanceThreshold;
    }
}
