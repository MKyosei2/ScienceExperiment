using UdonSharp;
using UnityEngine;

public class ChemVisualController : UdonSharpBehaviour
{
    private Renderer rend;
    private Material matInstance;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null) matInstance = rend.material;
    }

    public void SetElementAppearance(Color c)
    {
        if (matInstance == null) return;
        if (matInstance.HasProperty("_BaseColor")) matInstance.SetColor("_BaseColor", c);
        if (matInstance.HasProperty("_WireColor")) matInstance.SetColor("_WireColor", c * 1.2f);
    }
}
