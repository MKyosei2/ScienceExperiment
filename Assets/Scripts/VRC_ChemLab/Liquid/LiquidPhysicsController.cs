using UdonSharp;
using UnityEngine;
using VRC.Udon;

public class LiquidPhysicsController : UdonSharpBehaviour
{
    public ParticleSystem particle;
    public LiquidSurfaceController surface;

    public void ApplyPhysics(float viscosity, float density)
    {
        if (particle != null)
        {
            var main = particle.main;
            main.startSpeed = Mathf.Clamp(0.03f / viscosity, 0.005f, 0.05f);

            var noise = particle.noise;
            noise.strength = Mathf.Clamp(density * 0.1f, 0.05f, 0.3f);
        }

        if (surface != null)
        {
            surface.SetViscosity(viscosity);
        }
    }
}
