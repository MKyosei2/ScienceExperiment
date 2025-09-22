using UdonSharp;
using UnityEngine;

public class BoilingBehaviour : FlaskBehaviour
{
    public ParticleSystem bubbleEffect;

    public override void OnElementApplied(int elementId)
    {
        base.OnElementApplied(elementId);
        if (bubbleEffect != null) bubbleEffect.Play();
    }

    public override void Update()
    {
        if (!isActive) return;
        if (bubbleEffect != null && !bubbleEffect.isPlaying)
        {
            bubbleEffect.Play();
        }
    }
}
