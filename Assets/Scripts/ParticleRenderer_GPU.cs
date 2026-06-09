using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleRenderer_GPU : MonoBehaviour
{
    public ParticleConfig config;
    public ParticleSystem particleSystem;

    public Mesh quadMesh;
    public Material particleMaterial;

    private Matrix4x4[] matrices;

    private int width, height;
    private int prevW = -1;
    private int prevH = -1;

    // Start is called before the first frame update
    void Start()
    {
        if (config == null)
            return;

        matrices = new Matrix4x4[config.particleCount];

        width = config.width;
        height = config.height;

        CheckForNewSize();
    }

    // Update is called once per frame
    void Update()
    {
        if (config == null)
            Debug.Log("conmfig is null");
            //return;

        width = config.width;
        height = config.height;

        Debug.Log("aaa");
        CheckForNewSize();

        Debug.Log("bbb"); 
        //draw
        DrawParticles();
        Debug.Log("finished");
    }

    //GPU draw
    private void DrawParticles()
    {
        Debug.Log("xccccc");
        int particleCount = particleSystem.ParticleCount;

        for (int i = 0; i < particleCount; i++)
        {
            Particle p = particleSystem.GetParticle(i);

            matrices[i] = 
                Matrix4x4.TRS(
                    new Vector3(p.x, p.y, 0),//pos
                    Quaternion.identity, //rotation
                    Vector3.one * config.particleSize//scale
                );
        }
        Debug.Log("dddd");

        Graphics.DrawMeshInstanced(
            quadMesh,
            0,
            particleMaterial,
            matrices,
            particleCount
        );
        Debug.Log("eeee");
    }

    private void CheckForNewSize()
    {
        if (HasSizeChanged())
        {
            prevW = width;
            prevH = height;

            //changes...
        }
    }

    private bool HasSizeChanged()
    {
        return prevH != height || prevW != width;
    }

}
