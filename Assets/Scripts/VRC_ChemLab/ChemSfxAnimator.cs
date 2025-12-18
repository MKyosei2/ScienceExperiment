using UdonSharp;
using UnityEngine;

public class ChemSfxAnimator : UdonSharpBehaviour
{
    [Header("Input (AI)")]
    public AIRequestSender ai;

    [Header("Loop Sources (optional)")]
    public AudioSource bubbleLoop;   // 泡/攪拌
    public AudioSource hissLoop;     // 煙/ガス
    public AudioSource crackleLoop;  // 加熱/火花
    public AudioSource humLoop;      // 発光

    [Header("OneShot (optional)")]
    public AudioSource oneShotSource;
    public AudioClip sparkPop;
    public AudioClip bubblePop;
    public AudioClip whoosh;

    [Header("Tuning")]
    [Range(0.1f, 30f)] public float smoothing = 10f;
    [Range(0f, 1f)] public float minAudible = 0.04f;
    [Range(0f, 1f)] public float sparkOneShotThreshold = 0.65f;
    [Range(0f, 1f)] public float bubbleOneShotThreshold = 0.75f;

    private float _vBubble, _vHiss, _vCrackle, _vHum;
    private float _prevSpark, _prevFoam, _prevHeat;

    private void Start()
    {
        EnsureLoop(bubbleLoop);
        EnsureLoop(hissLoop);
        EnsureLoop(crackleLoop);
        EnsureLoop(humLoop);
    }

    private void Update()
    {
        if (ai == null) return;

        float dt = Time.deltaTime;
        float k = 1f - Mathf.Exp(-smoothing * dt);

        // AIのfx値→音の“意味”へ変換（学習記号×リアルの中間）
        float targetBubble = Mathf.Clamp01(ai.fxFoam * 0.85f + ai.fxWave * 0.60f);
        float targetHiss = Mathf.Clamp01(ai.fxSmoke);
        float targetCrackle = Mathf.Clamp01(ai.fxHeat * 0.70f + ai.fxSpark * 0.90f);
        float targetHum = Mathf.Clamp01(ai.fxGlow);

        _vBubble = Mathf.Lerp(_vBubble, targetBubble, k);
        _vHiss = Mathf.Lerp(_vHiss, targetHiss, k);
        _vCrackle = Mathf.Lerp(_vCrackle, targetCrackle, k);
        _vHum = Mathf.Lerp(_vHum, targetHum, k);

        ApplyLoop(bubbleLoop, _vBubble, 0.95f, 1.15f);
        ApplyLoop(hissLoop, _vHiss, 0.95f, 1.10f);
        ApplyLoop(crackleLoop, _vCrackle, 0.90f, 1.20f);
        ApplyLoop(humLoop, _vHum, 0.90f, 1.10f);

        // “イベント感”のあるワンショット（火花/泡/立ち上がり）
        if (CrossUp(_prevSpark, ai.fxSpark, sparkOneShotThreshold) && sparkPop != null)
            PlayOneShot(sparkPop, ai.fxSpark);

        if (CrossUp(_prevFoam, ai.fxFoam, bubbleOneShotThreshold) && bubblePop != null)
            PlayOneShot(bubblePop, ai.fxFoam);

        if (CrossUp(_prevHeat, ai.fxHeat, 0.55f) && whoosh != null)
            PlayOneShot(whoosh, ai.fxHeat);

        _prevSpark = ai.fxSpark;
        _prevFoam = ai.fxFoam;
        _prevHeat = ai.fxHeat;
    }

    private void EnsureLoop(AudioSource a)
    {
        if (a == null) return;
        a.loop = true;
        a.playOnAwake = false;
        a.volume = 0f;
        a.Pause();
    }

    private void ApplyLoop(AudioSource a, float v01, float pitchMin, float pitchMax)
    {
        if (a == null) return;

        float vol = Mathf.Clamp01(v01);
        a.volume = vol;
        a.pitch = Mathf.Lerp(pitchMin, pitchMax, vol);

        if (vol > minAudible)
        {
            if (!a.isPlaying) a.Play();
        }
        else
        {
            if (a.isPlaying) a.Pause();
        }
    }

    private bool CrossUp(float prev, float now, float th) => prev < th && now >= th;

    private void PlayOneShot(AudioClip clip, float intensity01)
    {
        if (oneShotSource == null || clip == null) return;

        oneShotSource.pitch = Mathf.Lerp(0.95f, 1.15f, Mathf.Clamp01(intensity01));
        // Udon環境でも PlayOneShot は概ね安全。もし環境差が出たら clip差し替え+Play に変更。
        oneShotSource.PlayOneShot(clip, Mathf.Clamp01(intensity01));
    }
}
