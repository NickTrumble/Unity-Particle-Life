using System;
using UnityEngine;

// Contains details that the user can change, passed onto the renderer.
public class ParticleConfig : MonoBehaviour
{
    
    public float[] weightMatrix;

    public int width = 800;
    public int height = 450;

    public int particleCount = 300;
    public int particleTypeCount = 4;
    public int particleSize = 5;
    public int particleMass = 12;
    public int forceRadius = 65;
    public int maxAttraction = 2;
    public float maxSpeed = 35f;
    public float damping = 0.88f;

    public float timeStep = 0.45f;

    public float forceScaling = 1f;

    public Action CountUpdated;
    public Action WeightUpdated;
    public Action ParticlesResetRequested;
    public Action ParticlePositionsRandomizedRequested;
    public Color[] TypeColours;

    private bool running = false;
    private int gridX = 8;
    private int gridY = 5;

    private Color[] baseColours = new Color[]
    {
        new Color(1f, 0f, 0f),          // red
        new Color(1f, 0.5f, 0f),        // orange
        new Color(1f, 0.92f, 0.16f),    // yellow
        new Color(0.18f, 0.85f, 0.42f), // green
        new Color(0.22f, 0.48f, 1.00f), // blue
        new Color(0.60f, 0.20f, 0.80f), // purple
    };
    private void Start()
    {
        EnsureConfigArrays();
    }

    public void StartStopSim()
    {
        running = !running;
    }

    public void ResetParticles()
    {
        EnsureConfigArrays();
        ParticlesResetRequested?.Invoke();
    }

    public void RandomiseParticlePositions()
    {
        ParticlePositionsRandomizedRequested?.Invoke();
    }

    public void RandomiseTypeColours()
    {
        EnsureConfigArrays();

        for (int i = 0; i < TypeColours.Length; i++)
        {
            TypeColours[i] = UnityEngine.Random.ColorHSV(0f, 1f, 0.65f, 1f, 0.85f, 1f);
        }
    }

    public void RandomiseWeightMatrix()
    {
        EnsureConfigArrays();

        System.Random rnd = new System.Random();
        for (int i = 0; i < particleTypeCount; i++)
        {
            for (int j = 0; j < particleTypeCount; j++)
            {
                SetWeight(i, j, (float)((rnd.NextDouble() * 2 * maxAttraction) - maxAttraction));
            }
        }
        WeightUpdated?.Invoke();
    }

    public void ApplyRecommendedSettings()
    {
        width = 1600;
        height = 900;
        particleCount = 2000;
        particleTypeCount = 6;
        particleSize = 5;
        particleMass = 12;
        forceRadius = 65;
        maxSpeed = 15f;
        damping = 0.95f;
        timeStep = 0.2f;

        gridX = 24;
        gridY = 6;
        TypeColours = new Color[particleTypeCount];
        Array.Copy(baseColours, TypeColours, particleTypeCount);

        weightMatrix = new float[particleTypeCount * particleTypeCount];
        float[] recommendedWeights =
        {
             1f, 0.2f,  0f,   0f,   0f,   0f,
             0f,   1f, 0.2f,   0f,   0f,   0f,
             0f,   0f,   1f, 0.2f,   0f,   0f,
             0f,   0f,   0f,   1f, 0.2f,   0f,
             0f,   0f,   0f,   0f,   1f, 0.2f,
             0.2f, 0f,   0f,   0f,   0f,   1f
        };
        Array.Copy(recommendedWeights, weightMatrix, recommendedWeights.Length);

        CountUpdated?.Invoke();
        WeightUpdated?.Invoke();
    }

    void OnValidate()
    {
        particleCount = Math.Max(0, particleCount);
        particleSize = Math.Max(3, particleSize % 2 == 0 ? particleSize + 1 : particleSize);// min 3, odd only
        particleTypeCount = Math.Max(1, particleTypeCount);
        particleMass = Math.Max(1, particleMass);
        forceRadius = Math.Max(1, forceRadius);
        maxSpeed = Mathf.Max(1f, maxSpeed);
        damping = Mathf.Clamp01(damping);

        width = Mathf.Max(100, width);
        height = Mathf.Max(100, height);
        height = 9 * width / 16;

        gridX = Math.Max(1, Mathf.FloorToInt((float)width / forceRadius));
        gridY = Math.Max(1, Mathf.FloorToInt((float)height / forceRadius));

        EnsureConfigArrays();

        CountUpdated?.Invoke();
    }

    private void EnsureConfigArrays()
    {
        int matrixSize = particleTypeCount * particleTypeCount;
        if (weightMatrix == null)
        {
            weightMatrix = new float[matrixSize];
        }
        else if (weightMatrix.Length != matrixSize)
        {
            float[] oldWeights = weightMatrix;
            weightMatrix = new float[matrixSize];
            Array.Copy(oldWeights, weightMatrix, Math.Min(oldWeights.Length, weightMatrix.Length));
        }

        if (TypeColours == null)
        {
            TypeColours = new Color[particleTypeCount];
            FillDefaultColours(0);
        }
        else if (TypeColours.Length != particleTypeCount)
        {
            Color[] oldColours = TypeColours;
            TypeColours = new Color[particleTypeCount];
            Array.Copy(oldColours, TypeColours, Math.Min(oldColours.Length, TypeColours.Length));
            FillDefaultColours(oldColours.Length);
        }
    }

    private void FillDefaultColours(int startIndex)
    {
        for (int i = startIndex; i < TypeColours.Length; i++)
        {
            TypeColours[i] = i < baseColours.Length
                ? baseColours[i]
                : UnityEngine.Random.ColorHSV(0f, 1f, 0.65f, 1f, 0.85f, 1f);
        }
    }

    public bool isRunning() { return running; }

    public float GetWeight(int x, int y)
    {
        EnsureConfigArrays();
        return weightMatrix[x * particleTypeCount + y];
    }

    public void SetWeight(int x, int y, float val)
    {
        EnsureConfigArrays();
        weightMatrix[x * particleTypeCount + y] = val;
    }

    public int GetGridX() { return gridX; }
    public int GetGridY() { return gridY; }
}
