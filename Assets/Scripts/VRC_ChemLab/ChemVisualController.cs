using UdonSharp;
using UnityEngine;

public enum ElementState { Solid, Liquid, Gas }

public class ChemVisualController : UdonSharpBehaviour
{
    private Renderer rend;
    private Material mat;

    void Start()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            rend = GetComponent<Renderer>();
        }
        if (rend != null)
        {
            mat = rend.material;
        }
    }

    public void SetElementAppearance(Color color, ElementState state)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_WireColor")) mat.SetColor("_WireColor", color * 1.2f);
    }

    public void UpdateEnvironment(float temp, float hum, float pres)
    {
        if (mat == null) return;
        if (mat.HasProperty("_Humidity")) mat.SetFloat("_Humidity", hum);
        if (mat.HasProperty("_GlowIntensity")) mat.SetFloat("_GlowIntensity", Mathf.Clamp01(temp / 100f));
    }
}
