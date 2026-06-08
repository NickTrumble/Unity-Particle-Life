using System;
using UnityEngine;
using UnityEngine.Serialization;

// Contains the rendering.
public class ParticleRenderer : MonoBehaviour
{
    public ParticleConfig config;
    [FormerlySerializedAs("particleSystem")]
    public ParticleSystem particleSimulation;
    public Renderer rend;

    private Texture2D texture;
    private int[] pixels;

    private int width;
    private int height;

    private int prevW = -1;
    private int prevH = -1;

    void Start()
    {
        if (config == null)
            return;

        width = config.width;
        height = config.height;

        CheckForNewSize();
    }

    void Update()
    {
        if (config == null)
            return;

        width = config.width;
        height = config.height;

        CheckForNewSize();
        DrawParticles();
    }

    private void CheckForNewSize()
    {
        if (HasSizeChanged())
        {
            prevW = width;
            prevH = height;

            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            pixels = new int[width * height];

            if (rend != null)
                rend.material.mainTexture = texture;
        }
    }

    private bool HasSizeChanged()
    {
        return prevH != height || prevW != width;
    }

    private void DrawParticles()
    {
        if (particleSimulation == null || texture == null || pixels == null)
            return;

        Array.Clear(pixels, 0, width * height);
        for (int i = 0; i < config.particleCount; i++)
        {
            RenderParticle(particleSimulation.GetParticle(i));
        }

        texture.SetPixelData(pixels, 0);
        texture.Apply();
    }

    private void RenderParticle(Particle p)
    {
        if (p == null)
            return;

        int rad = Mathf.FloorToInt(config.particleSize / 2);
        int rad2 = rad * rad;
        int x = (int)p.GetX();
        int y = (int)p.GetY();

        for (int i = -rad + x; i <= rad + x; i++)
        {
            for (int j = -rad + y; j <= rad + y; j++)
            {
                if (i >= 0 && i < width && j >= 0 && j < height)
                {
                    int dx = i - x;
                    int dy = j - y;

                    if (rad2 >= dx * dx + dy * dy)
                    {
                        pixels[j * width + i] = ColourToInt(config.TypeColours[p.GetParticleType()]);
                    }
                }
            }
        }
    }

    private int ColourToInt(Color colour)
    {
        return (255 << 24) |
            (FloatColourToInt(colour.b) << 16) |
            (FloatColourToInt(colour.g) << 8) |
            (FloatColourToInt(colour.r) << 0);
    }

    private int FloatColourToInt(float colour)
    {
        return Mathf.FloorToInt(colour * 255);
    }
}
