
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class ProceduralTerrainGenerator : MonoBehaviour
{
    [Header("Map Size")]
    [Min(33)]
    public int heightmapResolution = 513;

    public float mapWidth = 500f;
    public float mapLength = 500f;
    public float maxHeight = 120f;

    [Header("Noise Settings")]
    public int seed = 42;

    [Min(0.001f)]
    public float noiseScale = 0.008f;

    [Range(1, 8)]
    public int octaves = 5;

    [Range(0.1f, 0.9f)]
    public float persistence = 0.5f;

    [Range(1.5f, 3f)]
    public float lacunarity = 2f;

    [Header("Mountain Settings")]
    [Range(0f, 1f)]
    public float mountainStrength = 0.65f;

    public float mountainScale = 0.003f;

    private Terrain terrain;

    void Awake()
    {
        terrain = GetComponent<Terrain>();
        GenerateTerrain();
    }

    [ContextMenu("Generate Terrain")]
    public void GenerateTerrain()
    {
        if (terrain == null)
            terrain = GetComponent<Terrain>();

        TerrainData data = terrain.terrainData;

        data.heightmapResolution = Mathf.ClosestPowerOfTwo(
            Mathf.Clamp(heightmapResolution - 1, 32, 4096)
        ) + 1;

        data.size = new Vector3(mapWidth, maxHeight, mapLength);

        int resolution = data.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        System.Random random = new System.Random(seed);

        float offsetX = (float)random.NextDouble() * 10000f;
        float offsetZ = (float)random.NextDouble() * 10000f;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx = (float)x / (resolution - 1);
                float nz = (float)z / (resolution - 1);

                // Large-scale hills
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;
                float amplitudeSum = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX =
                        (x + offsetX) * noiseScale * frequency;
                    float sampleZ =
                        (z + offsetZ) * noiseScale * frequency;

                    float noise = Mathf.PerlinNoise(sampleX, sampleZ);

                    noiseHeight += noise * amplitude;
                    amplitudeSum += amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                noiseHeight /= amplitudeSum;

                // Large mountain regions
                float mountainNoise = Mathf.PerlinNoise(
                    (nx * mapWidth + offsetX) * mountainScale,
                    (nz * mapLength + offsetZ) * mountainScale
                );

                // Emphasize the high parts to create mountains
                float mountains = Mathf.SmoothStep(
                    0.35f, 0.8f, mountainNoise
                );

                float height = Mathf.Lerp(
                    noiseHeight * 0.25f,
                    noiseHeight,
                    1f - mountainStrength
                );

                height += mountains * mountainStrength * 0.65f;

                heights[z, x] = Mathf.Clamp01(height);
            }
        }

        data.SetHeights(0, 0, heights);
    }
}
