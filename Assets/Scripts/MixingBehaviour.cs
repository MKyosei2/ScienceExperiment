using UdonSharp;
using UnityEngine;

public class MixingBehaviour : FlaskBehaviour
{
    public Transform liquidSurface;
    private bool isActive = false;

    public override void OnElementApplied(int elementId)
    {
        isActive = true;
    }

    public override void Update()
    {
        if (isActive && liquidSurface != null)
        {
            float angle = Mathf.Sin(Time.time * 2f) * 10f;
            liquidSurface.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
