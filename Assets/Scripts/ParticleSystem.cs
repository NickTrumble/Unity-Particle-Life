using System;
using System.Collections.Generic;
using UnityEngine;

public struct Particle
{
    public float x;
    public float y;
    public float u;
    public float v;
    public float ax;
    public float ay;
    public int type;
}

// Contains calculations with multiple particles.
public class ParticleSystem : MonoBehaviour
{


    public ParticleConfig config;

    private Particle[] particles;
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
        config.ParticlesResetRequested += ResetParticles;
        config.ParticlePositionsRandomizedRequested += RandomiseParticlePositions;

        ResetParticles();
    }


    void Update()
    {
        if (config == null || particles == null || !config.isRunning())
            return;

        ApplyForcesOnNeighbours();

        for (int i = 0; i < particles.Length; i++)
            UpdateParticle(i);
    }

    private void ApplyForce(int i1, int i2)
    {
        ref Particle p1 = ref particles[i1];
        ref Particle p2 = ref particles[i2];

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

    private void ApplyForcesOnNeighbours()
    {
        BuildGrid();

        for(int i = 0; i < config.gridX; i++)
        {
            for (int j = 0; j < config.gridY; j++)
            {
                IterateCell(i, j);
            }
        }
    }

    private void IterateCell(int x, int y)
    {
        int neighbourRangeX = Mathf.CeilToInt(config.forceRadius / gridWidth);
        int neighbourRangeY = Mathf.CeilToInt(config.forceRadius / gridHeight);

        foreach(int i1 in gridIndices[x, y])
        {
            visitedNeighbourCells.Clear();

            for (int i = -neighbourRangeX; i <= neighbourRangeX; i++)
            {
                for (int j = -neighbourRangeY; j <= neighbourRangeY; j++)
                {
                    int x1 = WrapIndex(x + i, config.gridX);
                    int y1 = WrapIndex(y + j, config.gridY);
                    int cellIndex = y1 * config.gridX + x1;

                    if (!visitedNeighbourCells.Add(cellIndex))
                        continue;

                    List<int> neighbours = gridIndices[x1, y1];
                    foreach(int i2 in neighbours)
                    {
                        if (i2 <= i1)
                            continue;

                        ApplyForce(i1, i2);
                        ApplyForce(i2, i1);
                    }
                }
            }
        }
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

        particles = new Particle[config.particleCount];
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

    private void UpdateParticle(int index)
    {
        ref Particle particle = ref particles[index];

        particle.u += particle.ax * config.timeStep;
        particle.v += particle.ay * config.timeStep;

        LimitSpeed(index);

        particle.x = (config.width + particle.x + particle.u * config.timeStep) % config.width;
        particle.y = (config.height + particle.y + particle.v * config.timeStep) % config.height;

        particle.ax = 0f;
        particle.ay = 0f;

        particle.u *= config.damping;
        particle.v *= config.damping;
    }

    private void LimitSpeed(int index)
    {
        float speedSquared = particles[index].u * particles[index].u + particles[index].v * particles[index].v;
        float maxSpeedSquared = config.maxSpeed * config.maxSpeed;

        if (speedSquared <= maxSpeedSquared)
            return;

        float scale = config.maxSpeed / Mathf.Sqrt(speedSquared);
        particles[index].u *= scale;
        particles[index].v *= scale;
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
        particles[index].x = rnd.Next(config.width);
        particles[index].y = rnd.Next(config.height);
    }

    public Particle GetParticle(int index)
    {
        if (particles == null)
            return new Particle();

        if (index < 0 || index >= particles.Length)
            return new Particle();

        return particles[index];
    }

    public int ParticleCount
    {
        get { return particles == null ? 0 : particles.Length; }
    }
}
