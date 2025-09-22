using UdonSharp;
using UnityEngine;

public class CoolingBehaviour : FlaskBehaviour
{
    public Light glowLight;

    public override void OnElementApplied(int elementId)
    {
        base.OnElementApplied(elementId);
        if (glowLight != null)
        {
            glowLight.enabled = true;
            glowLight.color = Color.cyan;
        }
    }

    public override void Update()
    {
        if (!isActive) return;
        if (glowLight != null)
        {
            glowLight.intensity = 1f + Mathf.Sin(Time.time * 3f) * 0.3f;
        }
    }
}
