
using UnityEngine;

public class PerlinTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int terrainWidth = 100;
    public int terrainLength = 100;
    public int terrainHeight = 20;
    public int heightmapResolution = 257;

    [Header("Perlin Noise Settings")]
    [Range(1f, 10f)]
    public float noiseScale = 4f;

    [Range(0.1f, 1f)]
    public float heightMultiplier = 0.45f;

    [Range(0f, 0.5f)]
    public float baseHeight = 0.05f;

    public int seed = 42;

    private Terrain terrain;

    [ContextMenu("Generate Perlin Terrain")]
    public void GenerateTerrain()
    {
        terrain = GetComponent<Terrain>();

        if (terrain == null)
        {
            Debug.LogError(
                "PerlinTerrainGenerator needs a Terrain component."
            );
            return;
        }

        TerrainData terrainData = terrain.terrainData;

        terrainData.heightmapResolution = heightmapResolution;
        terrainData.size = new Vector3(
            terrainWidth,
            terrainHeight,
            terrainLength
        );

        int resolution = terrainData.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        // Seed creates a repeatable noise pattern.
        System.Random random = new System.Random(seed);

        float offsetX = (float)random.NextDouble() * 10000f;
        float offsetZ = (float)random.NextDouble() * 10000f;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float sampleX =
                    (float)x / (resolution - 1) * noiseScale + offsetX;

                float sampleZ =
                    (float)z / (resolution - 1) * noiseScale + offsetZ;

                float noise = Mathf.PerlinNoise(sampleX, sampleZ);

                // Store a normalized terrain height between 0 and 1.
                heights[z, x] =
                    baseHeight + noise * heightMultiplier;
            }
        }

        terrainData.SetHeights(0, 0, heights);

        Debug.Log("Perlin Noise terrain generated.");
    }
}
