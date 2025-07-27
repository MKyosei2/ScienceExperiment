using UdonSharp;
using UnityEngine;
using VRC.Udon;

public enum StepType
{
    EmissionChange,
    MoveElement,
    ShaderEffect
}

public class VisualExperimentPlayer : UdonSharpBehaviour
{
    public StepType[] stepTypes;
    public GameObject[] stepTargets;
    public float[] stepDurations;

    public Color[] emissionColors;
    public Vector3[] moveOffsets;
    public string[] shaderProperties;
    public float[] shaderValues;

    public Renderer reactionRenderer;
    public VRExperimentMonitor monitor;

    public GameObject naClPrefab;
    public Transform beakerTransform;

    private int currentStep = 0;
    private bool isPlaying = false;

    // スムーズ移動用
    private GameObject moveTarget;
    private Vector3 moveStart;
    private Vector3 moveEnd;
    private float moveDuration;
    private float moveElapsed;
    private bool isMoving = false;

    // 追加: Na/Clの参照を保持（シーケンス開始時にセット）
    private GameObject naObj;
    private GameObject clObj;

    public void PlaySequence()
    {
        Debug.Log("🎬 PlaySequence呼び出し");

        if (stepTypes == null || stepTargets == null || stepTypes.Length == 0)
        {
            Debug.LogError("❌ stepTypes または stepTargets が未設定です");
            return;
        }

        if (isPlaying)
        {
            Debug.Log("⚠️ 演出がすでに実行中です");
            return;
        }

        // Na/Clの参照を記録
        naObj = stepTargets.Length > 0 ? stepTargets[0] : null;
        clObj = stepTargets.Length > 1 ? stepTargets[1] : null;

        isPlaying = true;
        currentStep = 0;
        SendCustomEventDelayedFrames(nameof(PlayNextStep), 1);
    }

    public void PlayNextStep()
    {
        if (currentStep >= stepTypes.Length)
        {
            Debug.Log("✅ 全ステップ完了");
            isPlaying = false;

            // NaとClを削除
            if (naObj != null)
            {
                Destroy(naObj);
                Debug.Log("🧪 Naオブジェクトを削除しました");
            }
            if (clObj != null)
            {
                Destroy(clObj);
                Debug.Log("🧪 Clオブジェクトを削除しました");
            }

            // NaCl生成（ビーカーの中）
            if (naClPrefab != null && beakerTransform != null)
            {
                Vector3 spawnPos = beakerTransform.position + Vector3.up * 0.1f; // やや中
                GameObject nacl = Instantiate(naClPrefab, spawnPos, Quaternion.identity);
                Debug.Log("🧂 NaClオブジェクトを生成しました");
                if (monitor != null) monitor.Log("🧂 ビーカー内にNaCl（塩）ができた！");
            }
            else
            {
                Debug.LogWarning("⚠️ NaClPrefabまたはbeakerTransformがセットされていません");
            }

            if (monitor != null) monitor.Log("✅ 実験が完了しました！");
            return;
        }

        StepType step = stepTypes[currentStep];
        GameObject target = (currentStep < stepTargets.Length) ? stepTargets[currentStep] : null;
        float duration = (currentStep < stepDurations.Length) ? stepDurations[currentStep] : 1.0f;

        if (target == null)
        {
            Debug.LogWarning($"⚠️ ステップ {currentStep}: 対象が null です。スキップします");
        }
        else
        {
            switch (step)
            {
                case StepType.EmissionChange:
                    if (currentStep < emissionColors.Length && reactionRenderer != null)
                    {
                        Material mat = reactionRenderer.material;
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", emissionColors[currentStep] * 2f);
                            Debug.Log($"✨ EmissionColor を変更: {emissionColors[currentStep] * 2f}");
                        }
                    }
                    break;
                case StepType.MoveElement:
                    if (currentStep < moveOffsets.Length && target != null)
                    {
                        Vector3 start = target.transform.position;
                        Vector3 end = start + moveOffsets[currentStep];
                        float moveTime = duration;
                        StartMove(target, start, end, moveTime);
                        Debug.Log($"📦 {target.name} をアニメーション移動: {moveOffsets[currentStep]}");
                        return; // アニメ終了時に次に進む
                    }
                    break;
                case StepType.ShaderEffect:
                    if (reactionRenderer != null && currentStep < shaderProperties.Length && currentStep < shaderValues.Length)
                    {
                        Material mat = reactionRenderer.material;
                        string prop = shaderProperties[currentStep];
                        float val = shaderValues[currentStep];

                        if (!string.IsNullOrWhiteSpace(prop))
                        {
                            if (mat.HasProperty(prop))
                            {
                                mat.SetFloat(prop, val);
                                Debug.Log($"🎨 Shader '{prop}' を {val} に設定");
                            }
                            else if (prop == "_Shininess" && mat.HasProperty("_Glossiness"))
                            {
                                mat.SetFloat("_Glossiness", val);
                                Debug.Log($"🔁 Shader '_Shininess' → '_Glossiness' に変換して {val} を設定");
                            }
                            else
                            {
                                Debug.LogWarning($"⚠️ Shader プロパティ '{prop}' が見つかりません");
                            }
                        }
                    }
                    break;
            }
        }
        currentStep++;
        SendCustomEventDelayedSeconds(nameof(PlayNextStep), duration);
    }

    // スムーズ移動用
    public void StartMove(GameObject target, Vector3 from, Vector3 to, float duration)
    {
        moveTarget = target;
        moveStart = from;
        moveEnd = to;
        moveDuration = duration;
        moveElapsed = 0f;
        isMoving = true;
        SendCustomEventDelayedFrames(nameof(UpdateMove), 1);
    }

    public void UpdateMove()
    {
        if (!isMoving || moveTarget == null)
        {
            isMoving = false;
            moveTarget = null;
            currentStep++;
            SendCustomEventDelayedSeconds(nameof(PlayNextStep), 0.01f);
            return;
        }

        moveElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(moveElapsed / moveDuration);
        moveTarget.transform.position = Vector3.Lerp(moveStart, moveEnd, t);

        if (t < 1f)
        {
            SendCustomEventDelayedFrames(nameof(UpdateMove), 1);
        }
        else
        {
            isMoving = false;
            moveTarget = null;
            currentStep++;
            SendCustomEventDelayedSeconds(nameof(PlayNextStep), 0.01f);
        }
    }
}
