﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public static class Noise {

	public static float[, ] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset) {
		float[, ] noiseMap = new float[mapWidth, mapHeight];

		System.Random rng = new System.Random(seed);
		Vector2[] octaveOffsets = new Vector2[octaves];
		for (int i = 0; i < octaves; i++) {
			float offsetX = rng.Next(-100000, 100000) + offset.x;
			float offsetY = rng.Next(-100000, 100000) + offset.y;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);
		}

		if (scale <= 0) {
			scale = 0.0001f;
		}

		float maxNoiseHeight = float.MinValue;
		float minNoiseHeight = float.MaxValue;

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;

		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {

				float amp = 1, freq = 1, noiseHeight = 0;

				for (int i = 0; i < octaves; i++) {
					float sampleX = (x - halfWidth) / scale * freq + octaveOffsets[i].x;
					float sampleY = (y - halfHeight) / scale * freq + octaveOffsets[i].y;

					float perlinVal = Mathf.PerlinNoise(sampleX, sampleY * 2 - 1);
					noiseHeight += perlinVal * amp;

					amp *= persistance;
					freq *= lacunarity;
				}

				if (noiseHeight > maxNoiseHeight) {
					maxNoiseHeight = noiseHeight;
				} else if (noiseHeight < minNoiseHeight) {
					minNoiseHeight = noiseHeight;
				}

				noiseMap[x, y] = noiseHeight;
			}
		}

		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]); // normalise values
			}
		}

		return noiseMap;
	}
}
