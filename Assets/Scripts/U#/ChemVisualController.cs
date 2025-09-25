using UdonSharp;
using UnityEngine;

public enum ElementState { Liquid, Solid, Gas }

public class ChemVisualController : UdonSharpBehaviour
{
    private Renderer rend;
    private Material matInstance;

    private ParticleSystem liquidParticles;
    private ParticleSystem solidParticles;
    private ParticleSystem gasParticles;

    private MeshFilter mf;
    private Vector3[] vertices;
    private Vector3 center;
    private float bottomY;
    private float topY;

    [Header("状態管理")]
    public ElementState currentState = ElementState.Liquid;
    public float volume = 1.0f; // 1 = 満杯, 0 = 空

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null) matInstance = rend.material;

        mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            vertices = mf.sharedMesh.vertices;
            CalcBounds();
        }

        CreateParticles();
    }

    private void CalcBounds()
    {
        if (vertices == null) return;

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        Vector3 sum = Vector3.zero;

        foreach (var v in vertices)
        {
            Vector3 worldV = transform.TransformPoint(v);
            sum += worldV;
            if (worldV.y < minY) minY = worldV.y;
            if (worldV.y > maxY) maxY = worldV.y;
        }

        bottomY = minY;
        topY = maxY;
        center = sum / vertices.Length;
    }

    private void CreateParticles()
    {
        // --- 液体 (泡＋蒸気) ---
        GameObject liquidObj = new GameObject("LiquidParticles");
        liquidObj.transform.SetParent(transform, false);
        liquidParticles = liquidObj.AddComponent<ParticleSystem>();
        var mainL = liquidParticles.main;
        mainL.startLifetime = 1f;
        mainL.startSize = 0.05f;
        mainL.loop = true;
        liquidParticles.emission.rateOverTime = 0;
        liquidParticles.Play();

        // --- 固体 (粉末) ---
        GameObject solidObj = new GameObject("SolidParticles");
        solidObj.transform.SetParent(transform, false);
        solidParticles = solidObj.AddComponent<ParticleSystem>();
        var mainS = solidParticles.main;
        mainS.startLifetime = 2f;
        mainS.startSize = 0.05f;
        mainS.gravityModifier = 1f; // 下に落ちる
        solidParticles.emission.rateOverTime = 0;
        solidParticles.Play();

        // --- 気体 ---
        GameObject gasObj = new GameObject("GasParticles");
        gasObj.transform.SetParent(transform, false);
        gasParticles = gasObj.AddComponent<ParticleSystem>();
        var mainG = gasParticles.main;
        mainG.startLifetime = 3f;
        mainG.startSize = 0.1f;
        mainG.startSpeed = 0.5f;
        mainG.gravityModifier = -0.05f; // 上昇
        gasParticles.emission.rateOverTime = 0;
        gasParticles.Play();
    }

    public void UpdateEnvironment(float temperature, float pressure)
    {
        if (matInstance == null) return;

        // 状態に応じてパーティクルON/OFF
        liquidParticles.gameObject.SetActive(currentState == ElementState.Liquid);
        solidParticles.gameObject.SetActive(currentState == ElementState.Solid);
        gasParticles.gameObject.SetActive(currentState == ElementState.Gas);

        // 液体: 傾きと時間で volume 減少
        if (currentState == ElementState.Liquid)
        {
            float tilt = Vector3.Angle(transform.up, Vector3.up) / 90f; // 0=直立,1=横倒し
            float leakRate = tilt * 0.2f; // 横倒しで最大 0.2/sec 流出
            volume = Mathf.Max(0, volume - leakRate * Time.deltaTime);

            var emissionL = liquidParticles.emission;
            emissionL.rateOverTime = 50 * volume;

            matInstance.SetFloat("_Evaporation", 1 - volume);
        }

        // 固体: 倒すと時間で減る
        if (currentState == ElementState.Solid)
        {
            float tilt = Vector3.Angle(transform.up, Vector3.up) / 90f;
            float leakRate = tilt * 0.1f;
            volume = Mathf.Max(0, volume - leakRate * Time.deltaTime);

            var emissionS = solidParticles.emission;
            emissionS.rateOverTime = 80 * (1 - volume);
        }

        // 気体: 常に拡散
        if (currentState == ElementState.Gas)
        {
            var emissionG = gasParticles.emission;
            emissionG.rateOverTime = 100 * volume;

            // 圧力が高いほど速く広がる
            var mainG = gasParticles.main;
            mainG.startSpeed = 0.5f + pressure * 0.2f;

            // 減衰
            volume = Mathf.Max(0, volume - 0.05f * Time.deltaTime);
        }
    }
}
