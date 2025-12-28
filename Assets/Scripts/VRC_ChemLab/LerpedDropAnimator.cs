using UdonSharp;
using UnityEngine;

/// <summary>
/// LerpedDropAnimator (Async-only)
/// ChemVisualController から SendCustomEvent("_PlayDrop") で起動される想定。
/// tokenObject を start→end へ補間移動して「投入」っぽく見せる。
/// </summary>
public class LerpedDropAnimator : UdonSharpBehaviour
{
    [Header("References")]
    public ChemVisualController visual;   // optional（色取得用）
    public Transform start;
    public Transform end;

    [Header("Animated Object")]
    public GameObject tokenObject;        // 使い回す（Instantiateしない）
    public Renderer[] tokenRenderers;     // 色を当てたい場合

    [Header("Motion")]
    public float duration = 0.45f;
    public AnimationCurve curve;          // 任意（未設定ならSmoothStep）

    private bool _playing;
    private float _t;
    private Vector3 _p0;
    private Vector3 _p1;

    private void Start()
    {
        if (tokenObject != null) tokenObject.SetActive(false);
        if (tokenRenderers == null || tokenRenderers.Length == 0)
        {
            if (tokenObject != null)
                tokenRenderers = tokenObject.GetComponentsInChildren<Renderer>(true);
        }
    }

    public void _PlayDrop()
    {
        if (tokenObject == null || start == null || end == null) return;

        _p0 = start.position;
        _p1 = end.position;
        _t = 0f;
        _playing = true;

        tokenObject.transform.position = _p0;
        tokenObject.SetActive(true);

        // 色を反映（任意）
        if (visual != null)
        {
            ApplyColor(visual.lastSelectedColor);
        }
    }

    private void Update()
    {
        if (!_playing) return;

        _t += Time.deltaTime;
        float u = duration <= 0.001f ? 1f : Mathf.Clamp01(_t / duration);

        float k = (curve != null && curve.length > 0) ? curve.Evaluate(u) : Smooth(u);
        tokenObject.transform.position = Vector3.Lerp(_p0, _p1, k);

        if (u >= 1f)
        {
            _playing = false;
            tokenObject.SetActive(false);
        }
    }

    private void ApplyColor(Color c)
    {
        if (tokenRenderers == null) return;

        for (int i = 0; i < tokenRenderers.Length; i++)
        {
            var r = tokenRenderers[i];
            if (r == null) continue;
            var m = r.material;
            if (m == null) continue;

            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }

    private float Smooth(float t)
    {
        // SmoothStep
        return t * t * (3f - 2f * t);
    }
}
