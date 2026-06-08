using System;
using UnityEngine;

// Contains details that the user can change, passed onto the renderer.
public class ParticleConfig : MonoBehaviour
{
    [SerializeField]
    private float[] weightMatrix;

    public int width = 800;
    public int height = 450;

    public int particleCount = 300;
    public int particleTypeCount = 4;
    public int particleSize = 5;
    public int particleMass = 12;
    public int forceRadius = 65;
    public float maxSpeed = 35f;
    public float damping = 0.88f;

    public int gridX = 8;
    public int gridY = 5;

    public float timeStep = 0.45f;

    public Action CountUpdated;
    public Action ParticlesResetRequested;
    public Action ParticlePositionsRandomizedRequested;
    public Color[] TypeColours;

    private bool running = false;

    private Color[] baseColours = new Color[]
    {
        new Color(0.95f, 0.20f, 0.24f),
        new Color(0.18f, 0.85f, 0.42f),
        new Color(0.22f, 0.48f, 1.00f),
        new Color(1.00f, 0.82f, 0.18f)
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
                SetWeight(i, j, (float)((rnd.NextDouble() * 10f) - 5f));
            }
        }
    }

    public void ApplyRecommendedSettings()
    {
        width = 800;
        height = 450;
        particleCount = 300;
        particleTypeCount = 4;
        particleSize = 5;
        particleMass = 12;
        forceRadius = 65;
        maxSpeed = 35f;
        damping = 0.88f;
        gridX = 8;
        gridY = 5;
        timeStep = 0.45f;

        TypeColours = new Color[particleTypeCount];
        Array.Copy(baseColours, TypeColours, particleTypeCount);

        weightMatrix = new float[particleTypeCount * particleTypeCount];
        float[] recommendedWeights =
        {
             1.20f, -2.40f,  0.80f, -0.70f,
            -0.35f,  1.00f, -2.10f,  0.90f,
             0.55f, -0.80f,  1.10f, -2.20f,
            -1.80f,  0.75f, -0.45f,  1.00f
        };
        Array.Copy(recommendedWeights, weightMatrix, recommendedWeights.Length);

        CountUpdated?.Invoke();
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

        gridX = Math.Max(1, gridX);
        gridY = Math.Max(1, gridY);

        width = Mathf.Max(100, width);
        height = Mathf.Max(100, height);
        height = 9 * width / 16;

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
}
