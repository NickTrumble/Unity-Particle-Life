using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEditor.PackageManager;
using System;

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
    private List<int>[,] gridIndices;
    private readonly HashSet<int> visitedNeighbourCells = new HashSet<int>();

    private float gridWidth;
    private float gridHeight;

    private static System.Random rnd = new System.Random();


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

        var forceJob = new ExecuteJob
        {
            particles = particles,
            forceBuffer = forceBuffer,
            radius = config.forceRadius,
            width = config.width,
            height = config.height,
            typeCount = config.particleTypeCount,
            weights = cachedWeights
        };

        JobHandle handle = forceJob.Schedule(particles.Length, 64);

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

        handle = integrateJob.Schedule(particles.Length, 64, handle);

        handle.Complete();
    }

    void OnDestroy()
    {
        if (particles.IsCreated)
            particles.Dispose();

        if (forceBuffer.IsCreated)
            forceBuffer.Dispose();
        
        if (cachedWeights.IsCreated) 
            cachedWeights.Dispose();
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

    private void ApplyForce(int i1, int i2)
    {
        Particle p1 = particles[i1];
        Particle p2 = particles[i2];

        float dx = GetWrappedDeltaX(p1.x, p2.x);
        float dy = GetWrappedDeltaY(p1.y, p2.y);
        float distanceSquared = dx * dx + dy * dy;
        float radiusSquared = config.forceRadius * config.forceRadius;

        if (distanceSquared > radiusSquared)
            return;
        float force;
        float weight = config.GetWeight(p1.type, p2.type);
        if (distanceSquared / radiusSquared < 0.2f)
        {
            force = -1 * (1f - (distanceSquared / radiusSquared)) / 0.2f;
        } else
        {
            force = weight * (1f - (distanceSquared / radiusSquared));
        }

        float dist = Mathf.Sqrt(distanceSquared);
        float nx = dx / dist;
        float ny = dy / dist;

        p1.ax += nx * force / config.particleMass;
        p1.ay += ny * force / config.particleMass;

        particles[i1] = p1;
        particles[i2] = p2;
    }

    private float GetWrappedDeltaX(float p1, float p2)
    {
        float dx = p2 - p1;
        float halfWidth = config.width * 0.5f;

        if (dx > halfWidth)
            dx -= config.width;
        else if (dx < -halfWidth)
            dx += config.width;

        return dx;
    }

    private float GetWrappedDeltaY(float p1, float p2)
    {
        float dy = p2 - p1;
        float halfHeight = config.height * 0.5f;

        if (dy > halfHeight)
            dy -= config.height;
        else if (dy < -halfHeight)
            dy += config.height;

        return dy;
    }

    private void InitGrid()
    {
        gridIndices = new List<int>[config.gridX, config.gridY];
        gridWidth = (float)config.width / config.gridX;
        gridHeight = (float)config.height / config.gridY;

        for (int i = 0; i < config.gridX; i++)
        {
            for (int j = 0; j < config.gridY; j++)
            {
                gridIndices[i, j] = new List<int>();
            }
        }
    }

    private void ClearGrid()
    {
        if (NeedsGridInit())
            InitGrid();

        for (int i = 0; i < config.gridX; i++)
        {
            for (int j = 0; j < config.gridY; j++)
            {
                gridIndices[i, j].Clear();
            }
        }
    }

    private void BuildGrid()
    {
        ClearGrid();
        for (int i = 0; i < particles.Length; i++)
        {
            int x = Mathf.Clamp(
                (int)(particles[i].x / gridWidth),
                0,
                config.gridX - 1
                );
            int y = Mathf.Clamp(
                (int)(particles[i].y / gridHeight),
                0,
                config.gridY - 1
                );
            gridIndices[x, y].Add(i);
        }
    }

    private bool NeedsGridInit()
    {
        return gridIndices == null ||
            gridIndices.GetLength(0) != config.gridX ||
            gridIndices.GetLength(1) != config.gridY ||
            !Mathf.Approximately(gridWidth, (float)config.width / config.gridX) ||
            !Mathf.Approximately(gridHeight, (float)config.height / config.gridY);
    }

    private int WrapIndex(int index, int length)
    {
        return (index % length + length) % length;
    }

    private void UpdateCount()
    {
        InitGrid();
        ResetParticles();
    }

    private void ResetParticles()
    {
        if (config == null)
            return;

        particles = new NativeArray<Particle>(config.particleCount, Allocator.Persistent);
        forceBuffer = new NativeArray<float2>(config.particleCount, Allocator.Persistent);
        FlattenWeights();
        InitGrid();
        PopulateGrid();
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

    public void Execute(int i)
    {
        Particle p = particles[i];
        Particle p1;

        float2 total = float2.zero;

        for (int j = 0; j < particles.Length; j++)
        {
            if (i == j)
                continue;

            p1 = particles[j];

            //wraps
            float dx = p1.x - p.x;
            dx -= width * math.round(dx / width);

            float dy = p1.y - p.y;
            dy -= height * math.round(dy / height);

            float distSquared = dx * dx + dy * dy;

            if (distSquared > radius * radius)
                continue;

            float dist = math.sqrt(distSquared) + 1e-8f;
            float nd = dist / radius;

            
            float weight = weights[p.type * typeCount + p1.type];

            float force = weight * (1f - nd);
            if (nd < 0.4f)
                force = -1f * (0.2f / (nd + 0.001f));

            total += new float2(dx / dist, dy / dist) * force;
        }

        forceBuffer[i] = total;
    }
}