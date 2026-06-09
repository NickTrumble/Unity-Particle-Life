using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class ParticleRenderer_GPU : MonoBehaviour
{
    public ParticleConfig config;
    public ParticleSystem particleSystem;

    public Mesh quadMesh;
    public Material particleMaterial;

    private Matrix4x4[] matrices;
    private MaterialPropertyBlock colourArray;
    private Vector4[] colours;

    private int width, height;
    private int prevW = -1;
    private int prevH = -1;
    
    private const int BATCH_SIZE = 1023;
    private Matrix4x4[] batchMatrices;
    private Vector4[] batchColours;

    // Start is called before the first frame update
    void Start()
    {
        if (config == null)
            return;

        config.CountUpdated += UpdateArrays;

        width = config.width;
        height = config.height;

        CheckForNewSize();
    }

    // Update is called once per frame
    void Update()
    {
        if (config == null)
            return;

        width = config.width;
        height = config.height;

        CheckForNewSize();

        DrawParticles();
    }

    //GPU draw
    private void DrawParticles()
    {
        NativeArray<Particle> particles = particleSystem.GetParticles();
        int particleCount = particleSystem.ParticleCount;
        Color c;
        for (int i = 0; i < particleCount; i++)
        {
            Particle p = particles[i];

            float pixelsPerUnit = height / (Camera.main.orthographicSize * 2f);
            float worldSize = config.particleSize / pixelsPerUnit;
            
            matrices[i] = Matrix4x4.TRS(
                PixelToWorld(p.x, p.y),
                Quaternion.identity,
                Vector3.one * worldSize
            );

            c = config.TypeColours[p.type];
            colours[i] = new Vector4(c.r, c.g, c.b, c.a);
        }

        int count = 0;
        while (count < particleCount)
        {
            int batchCount = Mathf.Min(BATCH_SIZE, particleCount - count);

            for (int i = 0; i < batchCount; i++)
            {
                batchMatrices[i] = matrices[count + i];
                batchColours[i] = colours[count + i];
            }

            colourArray.SetVectorArray("_Color", batchColours);
            Graphics.DrawMeshInstanced(quadMesh, 0, particleMaterial, batchMatrices, batchCount, colourArray);

            count += BATCH_SIZE;
        }
    
    }

    private Vector3 PixelToWorld(float px, float py)
    {
        // Convert pixel coords to 0..1 range, then to viewport, then to world
        Vector3 viewportPoint = new Vector3(px / width, py / height, -Camera.main.transform.position.z);
        return Camera.main.ViewportToWorldPoint(viewportPoint);
    }

    private void CheckForNewSize()
    {
        if (HasSizeChanged())
        {
            UpdateArrays();
            //changes...
        }
    }

    private void UpdateArrays()
    {
        prevW = width;
        prevH = height;

        matrices = new Matrix4x4[config.particleCount];

        colours = new Vector4[config.particleCount];
        colourArray = new MaterialPropertyBlock();

        batchMatrices = new Matrix4x4[BATCH_SIZE];
        batchColours = new Vector4[BATCH_SIZE];
    }

    private bool HasSizeChanged()
    {
        return prevH != height || prevW != width;
    }

}
