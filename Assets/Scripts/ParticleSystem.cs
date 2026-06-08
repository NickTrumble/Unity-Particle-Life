using System;
using System.Collections.Generic;
using UnityEngine;

// Contains calculations with multiple particles.
public class ParticleSystem : MonoBehaviour
{
    public ParticleConfig config;

    private Particle[] particles;
    private List<int>[,] gridIndices;
    private readonly HashSet<int> visitedNeighbourCells = new HashSet<int>();

    private float gridWidth;
    private float gridHeight;

    void Start()
    {
        if (config == null)
            return;

        config.CountUpdated += UpdateCount;
        config.ParticlesResetRequested += ResetParticles;
        config.ParticlePositionsRandomizedRequested += RandomiseParticlePositions;

        ResetParticles();
    }

    private void OnDestroy()
    {
        if (config == null)
            return;

        config.CountUpdated -= UpdateCount;
        config.ParticlesResetRequested -= ResetParticles;
        config.ParticlePositionsRandomizedRequested -= RandomiseParticlePositions;
    }

    void Update()
    {
        if (config == null || particles == null || !config.isRunning())
            return;

        //ApplyForces();

        ApplyForcesOnNeighbours();

        for (int i = 0; i < particles.Length; i++)
            particles[i]?.Update();
    }

    private void ApplyForces()
    {
        for (int i = 0; i < particles.Length - 1; i++)
        {
            for (int j = i + 1; j < particles.Length; j++)
            {
                Particle first = particles[i];
                Particle second = particles[j];

                if (first == null || second == null)
                    continue;

                first.ApplyForce(second);
                second.ApplyForce(first);
            }
        }
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
            if (particles[i] == null)
                continue;

            int x = Mathf.Clamp(
                (int)(particles[i].GetX() / gridWidth),
                0,
                config.gridX - 1
                );
            int y = Mathf.Clamp(
                (int)(particles[i].GetY() / gridHeight),
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

                        particles[i1].ApplyForce(particles[i2]);
                        particles[i2].ApplyForce(particles[i1]);
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
            if (particles[i] == null)
                particles[i] = new Particle(i % config.particleTypeCount, config);
            else
                particles[i].RandomisePosition();
        }
    }

    private void PopulateGrid()
    {
        int type = -1;
        for (int i = 0; i < config.particleCount; i++)
        {
            type = (type + 1) % config.particleTypeCount;
            particles[i] = new Particle(type, config);
        }
    }

    public Particle GetParticle(int index)
    {
        if (particles == null || index < 0 || index >= particles.Length)
            return null;

        return particles[index];
    }
}
