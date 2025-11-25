using UdonSharp;
using UnityEngine;

public class ChemReactionAnimator : UdonSharpBehaviour
{
    public void PlayHeat(ParticleSystem ps)
    {
        var m = ps.main;
        m.startColor = Color.red;
    }

    public void PlayCold(ParticleSystem ps)
    {
        var m = ps.main;
        m.startColor = new Color(0.6f, 0.8f, 1f, 0.4f);
    }

    public void PlayFoam(ParticleSystem ps)
    {
        var em = ps.emission;
        em.rateOverTime = 200f;
    }

    public void PlaySpark(ParticleSystem ps)
    {
        var m = ps.main;
        m.startColor = Color.yellow;
    }
}
