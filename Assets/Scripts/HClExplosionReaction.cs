using UdonSharp;
using UnityEngine;

public class HClExplosionReaction : UdonSharpBehaviour
{
    [Header("Detection (flask root object names)")]
    public string flaskHName = "CONICAL_FLASK_H";
    public string flaskClName = "CONICAL_FLASK_Cl";

    [Header("Explosion Light")]
    public Light explosionLight;
    public float peakIntensity = 8f;
    public float duration = 0.25f;

    [Header("Optional Effects")]
    public AudioSource sfx;              // 任意
    public ParticleSystem burstVfx;      // 任意

    private bool _hasH;
    private bool _hasCl;
    private bool _fired;

    private float _t;

    private void Start()
    {
        if (explosionLight != null)
        {
            explosionLight.enabled = false;
            explosionLight.intensity = 0f;
        }
    }

    // ★UdonSharpでは override しない（これがコンパイルエラーの原因）
    public void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (other == null) return;

        string rootName = GetRootName(other.transform);

        if (rootName == flaskHName) _hasH = true;
        if (rootName == flaskClName) _hasCl = true;

        if (_hasH && _hasCl)
        {
            FireExplosion();
        }
    }

    private void FireExplosion()
    {
        _fired = true;

        if (burstVfx != null) burstVfx.Play();
        if (sfx != null) sfx.Play();

        _t = 0f;

        if (explosionLight != null)
        {
            explosionLight.enabled = true;
            explosionLight.intensity = peakIntensity;
        }

        // 毎フレーム更新（UdonのUpdateより安全に制御したいのでイベントループ）
        SendCustomEventDelayedFrames(nameof(FlashUpdate), 1);
    }

    public void FlashUpdate()
    {
        if (explosionLight == null) return;

        _t += Time.deltaTime;

        // 減衰（指数）
        float safeDuration = Mathf.Max(0.01f, duration);
        float k = 10f / safeDuration;
        float intensity = peakIntensity * Mathf.Exp(-k * _t);

        explosionLight.intensity = intensity;

        if (_t >= safeDuration || intensity <= 0.05f)
        {
            explosionLight.intensity = 0f;
            explosionLight.enabled = false;
            return;
        }

        SendCustomEventDelayedFrames(nameof(FlashUpdate), 1);
    }

    private string GetRootName(Transform t)
    {
        if (t == null) return "";

        Transform cur = t;
        Transform last = t;

        while (cur != null)
        {
            last = cur;

            if (cur.name == flaskHName || cur.name == flaskClName)
                return cur.name;

            cur = cur.parent;
        }

        return last != null ? last.name : "";
    }
}
