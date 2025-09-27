using UdonSharp;
using UnityEngine;

// --- 状態を表す列挙型 ---
public enum ElementState
{
    Solid = 0,
    Liquid = 1,
    Gas = 2
}

public class ChemVisualController : UdonSharpBehaviour
{
    private Renderer _rend;
    private Material _mat;

    private ParticleSystem steamParticles;
    private ParticleSystem bubbleParticles;
    private ParticleSystem mistParticles;
    private ParticleSystem dropletParticles;

    [Header("現在の状態（自動判定される）")]
    public ElementState currentState = ElementState.Liquid;

    [Header("粒子エフェクトのPrefab（必ず設定してください）")]
    public GameObject steamPrefab;
    public GameObject bubblePrefab;
    public GameObject mistPrefab;
    public GameObject dropletPrefab;

    void Start()
    {
        _rend = GetComponentInChildren<Renderer>();
        if (_rend == null) _rend = GetComponent<Renderer>();
        if (_rend != null) _mat = _rend.material;

        CreateParticles();
    }

    public void SetElementAppearance(Color elementColor, ElementState state)
    {
        currentState = state;

        if (_mat == null) return;
        if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", elementColor);
        if (_mat.HasProperty("_WireColor")) _mat.SetColor("_WireColor", elementColor * 1.2f);
        if (_mat.HasProperty("_FillEnabled")) _mat.SetFloat("_FillEnabled", 1f);
    }

    public void UpdateEnvironment(float temperature, float humidity, float pressure)
    {
        if (_mat == null) return;

        if (_mat.HasProperty("_Humidity")) _mat.SetFloat("_Humidity", humidity);
        if (_mat.HasProperty("_ColorShift"))
            _mat.SetFloat("_ColorShift", Mathf.Clamp(pressure / 5f, -1f, 1f));

        if (currentState == ElementState.Liquid)
            ApplyLiquidBehavior(temperature, humidity, pressure);
        else if (currentState == ElementState.Solid)
            ApplySolidBehavior(humidity);
        else if (currentState == ElementState.Gas)
            ApplyGasBehavior(humidity, pressure);
    }

    private void ApplyLiquidBehavior(float temperature, float humidity, float pressure)
    {
        if (_mat.HasProperty("_GlowIntensity"))
            _mat.SetFloat("_GlowIntensity", Mathf.Clamp01((temperature - 20f) / 80f) * 2f);

        if (steamParticles != null)
        {
            var e = steamParticles.emission;
            var m = steamParticles.main;
            e.rateOverTime = Mathf.Lerp(0, 60, Mathf.Clamp01((temperature - 60f) / 40f));
            m.startSpeed = 0.5f + pressure * 0.2f;
        }
        if (bubbleParticles != null)
        {
            var e = bubbleParticles.emission;
            e.rateOverTime = Mathf.Lerp(0, 120, Mathf.Clamp01((temperature - 90f) / 20f));
        }
        if (mistParticles != null)
        {
            var e = mistParticles.emission;
            e.rateOverTime = Mathf.Lerp(0, 40, humidity);
        }
        if (dropletParticles != null)
        {
            var e = dropletParticles.emission;
            e.rateOverTime = Mathf.Lerp(0, 25, humidity);
        }
    }

    private void ApplySolidBehavior(float humidity)
    {
        if (mistParticles != null)
        {
            var e = mistParticles.emission;
            e.rateOverTime = Mathf.Lerp(0, 20, humidity);
        }
        if (dropletParticles != null)
        {
            var e = dropletParticles.emission;
            e.rateOverTime = Mathf.Lerp(0, 15, humidity);
        }
        if (steamParticles != null) { var e = steamParticles.emission; e.rateOverTime = 0; }
        if (bubbleParticles != null) { var e = bubbleParticles.emission; e.rateOverTime = 0; }
    }

    private void ApplyGasBehavior(float humidity, float pressure)
    {
        if (steamParticles != null)
        {
            var e = steamParticles.emission;
            var m = steamParticles.main;
            e.rateOverTime = Mathf.Lerp(20, 80, pressure);
            m.startSpeed = 1f + pressure * 0.5f;
        }
        if (mistParticles != null)
        {
            var e = mistParticles.emission;
            e.rateOverTime = Mathf.Lerp(0, 60, humidity);
        }
        if (bubbleParticles != null) { var e = bubbleParticles.emission; e.rateOverTime = 0; }
        if (dropletParticles != null) { var e = dropletParticles.emission; e.rateOverTime = 0; }
    }

    private void CreateParticles()
    {
        if (steamPrefab != null)
        {
            var obj = VRCInstantiate(steamPrefab);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(0, 0.5f, 0);
            steamParticles = obj.GetComponent<ParticleSystem>();
        }

        if (bubblePrefab != null)
        {
            var obj = VRCInstantiate(bubblePrefab);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = Vector3.zero;
            bubbleParticles = obj.GetComponent<ParticleSystem>();
        }

        if (mistPrefab != null)
        {
            var obj = VRCInstantiate(mistPrefab);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(0, 0.5f, 0);
            mistParticles = obj.GetComponent<ParticleSystem>();
        }

        if (dropletPrefab != null)
        {
            var obj = VRCInstantiate(dropletPrefab);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = Vector3.zero;
            dropletParticles = obj.GetComponent<ParticleSystem>();
        }
    }
}
