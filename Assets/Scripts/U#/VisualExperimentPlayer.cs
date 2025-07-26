using UdonSharp;
using UnityEngine;
using VRC.Udon;

public enum StepType
{
    EmissionChange,
    MoveElement,
    ShaderEffect,
    CustomEvent
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

    private int currentStep = 0;
    private bool isPlaying = false;

    public void PlaySequence()
    {
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
            return;
        }

        StepType step = stepTypes[currentStep];
        GameObject target = (currentStep < stepTargets.Length) ? stepTargets[currentStep] : null;
        float duration = (currentStep < stepDurations.Length) ? stepDurations[currentStep] : 1.0f;

        if (target == null && step != StepType.CustomEvent)
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
                        Vector3 offset = moveOffsets[currentStep];
                        target.transform.position += offset;
                        Debug.Log($"📦 {target.name} を移動: {offset}");
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

                case StepType.CustomEvent:
                    if (target != null)
                    {
                        UdonBehaviour udon = target.GetComponent<UdonBehaviour>();
                        if (udon != null)
                        {
                            udon.SendCustomEvent("Spawn");
                            Debug.Log($"📢 CustomEvent 'Spawn' を {target.name} に送信しました");
                        }
                        else
                        {
                            Debug.LogWarning($"⚠️ {target.name} に UdonBehaviour が見つかりません");
                        }
                    }
                    break;
            }
        }

        currentStep++;
        SendCustomEventDelayedSeconds(nameof(PlayNextStep), duration);
    }
}
