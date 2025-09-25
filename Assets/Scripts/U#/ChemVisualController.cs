using UdonSharp;
using UnityEngine;

public class ChemVisualController : UdonSharpBehaviour
{
    private Renderer rend;
    private Material matInstance;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            matInstance = rend.material;
        }
    }

    public void SetElementAppearance(Color elementColor)
    {
        if (matInstance == null) return;

        if (matInstance.HasProperty("_BaseColor"))
            matInstance.SetColor("_BaseColor", elementColor);

        if (matInstance.HasProperty("_WireColor"))
            matInstance.SetColor("_WireColor", elementColor * 1.2f);

        if (matInstance.HasProperty("_GlowIntensity"))
            matInstance.SetFloat("_GlowIntensity", 1.0f);
    }
}
