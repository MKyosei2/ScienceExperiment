using UdonSharp;
using UnityEngine;

public class LiquidBoilingController : UdonSharpBehaviour
{
    public ParticleSystem bubbles;

    public void UpdateBoiling(float temp)
    {
        var em = bubbles.emission;

        if (temp < 60f)
        {
            em.rateOverTime = 0;
            return;
        }

        float rate = (temp - 60f) * 1.8f;
        em.rateOverTime = Mathf.Clamp(rate, 0f, 60f);
    }
}
