using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;

using UnityEngine;

public class MapGenerator : MonoBehaviour {

    public enum DrawMode { NoiseMap, ColourMap, Mesh };
    public DrawMode drawMode;

    public const int mapChunkSize = 241;
    [Range(0,6)]
    public int editorPreviewLod;
    public float noiseScale;

    public int octaves;
    [Range(0, 1)]
    public float persistence;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

    public TerrainType[] regions;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor() {
        MapData mapData = GenerateMapData();

        // display the texture on the screen
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.noiseMap));
        } else if (drawMode == DrawMode.ColourMap) {
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        } else if (drawMode == DrawMode.Mesh) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.noiseMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLod),
                             TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
    }

    public void RequestMapData(Action<MapData> callback) {
        ThreadStart threadStart = delegate {
            MapDataThread(callback);
	    };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Action<MapData> callback) {
        MapThreadInfo<MapData> mapInfo = new MapThreadInfo<MapData>(callback, GenerateMapData());

        lock (mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue(mapInfo);
        }
    }

    public void RequestMeshData(MapData data, int lod, Action<MeshData> callback) {
        ThreadStart threadStart = delegate {
            MeshDataThread(data, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData data, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(data.noiseMap, meshHeightMultiplier, meshHeightCurve, lod);
        MapThreadInfo<MeshData> meshThreadInfo = new MapThreadInfo<MeshData>(callback, meshData);

        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(meshThreadInfo);
        }
    }

    private void Update() {
        if (mapDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.param);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.param);
            }
        }
    }

    MapData GenerateMapData() {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistence, lacunarity, offset);

        // create a map of colours depending on the height at the given point
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++) {
                    if (currentHeight <= regions[i].height) {
                        colourMap[y * mapChunkSize + x] = regions[i].colour;
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colourMap);
    }

    void OnValidate() {
        if (lacunarity < 1) {
            lacunarity = 1;
        }

        if (octaves < 1) {
            octaves = 1;
        }
    }

    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T param;

        public MapThreadInfo(Action<T> callback, T param) {
            this.callback = callback;
            this.param = param;
        }
    }
}

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color colour;
}

public struct MapData {
    public readonly float[,] noiseMap;
    public readonly Color[] colourMap;

    public MapData(float[,] noiseMap, Color[] colourMap) {
        this.noiseMap = noiseMap;
        this.colourMap = colourMap;
    }
}
