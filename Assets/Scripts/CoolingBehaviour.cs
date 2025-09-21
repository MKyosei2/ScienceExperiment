using UdonSharp;
using UnityEngine;

public class CoolingBehaviour : FlaskBehaviour
{
    public Light glowLight;
    private bool isActive = false;

    public override void OnElementApplied(int elementId)
    {
        isActive = true;
        if (glowLight != null)
        {
            glowLight.enabled = true;
            glowLight.color = Color.cyan;
        }
    }

    public override void Update()
    {
        if (isActive && glowLight != null)
        {
            glowLight.intensity = 1f + Mathf.Sin(Time.time * 3f) * 0.3f; // ƒ`ƒ‰ƒ`ƒ‰Œõ‚é
        }
    }
}
