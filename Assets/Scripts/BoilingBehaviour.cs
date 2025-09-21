using UdonSharp;
using UnityEngine;

public class BoilingBehaviour : FlaskBehaviour
{
    public ParticleSystem bubbleEffect;
    private bool isActive = false;

    public override void OnElementApplied(int elementId)
    {
        isActive = true;
        if (bubbleEffect != null) bubbleEffect.Play();
    }

    public override void Update()
    {
        if (isActive && bubbleEffect != null && !bubbleEffect.isPlaying)
        {
            bubbleEffect.Play();
        }
    }
}
