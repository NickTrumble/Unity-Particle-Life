using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEditor.PackageManager;
using System;
using JetBrains.Annotations;
using static UnityEngine.ParticleSystem;

public struct Particle
{
    public float x, y;
    public float u, v;
    public float ax, ay;
    public int type;
}

// Contains calculations with multiple particles.
public class ParticleSystem : MonoBehaviour
{
    public ParticleConfig config;

    private NativeArray<Particle> particles;
    private NativeArray<float2> forceBuffer;
    private NativeArray<float> cachedWeights;

    private static System.Random rnd = new System.Random();

    private NativeArray<int> gridCells;
    private NativeArray<int> gridCellStart;
    private NativeArray<int> gridCellCount;

    private float gridWidth;
    private float gridHeight;

    void Start()
    {
        if (config == null)
            return;

        config.CountUpdated += UpdateCount;
        config.WeightUpdated += FlattenWeights;
        config.ParticlesResetRequested += ResetParticles;
        config.ParticlePositionsRandomizedRequested += RandomiseParticlePositions;

        ResetParticles();
    }

    void Update()
    {
        if (config == null || particles == null || !config.isRunning())
            return;

        //BuildGrid();

        var forceJob = new ExecuteJob
        {
            particles = particles,
            forceBuffer = forceBuffer,
            radius = config.forceRadius,
            width = config.width,
            height = config.height,
            typeCount = config.particleTypeCount,
            weights = cachedWeights,
            //grid
            gridCellCount = gridCellCount,
            gridCells = gridCells,
            gridCellStart = gridCellStart,
            gridX = config.GetGridX(),
            gridY = config.GetGridY(),
            cellHeight = gridHeight,
            cellWidth = gridWidth
        };

        var integrateJob = new IntegrateJob
        {
            particles = particles,
            forceBuffer = forceBuffer,
            dt = config.timeStep,
            dampening = config.damping,
            maxSpeed = config.maxSpeed,
            width = config.width,
            height = config.height
        };

        var buildJob = new BuildGridJob
        {
            cellCount = config.GetGridX() * config.GetGridY(),
            particles = particles,
            gridCellCount = gridCellCount,
            gridCells = gridCells,
            gridCellStart = gridCellStart,
            gridX = config.GetGridX(),
            gridY = config.GetGridY(),
            gridHeight = gridHeight,
            gridWidth = gridWidth

        };

        JobHandle handle = buildJob.Schedule();
        handle = forceJob.Schedule(particles.Length, 64, handle);
        handle = integrateJob.Schedule(particles.Length, 64, handle);

        handle.Complete();
    }

    void OnDestroy()
    {
        if (particles.IsCreated) particles.Dispose();

        if (forceBuffer.IsCreated) forceBuffer.Dispose();
        
        if (cachedWeights.IsCreated) cachedWeights.Dispose();
        
        if (gridCells.IsCreated) gridCells.Dispose();

        if (gridCellStart.IsCreated) gridCellStart.Dispose();

        if (gridCellCount.IsCreated) gridCellCount.Dispose();
    }

    private void InitGrid()
    {
        int cellCount = config.GetGridX() * config.GetGridY();

        if (gridCells.IsCreated) gridCells.Dispose();
        if (gridCellStart.IsCreated) gridCellStart.Dispose();
        if (gridCellCount.IsCreated) gridCellCount.Dispose();

        gridCells = new NativeArray<int>(config.particleCount, Allocator.Persistent);
        gridCellStart = new NativeArray<int>(cellCount, Allocator.Persistent);
        gridCellCount = new NativeArray<int>(cellCount, Allocator.Persistent);
    }

    private void BuildGrid()
    {
        int cellCount = config.GetGridX() * config.GetGridY();

        EmptyGridCellCount(cellCount);

        for (int i = 0; i < particles.Length; i++)
        {
            gridCellCount[GetCellIndex(particles[i])]++;
        }

        gridCellStart[0] = 0;
        for (int i = 1; i < cellCount; i++)
        {
            gridCellStart[i] = gridCellStart[i - 1] + gridCellCount[i - 1];
        }

        EmptyGridCellCount(cellCount);

        for (int i = 0; i < particles.Length; i++)
        {
            int cell = GetCellIndex(particles[i]);
            gridCells[gridCellStart[cell] + gridCellCount[cell]] = i;
            gridCellCount[cell]++;
        }
    }

    private int GetCellIndex(Particle p)
    {
        int[] cell = GetCellIndexes(p);
        return cell[0] * config.GetGridY() + cell[1];
    }

    private int[] GetCellIndexes(Particle p)
    {
        int cellX = Mathf.Clamp((int)(p.x / gridWidth), 0, config.GetGridX() - 1);
        int cellY = Mathf.Clamp((int)(p.y / gridHeight), 0, config.GetGridY() - 1);
        return new int[] { cellX, cellY };
    }

    private void EmptyGridCellCount(int cellCount)
    {
        for (int i = 0; i < cellCount; i++)
            gridCellCount[i] = 0;
    }

    private void FlattenWeights()
    {
        int typeCount = config.particleTypeCount;

        NativeArray<float> weights = new NativeArray<float>(typeCount * typeCount, Allocator.Persistent);
        for (int i = 0; i < config.particleTypeCount * config.particleTypeCount; i++)
        {
            weights[i] = config.weightMatrix[i];
        }
        cachedWeights = weights;
    }

    private void UpdateCount()
    {
        ResetParticles();
    }

    private void ResetParticles()
    {
        if (config == null)
            return;

        particles = new NativeArray<Particle>(config.particleCount, Allocator.Persistent);
        forceBuffer = new NativeArray<float2>(config.particleCount, Allocator.Persistent);

        gridWidth = (float)config.width / config.GetGridX();
        gridHeight = (float)config.height / config.GetGridY();

        FlattenWeights();
        PopulateGrid();

        InitGrid();
    }

    private void RandomiseParticlePositions()
    {
        if (config == null)
            return;

        if (particles == null || particles.Length != config.particleCount)
            ResetParticles();

        for (int i = 0; i < particles.Length; i++)
        {
            particles[i] = CreateParticle(i % config.particleTypeCount);
        }
    }

    private Particle CreateParticle(int newType)
    { 
        return new Particle
        {
            x = rnd.Next(config.width),
            y = rnd.Next(config.height),
            u = 0f,
            v = 0f,
            ax = 0f,
            ay = 0f,
            type = newType
        };
    }

    private void PopulateGrid()
    {
        int type = -1;
        for (int i = 0; i < config.particleCount; i++)
        {
            type = (type + 1) % config.particleTypeCount;
            particles[i] = CreateParticle(type);
        }
    }

    public void RandomisePosition(int index)
    {
        Particle p = particles[index];
        p.x = rnd.Next(config.width);
        p.y = rnd.Next(config.height);
        particles[index] = p;
    }

    public Particle GetParticle(int index)
    {
        if (particles == null)
            return new Particle();

        if (index < 0 || index >= particles.Length)
            return new Particle();

        return particles[index];
    }

    public NativeArray<Particle> GetParticles() { return particles; }

    public int ParticleCount
    {
        get { return particles == null ? 0 : particles.Length; }
    }
}


//apply the force
[BurstCompile]
public struct IntegrateJob : IJobParallelFor
{
    public NativeArray<Particle> particles;
    public NativeArray<float2> forceBuffer;

    public float dt;
    public float dampening;
    public float maxSpeed;

    public int width;
    public int height;

    Particle p;

    public void Execute(int i)
    {
        p = particles[i];
        float2 a = forceBuffer[i];

        p.u += a.x * dt;
        p.v += a.y * dt;

        float speedSquared = p.u * p.u + p.v * p.v;

        if (speedSquared > maxSpeed * maxSpeed)
        {
            float scale = maxSpeed / math.sqrt(speedSquared);
            p.u *= scale;
            p.v *= scale;
        }

        p.x += p.u * dt;
        p.y += p.v * dt;

        p.u *= dampening;
        p.v *= dampening;

        p.x = math.fmod(p.x + width, width);
        p.y = math.fmod(p.y + height, height);

        forceBuffer[i] = float2.zero;

        particles[i] = p;
    }

}

//calc the force
[BurstCompile]
public struct ExecuteJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Particle> particles;
    public NativeArray<float2> forceBuffer;

    public float radius;
    public float width;
    public float height;

    [ReadOnly] public NativeArray<float> weights;
    public int typeCount;

    [ReadOnly] public NativeArray<int> gridCells;
    [ReadOnly] public NativeArray<int> gridCellStart;
    [ReadOnly] public NativeArray<int> gridCellCount;
    public int gridX;
    public int gridY;
    public float cellWidth;
    public float cellHeight;

    public void Execute(int i)
    {
        Particle p = particles[i];
        float2 total = float2.zero;

        int cellX = math.clamp((int)(p.x / cellWidth), 0, gridX - 1);
        int cellY = math.clamp((int)(p.y / cellHeight), 0, gridY - 1);

        int radX = (int)math.ceil(radius / cellWidth);
        int radY = (int)math.ceil(radius / cellHeight);

        for (int x = -radX; x <= radX; x++)
        {
            for(int y = -radY; y <= radY; y++)
            {
                int wx = (gridX + cellX + x) % gridX;
                int wy = (gridY + cellY + y) % gridY;


                int cell = wx * gridY + wy;

                for (int j = 0; j < gridCellCount[cell]; j++)
                {
                    Particle p1 = particles[gridCells[gridCellStart[cell] + j]];

                    float dx = p1.x - p.x;
                    float dy = p1.y - p.y;

                    dx -= width * math.round(dx / width);
                    dy -= height * math.round(dy / height);

                    float distSquared = dx * dx + dy * dy;
                    if (distSquared > radius * radius)
                        continue;

                    float dist = math.sqrt(distSquared) + 1e-8f;
                    float nd = dist / radius;

                    float weight = weights[p.type * typeCount + p1.type];

                    //float force = weight * (1f - nd);
                    //if (nd < 0.4f)
                    //    force = -1f * (1f - (nd + 1e-5f)) / 0.2f;

                    float force;
                    if (nd < 0.35f)
                    {
                        force = -1f * (0.35f - nd) / 0.35f;   // smooth repulsion
                    }
                    else
                    {
                        float t = (nd - 0.35f) / (1f - 0.35f);
                        force = weight * (1f - t);           // smooth attraction decay
                    }

                    total += new float2(dx / dist, dy / dist) * force;
                } 


            }
        }

        forceBuffer[i] = total;
    }
}

//build grid on gpu
[BurstCompile]
public struct BuildGridJob : IJob
{
    public NativeArray<Particle> particles;

    public int cellCount;
    public NativeArray<int> gridCells;
    public NativeArray<int> gridCellStart;
    public NativeArray<int> gridCellCount;

    public float gridWidth;
    public float gridHeight;

    public int gridX;
    public int gridY;

    public void Execute()
    {

        for (int i = 0; i < cellCount; i++)
            gridCellCount[i] = 0;

        for (int i = 0; i < particles.Length; i++)
        {
            gridCellCount[GetCellIndex(particles[i])]++;
        }

        gridCellStart[0] = 0;
        for (int i = 1; i < cellCount; i++)
        {
            gridCellStart[i] = gridCellStart[i - 1] + gridCellCount[i - 1];
        }

        for (int i = 0; i < cellCount; i++)
            gridCellCount[i] = 0;

        for (int i = 0; i < particles.Length; i++)
        {
            int cell = GetCellIndex(particles[i]);
            gridCells[gridCellStart[cell] + gridCellCount[cell]] = i;
            gridCellCount[cell]++;
        }
    }

    private int GetCellIndex(Particle p)
    {
        int cellX = Mathf.Clamp((int)(p.x / gridWidth), 0, gridX - 1);
        int cellY = Mathf.Clamp((int)(p.y / gridHeight), 0, gridY - 1);
        return cellX * gridY + cellY;
    }
}