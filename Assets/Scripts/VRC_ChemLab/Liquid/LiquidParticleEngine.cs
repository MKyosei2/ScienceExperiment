using UdonSharp;
using UnityEngine;

public class LiquidParticleEngine : UdonSharpBehaviour
{
    public ParticleSystem particle;

    public void InitEngine()
    {
        if (particle == null) return;

        var main = particle.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = 0f;
        main.startSpeed = 0.00f;
        main.startSize = 0.025f;
        main.startLifetime = 5f;

        var shape = particle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.38f, 0.78f, 0.38f);

        var noise = particle.noise;
        noise.enabled = true;
        noise.strength = 0.05f;
        noise.frequency = 0.4f;
        noise.scrollSpeed = 0.1f;

        var col = particle.collision;
        col.enabled = false;
    }

    public void UpdateStrength(float strength, float freq)
    {
        var noise = particle.noise;
        noise.strength = new ParticleSystem.MinMaxCurve(strength);
        noise.frequency = freq;
    }

    public void SetColor(Color c)
    {
        var main = particle.main;
        main.startColor = c;
    }

    public void SetViscosity(float v)
    {
        var main = particle.main;
        main.startSpeed = Mathf.Clamp(0.015f / v, 0.001f, 0.02f);
    }

    public void SetDensity(float d)
    {
        var noise = particle.noise;
        noise.strength = new ParticleSystem.MinMaxCurve(Mathf.Clamp(d * 0.05f, 0.01f, 0.10f));
    }
}
