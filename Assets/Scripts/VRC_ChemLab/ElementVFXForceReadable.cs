using UdonSharp;
using UnityEngine;

public class ElementVFXForceReadable : UdonSharpBehaviour
{
    [Header("Assign (on the VFX root object)")]
    public Renderer[] targetRenderers;               // Solid/Liquid/Gas のRendererなど（空なら自動収集）
    public ParticleSystem[] particleSystems;         // 泡/ミスト/火花（空なら自動収集）
    public MeshFilter targetMeshFilter;              // キューブのMeshFilter（ある場合）

    [Header("Readability (do NOT scale up)")]
    [Range(0f, 1f)] public float minFill = 0.65f;    // 最低でも半分以上に見える
    public float emissionMin = 0.8f;                 // 発光最低値（ガラスと同化防止）
    public float opacityMin = 0.75f;                // 不透明度最低値
    public float densityPerM3 = 400f;                // 容積に比例して粒子量を増やす（満杯感）
    public float maxRateOverTime = 2500f;            // 粒子上限（重すぎ防止）

    private bool _applied;

    private void Start()
    {
        ApplyNow();
    }

    public void ApplyNow()
    {
        if (_applied) return;

        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(true);

        if (particleSystems == null || particleSystems.Length == 0)
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        if (targetMeshFilter == null)
            targetMeshFilter = GetComponentInChildren<MeshFilter>(true);

        // 1) 親器具の ElementEffectAnchor を探す
        Transform anchor = FindAncestorByName(transform, "ElementEffectAnchor");
        if (anchor == null)
        {
            // それでも最低限見えるようにする
            ForceMaterialReadable();
            BoostParticlesByFixedRate();
            _applied = true;
            return;
        }

        // 2) VFXVolumeMesh があればキューブメッシュを器具メッシュへ差し替え（変化が出る）
        Transform vfxMesh = anchor.Find("VFXVolumeMesh");
        if (vfxMesh != null && targetMeshFilter != null)
        {
            MeshFilter src = (MeshFilter)vfxMesh.GetComponent(typeof(MeshFilter));
            if (src != null && src.sharedMesh != null)
            {
                // Copy mesh
                targetMeshFilter.sharedMesh = src.sharedMesh;

                // Also copy transform so the shader volume matches the instrument shape/size.
                // (The VFXVolumeMesh itself is already slightly shrunk by the spawner.)
                Transform dstTr = targetMeshFilter.transform;
                dstTr.position = vfxMesh.position;
                dstTr.rotation = vfxMesh.rotation;

                Vector3 parentLossy = dstTr.parent != null ? dstTr.parent.lossyScale : Vector3.one;
                Vector3 targetLossy = vfxMesh.lossyScale;
                Vector3 local = dstTr.localScale;
                if (parentLossy.x != 0f) local.x = targetLossy.x / parentLossy.x;
                if (parentLossy.y != 0f) local.y = targetLossy.y / parentLossy.y;
                if (parentLossy.z != 0f) local.z = targetLossy.z / parentLossy.z;
                dstTr.localScale = local;
            }
        }

        // 3) VFXVolume(BoxCollider) 容積から粒子密度を決める（拡大しないで満杯感）
        float rate = 400f;
        Transform vfxVol = anchor.Find("VFXVolume");
        if (vfxVol != null)
        {
            BoxCollider box = (BoxCollider)vfxVol.GetComponent(typeof(BoxCollider));
            if (box != null)
            {
                Vector3 s = box.size;
                float volume = Mathf.Max(0.001f, s.x * s.y * s.z);
                rate = Mathf.Min(maxRateOverTime, volume * densityPerM3);
            }
        }

        BoostParticles(rate);

        // 4) マテリアルを「見える」方向へ強制（ガラス内で同化しない）
        ForceMaterialReadable();

        _applied = true;
    }

    private void BoostParticles(float rateOverTime)
    {
        if (particleSystems == null) return;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null) continue;

            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = rateOverTime;

            // 粒子が小さすぎると見えないので、サイズ/寿命を最低保証
            var main = ps.main;
            if (main.startLifetime.constant < 0.7f) main.startLifetime = 0.7f;
            if (main.startSize.constant < 0.02f) main.startSize = 0.02f;
            if (main.startSpeed.constant < 0.03f) main.startSpeed = 0.03f;

            ps.Play();
        }
    }

    private void BoostParticlesByFixedRate()
    {
        BoostParticles(600f);
    }

    private void ForceMaterialReadable()
    {
        if (targetRenderers == null) return;

        // 注意：共有マテリアルは壊したくないので MaterialPropertyBlock だけで上書き
        var mpb = new MaterialPropertyBlock();

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var r = targetRenderers[i];
            if (r == null) continue;

            r.enabled = true;
            r.GetPropertyBlock(mpb);

            // よくあるプロパティ名に幅広く対応（あなたのシェーダ名が違っても効く可能性を上げる）
            SetIfExists(mpb, "_Opacity", opacityMin);
            SetIfExists(mpb, "_Alpha", opacityMin);
            SetIfExists(mpb, "_EmissionStrength", emissionMin);
            SetIfExists(mpb, "_Emission", emissionMin);
            SetIfExists(mpb, "_Glow", emissionMin);

            // FillLevel を持つシェーダなら半分以上へ
            SetIfExists(mpb, "_FillLevel", Mathf.Max(minFill, 0.55f));

            r.SetPropertyBlock(mpb);
        }
    }

    private static void SetIfExists(MaterialPropertyBlock mpb, string name, float value)
    {
        // PropertyBlock に存在確認APIがないので、例外は出ない→そのままセットでOK
        mpb.SetFloat(name, value);
    }

    private static Transform FindAncestorByName(Transform t, string name)
    {
        Transform cur = t;
        for (int i = 0; i < 20 && cur != null; i++)
        {
            if (cur.name == name) return cur;
            cur = cur.parent;
        }
        return null;
    }
}
