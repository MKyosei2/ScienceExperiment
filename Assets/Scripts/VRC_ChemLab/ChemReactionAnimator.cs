using UdonSharp;
using UnityEngine;

public class ChemReactionAnimator : UdonSharpBehaviour
{
    public Material glowMat;       // 発光
    public Material explosionMat;  // 爆発
    public Material waveMat;       // 波紋

    // ============================
    // 中和 → 泡
    // ============================
    public void PlayFoam(ParticleSystem ps)
    {
        var em = ps.emission;
        em.rateOverTime = 200f;

        var main = ps.main;
        Color c = main.startColor.color;
        c.a = 1f;
        main.startColor = c;
    }

    // ============================
    // 酸化 → 加熱
    // ============================
    public void PlayHeat(ParticleSystem ps)
    {
        var main = ps.main;
        main.startColor = new Color(1f, 0.4f, 0.2f, 1f);

        var em = ps.emission;
        em.rateOverTime = 120f;
    }

    // ============================
    // 金属反応 → 火花
    // ============================
    public void PlaySpark(ParticleSystem ps)
    {
        var main = ps.main;
        main.startColor = Color.yellow;

        var em = ps.emission;
        em.rateOverTime = 150f;
    }

    // ============================
    // 発光反応（例：Na・Xe）
    // ============================
    public void PlayGlow(GameObject flask)
    {
        MeshRenderer mr = flask.transform.Find("Model").GetComponent<MeshRenderer>();
        mr.material = glowMat;

        glowMat.SetFloat("_GlowIntensity", 6f);
    }

    // ============================
    // 波動反応（ゆらぎ）
    // ============================
    public void PlayWave(ParticleSystem ps)
    {
        ParticleSystemRenderer pr = ps.GetComponent<ParticleSystemRenderer>();
        pr.material = waveMat;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 2.5f;
    }
}
