using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Contains individual particle behaviour

public class Particle
{
    private ParticleConfig config;
    private static System.Random rnd = new System.Random();

    //type = 0 - particle type - 1
    private int type = 0;

    private float x;
    private float y;

    private float u;
    private float v;

    private float ax;
    private float ay;

    public Particle(int type, ParticleConfig config)
    {
        this.type = type;
        this.config = config;

        RandomisePosition();

        u = 0f;
        v = 0f;

        ax = 0f;
        ay = 0f;
    }

    public void Update()
    {
        x = (config.width + x + u * config.timeStep) % config.width;
        y = (config.height + y + v * config.timeStep) % config.height;

        u += ax * config.timeStep;
        v += ay * config.timeStep;
        ax = 0f;
        ay = 0f;
        u *= config.damping;
        v *= config.damping;
        LimitSpeed();
        //Debug.Log(ax);
        //Debug.Log($"updated velocity: {u}, {v}");
    }

    public void ApplyForce(Particle p)
    {
        float dx = GetWrappedDeltaX(p);
        float dy = GetWrappedDeltaY(p);
        float dist = dx * dx + dy * dy + 1e-8f;
        if (dist > config.forceRadius * config.forceRadius)
            return;

        float weight = config.GetWeight(type, p.GetParticleType());
        float force;
        if (dist < 0.2f)
        {
            force = -1f * (0.2f - (dist / (config.forceRadius * config.forceRadius))) / 0.2f;
        } else
        {
            force = weight * (1f - (dist / (config.forceRadius * config.forceRadius)));
        }

        //force = Mathf.Max(0f, 1 - (float)dist / (config.forceRadius * config.forceRadius)) * weight;

        float d = Mathf.Sqrt(dist);
        float nx = dx / d;
        float ny = dy / d;


        float fx = force * nx / config.particleMass;
        float fy = force * ny / config.particleMass;
        //Debug.Log($"Force: {force}, fx:{fx}, fy:{fy}, dx:{dx}, dy:{dy}, weight:{weight}, dist:{dist}, rad^2:{config.forceRadius * config.forceRadius}, force:{(float)dist / config.forceRadius * config.forceRadius}");
        p.AddForceX(fx); p.AddForceY(fy);
    }

    public float GetDistanceSquared(Particle p)
    {
        float dx = GetWrappedDeltaX(p);
        float dy = GetWrappedDeltaY(p);
        return dx * dx + dy * dy;
    }

    private float GetWrappedDeltaX(Particle p)
    {
        float dx = x - p.GetX();
        float halfWidth = config.width * 0.5f;

        if (dx > halfWidth)
            dx -= config.width;
        else if (dx < -halfWidth)
            dx += config.width;

        return dx;
    }

    private float GetWrappedDeltaY(Particle p)
    {
        float dy = y - p.GetY();
        float halfHeight = config.height * 0.5f;

        if (dy > halfHeight)
            dy -= config.height;
        else if (dy < -halfHeight)
            dy += config.height;

        return dy;
    }

    public float GetMagnitude() { return x * x + y * y; }

    public float GetX() { return x; }
    public float GetY() { return y; }
    public float GetU() { return u; }
    public float GetV() { return v; }
    public int GetParticleType() { return type; }

    public void RandomisePosition()
    {
        x = rnd.Next(config.width - 1);
        y = rnd.Next(config.height - 1);
    }

    private void LimitSpeed()
    {
        float speedSquared = u * u + v * v;
        float maxSpeedSquared = config.maxSpeed * config.maxSpeed;

        if (speedSquared <= maxSpeedSquared)
            return;

        float scale = config.maxSpeed / Mathf.Sqrt(speedSquared);
        u *= scale;
        v *= scale;
    }

    public void AddForceX(float x) { ax += x; }
    public void AddForceY(float y) { ay += y; }
}
