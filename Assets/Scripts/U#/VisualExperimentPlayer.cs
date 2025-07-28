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
    // === 基本演出 ===
    public StepType[] stepTypes;
    public GameObject[] stepTargets;
    public float[] stepDurations;
    public Color[] emissionColors;
    public Vector3[] moveOffsets;
    public string[] shaderProperties;
    public float[] shaderValues;
    public Renderer reactionRenderer;
    public VRExperimentMonitor monitor;
    public SelectedObjectHolder holder;

    // === 複数器具対応 ===
    public string[] toolIDs;
    public Transform[] toolSpawnPoints;

    // === 反応パターン配列 ===
    public string[][] reaction_elementIDs;
    public string[][] reaction_toolIDs;
    public string[] reaction_conditionIDs;
    public GameObject[] reaction_productPrefabs;
    public int[] reaction_productCounts;
    public Vector3[] reaction_productOffsets;

    // === 内部状態 ===
    private int currentStep = 0;
    private bool isPlaying = false;
    private GameObject moveTarget;
    private Vector3 moveStart;
    private Vector3 moveEnd;
    private float moveDuration;
    private float moveElapsed;
    private bool isMoving = false;
    private GameObject[] usedElements;

    public void PlaySequence()
    {
        Debug.Log("🎬 PlaySequence呼び出し");
        if (stepTypes == null || stepTargets == null || stepTypes.Length == 0)
        {
            Debug.LogError("❌ stepTypes または stepTargets が未設定です");
            return;
        }
        if (holder == null)
        {
            Debug.LogError("❌ holder (SelectedObjectHolder) が未設定です");
            return;
        }
        if (isPlaying)
        {
            Debug.Log("⚠️ 演出がすでに実行中です");
            return;
        }
        usedElements = new GameObject[stepTargets.Length];
        for (int i = 0; i < stepTargets.Length; i++) usedElements[i] = stepTargets[i];

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

            // 元素オブジェクトをすべて消去
            if (usedElements != null)
            {
                for (int i = 0; i < usedElements.Length; i++)
                {
                    if (usedElements[i] != null)
                    {
                        Destroy(usedElements[i]);
                    }
                }
                Debug.Log("🧪 元素オブジェクトをすべて削除しました");
            }

            if (holder != null)
            {
                CreateProducts(holder.selectedElementIDs, holder.selectedToolIDs, holder.selectedConditionID);
            }

            if (monitor != null) monitor.Log("✅ 実験が完了しました！");
            return;
        }

        StepType step = stepTypes[currentStep];
        GameObject target = null;
        if (currentStep < stepTargets.Length)
        {
            target = stepTargets[currentStep];
        }
        float duration = 1.0f;
        if (currentStep < stepDurations.Length)
        {
            duration = stepDurations[currentStep];
        }

        if (target == null)
        {
            Debug.LogWarning($"⚠️ ステップ {currentStep}: 対象が null です。スキップします");
        }
        else
        {
            if (step == StepType.EmissionChange)
            {
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
            }
            else if (step == StepType.MoveElement)
            {
                if (currentStep < moveOffsets.Length && target != null)
                {
                    Vector3 start = target.transform.position;
                    Vector3 end = start + moveOffsets[currentStep];
                    float moveTime = duration;
                    StartMove(target, start, end, moveTime);
                    Debug.Log($"📦 {target.name} をアニメーション移動: {moveOffsets[currentStep]}");
                    return;
                }
            }
            else if (step == StepType.ShaderEffect)
            {
                if (reactionRenderer != null && currentStep < shaderProperties.Length && currentStep < shaderValues.Length)
                {
                    Material mat = reactionRenderer.material;
                    string prop = shaderProperties[currentStep];
                    float val = shaderValues[currentStep];

                    if (prop != null && prop != "")
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
            }
        }
        currentStep++;
        SendCustomEventDelayedSeconds(nameof(PlayNextStep), duration);
    }

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

    private Transform GetSpawnPointForTool(string toolID)
    {
        if (toolID == null) return null;
        for (int i = 0; i < toolIDs.Length; i++)
        {
            if (toolIDs[i] == toolID && toolSpawnPoints[i] != null)
            {
                return toolSpawnPoints[i];
            }
        }
        return null;
    }

    private void CreateProducts(string[] elements, string[] tools, string condition)
    {
        string toolID = null;
        if (tools != null && tools.Length > 0)
        {
            toolID = tools[0];
        }
        Transform spawnPoint = GetSpawnPointForTool(toolID);
        if (spawnPoint == null)
        {
            Debug.LogWarning("⚠️ ツールIDに対応するスポーン位置がありません");
            if (monitor != null) monitor.Log("⚠️ 反応生成位置が不明です");
            return;
        }

        // --- PC/VR判定 ---
        bool isVR = false;
#if UNITY_EDITOR
        isVR = false;
#else
        if (Networking.LocalPlayer != null)
        {
            isVR = Networking.LocalPlayer.IsUserInVR();
        }
#endif

        for (int i = 0; i < reaction_productPrefabs.Length; i++)
        {
            string[] patternElements = reaction_elementIDs[i];
            string[] patternTools = reaction_toolIDs[i];
            string patternCondition = reaction_conditionIDs[i];
            GameObject patternPrefab = reaction_productPrefabs[i];
            int patternCount = reaction_productCounts[i];
            Vector3 patternOffset = reaction_productOffsets[i];

            if (MatchPattern(elements, tools, condition, patternElements, patternTools, patternCondition))
            {
                for (int j = 0; j < patternCount; j++)
                {
                    if (patternPrefab != null)
                    {
                        Vector3 pos = spawnPoint.position + patternOffset + Vector3.right * 0.07f * (j - (patternCount - 1) * 0.5f);
                        GameObject obj = Instantiate(patternPrefab, pos, Quaternion.identity);

                        // --- PCモードではVRC_Pickupを外す（型がない場合は文字列） ---
                        if (!isVR)
                        {
                            Component pickup = obj.GetComponent("VRC_Pickup");
                            if (pickup != null)
                            {
                                Destroy(pickup);
                                Debug.Log("💻 PCモードなのでVRC_Pickupを外しました: " + obj.name);
                            }
                        }
                        else
                        {
                            Debug.Log("🕶️ VRモードなのでVRC_Pickupを残しました: " + obj.name);
                        }
                    }
                }
                if (monitor != null) monitor.Log("🧪 " + patternPrefab.name + "×" + patternCount + " が" + toolID + "でできた！");
                return;
            }
        }
        Debug.LogWarning("⚠️ 該当する反応パターンがありませんでした");
        if (monitor != null) monitor.Log("⚠️ 反応しませんでした");
    }

    private bool MatchPattern(string[] selE, string[] selT, string selC, string[] patE, string[] patT, string patC)
    {
        if (!MatchArray(selE, patE)) return false;
        if (!MatchArray(selT, patT)) return false;
        if (patC != null && patC != "" && patC != selC) return false;
        return true;
    }

    private bool MatchArray(string[] arr1, string[] arr2)
    {
        if (arr1 == null || arr2 == null) return false;
        if (arr1.Length != arr2.Length) return false;
        bool[] used = new bool[arr1.Length];
        for (int i = 0; i < arr2.Length; i++)
        {
            bool found = false;
            for (int j = 0; j < arr1.Length; j++)
            {
                if (!used[j] && arr2[i] == arr1[j])
                {
                    used[j] = true;
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        return true;
    }
}
