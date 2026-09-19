# Particle Life

A Unity implementation of a particle-life simulation: particles are assigned a type and move according to a configurable attraction/repulsion matrix between types.

The simulation uses Unity `NativeArray` data, Burst-compiled jobs, and a uniform spatial grid to organise particle interaction work. The project contains both CPU and GPU renderer scripts, plus a custom editor for `ParticleConfig`.

## Opening the project

Open this repository as a Unity project, then load `Assets/Scenes/SampleScene.unity`.

The scene uses the configuration asset and scripts under `Assets/Scripts`. Change particle count, interaction weights, damping, radius, and bounds through the inspector.

## Notes

This is an interactive visual simulation rather than a physical particle model. The force law is deliberately tuned for emergent patterns.
