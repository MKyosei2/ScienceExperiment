using UdonSharp;
using UnityEngine;

public enum ElementState { Solid, Liquid, Gas }

public class ChemVisualController : UdonSharpBehaviour
{
    [Header("State Visuals (optional)")]
    public GameObject solidObj;   // 子に "Solid" があるなら未指定でOK
    public GameObject liquidObj;  // 子に "Liquid"
    public GameObject gasObj;     // 子に "Gas"

    [Header("Color Apply")]
    public Renderer[] targetRenderers; // 未指定なら子Rendererを自動取得

    private void Start()
    {
        if (solidObj == null) solidObj = FindChild("Solid");
        if (liquidObj == null) liquidObj = FindChild("Liquid");
        if (gasObj == null) gasObj = FindChild("Gas");

        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private GameObject FindChild(string name)
    {
        Transform t = transform.Find(name);
        return t != null ? t.gameObject : null;
    }

    public void SetElementAppearance(Color color, ElementState state)
    {
        if (solidObj != null) solidObj.SetActive(state == ElementState.Solid);
        if (liquidObj != null) liquidObj.SetActive(state == ElementState.Liquid);
        if (gasObj != null) gasObj.SetActive(state == ElementState.Gas);

        ApplyColor(color);
    }

    public void UpdateEnvironment(float tempC, float hum, float pres)
    {
        // 任意：シェーダー側が対応していれば反映
        if (targetRenderers == null) return;

        float glow = Mathf.Clamp01(tempC / 120f);
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var r = targetRenderers[i];
            if (r == null) continue;
            var m = r.material;
            if (m == null) continue;

            if (m.HasProperty("_Humidity")) m.SetFloat("_Humidity", hum);
            if (m.HasProperty("_Pressure")) m.SetFloat("_Pressure", pres);
            if (m.HasProperty("_GlowIntensity")) m.SetFloat("_GlowIntensity", glow);
        }
    }

    private void ApplyColor(Color c)
    {
        if (targetRenderers == null) return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var r = targetRenderers[i];
            if (r == null) continue;
            var m = r.material;
            if (m == null) continue;

            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }
}
