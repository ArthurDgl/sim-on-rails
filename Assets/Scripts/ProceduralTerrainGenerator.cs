#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Biome
{
    public string name = "Biome";

    [Header("Climate (where this biome appears)")]
    [Range(0f, 1f)] public float temperature = 0.5f; // 0 = cold, 1 = hot
    [Range(0f, 1f)] public float moisture = 0.5f;    // 0 = dry,  1 = wet

    [Header("Shape")]
    [Range(0f, 1f)] public float baseHeight = 0.2f;       // average elevation
    [Range(0f, 1f)] public float heightVariation = 0.3f;  // how bumpy
    [Range(0.1f, 0.9f)] public float persistence = 0.5f;  // roughness
    [Range(0f, 1f)] public float mountainInfluence = 0f;  // 0 = flat-ish, 1 = mountains allowed

    [Header("Look")]
    public TerrainLayer layer; // texture painted for this biome (optional)
}

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

    [Range(1.5f, 3f)]
    public float lacunarity = 2f;

    [Header("Mountain Settings")]
    [Range(0f, 1f)]
    public float mountainStrength = 0.65f;

    public float mountainScale = 0.003f;

    [Header("Biomes")]
    [Tooltip("Size of biome regions. Smaller = bigger biomes.")]
    [Min(0.0001f)]
    public float climateScale = 0.0025f;

    [Tooltip("Pushes climate values toward the extremes so all biomes appear.")]
    [Range(1f, 4f)]
    public float climateContrast = 2f;

    [Tooltip("Higher = sharper borders between biomes, lower = softer blending.")]
    [Range(1f, 12f)]
    public float blendSharpness = 4f;

    public List<Biome> biomes = new List<Biome>
    {
        new Biome { name = "Desert",    temperature = 0.9f, moisture = 0.1f, baseHeight = 0.12f, heightVariation = 0.10f, persistence = 0.40f, mountainInfluence = 0.1f },
        new Biome { name = "Plains",    temperature = 0.6f, moisture = 0.4f, baseHeight = 0.18f, heightVariation = 0.12f, persistence = 0.45f, mountainInfluence = 0.2f },
        new Biome { name = "Forest",    temperature = 0.5f, moisture = 0.8f, baseHeight = 0.22f, heightVariation = 0.25f, persistence = 0.50f, mountainInfluence = 0.4f },
        new Biome { name = "Tundra",    temperature = 0.1f, moisture = 0.3f, baseHeight = 0.25f, heightVariation = 0.20f, persistence = 0.50f, mountainInfluence = 0.7f },
        new Biome { name = "Highlands", temperature = 0.2f, moisture = 0.8f, baseHeight = 0.30f, heightVariation = 0.35f, persistence = 0.55f, mountainInfluence = 1.0f },
    };

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

        if (biomes == null || biomes.Count == 0)
        {
            Debug.LogWarning("ProceduralTerrainGenerator: add at least one biome.");
            return;
        }

        // Safety net: the Terrain lost its TerrainData asset (e.g. after a bad merge)
        if (terrain.terrainData == null)
        {
            Debug.LogWarning("ProceduralTerrainGenerator: the Terrain has no TerrainData assigned, creating a temporary one. Reassign your TerrainData asset to keep it saved.");
            TerrainData created = new TerrainData();
            terrain.terrainData = created;

            TerrainCollider col = GetComponent<TerrainCollider>();
            if (col != null)
                col.terrainData = created;
        }

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
        // Separate offsets so temperature / moisture / mountains are independent
        float tempOffX = (float)random.NextDouble() * 10000f;
        float tempOffZ = (float)random.NextDouble() * 10000f;
        float moistOffX = (float)random.NextDouble() * 10000f;
        float moistOffZ = (float)random.NextDouble() * 10000f;

        float[] weights = new float[biomes.Count];

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx = (float)x / (resolution - 1);
                float nz = (float)z / (resolution - 1);
                float worldX = nx * mapWidth;
                float worldZ = nz * mapLength;

                ComputeBiomeWeights(worldX, worldZ,
                    tempOffX, tempOffZ, moistOffX, moistOffZ, weights);

                float mountainNoise = Mathf.PerlinNoise(
                    (worldX + offsetX) * mountainScale,
                    (worldZ + offsetZ) * mountainScale
                );
                float mountains = Mathf.SmoothStep(0.35f, 0.8f, mountainNoise);

                float height = 0f;

                for (int b = 0; b < biomes.Count; b++)
                {
                    if (weights[b] <= 0.0001f) continue;

                    Biome biome = biomes[b];

                    float n = Fbm(worldX + offsetX, worldZ + offsetZ,
                        noiseScale, octaves, biome.persistence, lacunarity);

                    float h = biome.baseHeight
                              + (n - 0.5f) * 2f * biome.heightVariation
                              + mountains * mountainStrength * 0.65f * biome.mountainInfluence;

                    height += h * weights[b]; // blend heights across biome borders
                }

                heights[z, x] = Mathf.Clamp01(height);
            }
        }

        data.SetHeights(0, 0, heights);

        PaintBiomes(data, tempOffX, tempOffZ, moistOffX, moistOffZ);
    }

    // ---------- Biomes ----------

    void ComputeBiomeWeights(float worldX, float worldZ,
        float tOffX, float tOffZ, float mOffX, float mOffZ, float[] weights)
    {
        float temperature = Climate(worldX + tOffX, worldZ + tOffZ);
        float moisture = Climate(worldX + mOffX, worldZ + mOffZ);

        float sum = 0f;
        for (int i = 0; i < biomes.Count; i++)
        {
            float dt = temperature - biomes[i].temperature;
            float dm = moisture - biomes[i].moisture;
            float dist = Mathf.Sqrt(dt * dt + dm * dm);

            // Closer in climate space = heavier weight
            float w = Mathf.Pow(1f / (dist + 0.02f), blendSharpness);
            weights[i] = w;
            sum += w;
        }

        for (int i = 0; i < weights.Length; i++)
            weights[i] /= sum;
    }

    float Climate(float x, float z)
    {
        float v = Fbm(x, z, climateScale, 3, 0.5f, 2f);
        // Perlin clusters around 0.5, so stretch it to reach 0 and 1
        return Mathf.Clamp01((v - 0.5f) * climateContrast + 0.5f);
    }

    void PaintBiomes(TerrainData data,
        float tOffX, float tOffZ, float mOffX, float mOffZ)
    {
        // Collect the distinct layers assigned to biomes
        List<TerrainLayer> layerList = new List<TerrainLayer>();
        int[] layerIndex = new int[biomes.Count];

        for (int i = 0; i < biomes.Count; i++)
        {
            layerIndex[i] = -1;
            if (biomes[i].layer == null) continue;

            int idx = layerList.IndexOf(biomes[i].layer);
            if (idx < 0)
            {
                layerList.Add(biomes[i].layer);
                idx = layerList.Count - 1;
            }
            layerIndex[i] = idx;
        }

        if (layerList.Count == 0)
        {
            Debug.LogWarning("ProceduralTerrainGenerator: no Terrain Layer assigned to any biome (Biomes > Layer), so nothing was painted.");
            return;
        }

        // Biomes without a layer fall back to the first assigned layer
        for (int i = 0; i < biomes.Count; i++)
        {
            if (layerIndex[i] < 0)
            {
                layerIndex[i] = 0;
                Debug.LogWarning($"ProceduralTerrainGenerator: biome '{biomes[i].name}' has no Terrain Layer, using '{layerList[0].name}' instead.");
            }
        }

        data.terrainLayers = layerList.ToArray();

        int ar = data.alphamapResolution;
        float[,,] maps = new float[ar, ar, layerList.Count];
        float[] weights = new float[biomes.Count];

        for (int z = 0; z < ar; z++)
        {
            for (int x = 0; x < ar; x++)
            {
                float worldX = (float)x / (ar - 1) * mapWidth;
                float worldZ = (float)z / (ar - 1) * mapLength;

                ComputeBiomeWeights(worldX, worldZ, tOffX, tOffZ, mOffX, mOffZ, weights);

                for (int b = 0; b < biomes.Count; b++)
                    maps[z, x, layerIndex[b]] += weights[b];
            }
        }

        data.SetAlphamaps(0, 0, maps);
    }

    // ---------- Noise helper ----------

    static float Fbm(float x, float z, float scale, int octaves, float persistence, float lacunarity)
    {
        float amplitude = 1f, frequency = 1f, value = 0f, sum = 0f;

        for (int i = 0; i < octaves; i++)
        {
            value += Mathf.PerlinNoise(x * scale * frequency, z * scale * frequency) * amplitude;
            sum += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return value / sum;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ProceduralTerrainGenerator))]
public class ProceduralTerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ProceduralTerrainGenerator generator = (ProceduralTerrainGenerator)target;
        if (GUILayout.Button("Generate Terrain"))
        {
            generator.GenerateTerrain();
        }
    }
}
#endif