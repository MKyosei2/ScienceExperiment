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
    public SelectedObjectHolder holder;

    public string[][] reaction_elementIDs;
    public string[][] reaction_toolIDs;
    public string[] reaction_conditionIDs;
    public GameObject[] reaction_productPrefabs;
    public int[] reaction_productCounts;
    public Vector3[] reaction_productOffsets;
    public string[] reaction_descriptions;

    public string[] toolIDs;
    public Transform[] toolSpawnPoints;

    private int currentStep = 0;
    private bool isPlaying = false;
    private GameObject[] usedElements;
    private GameObject moveTarget;
    private Vector3 moveStart;
    private Vector3 moveEnd;
    private float moveDuration;
    private float moveElapsed;
    private bool isMoving = false;

    public void PlaySequence()
    {
        if (stepTypes == null || stepTargets == null || stepTypes.Length == 0) return;
        if (holder == null) return;
        if (isPlaying) return;

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
            isPlaying = false;
            if (usedElements != null)
                for (int i = 0; i < usedElements.Length; i++)
                    if (usedElements[i] != null) Destroy(usedElements[i]);

            if (holder != null)
                CreateProducts(holder.selectedElementIDs, holder.selectedToolIDs, holder.selectedConditionID);

            if (monitor != null) monitor.Log("✅ 実験が完了しました！");
            return;
        }

        StepType step = stepTypes[currentStep];
        GameObject target = (currentStep < stepTargets.Length) ? stepTargets[currentStep] : null;
        float duration = (currentStep < stepDurations.Length) ? stepDurations[currentStep] : 1.0f;

        if (target != null)
        {
            if (step == StepType.EmissionChange)
            {
                if (currentStep < emissionColors.Length && reactionRenderer != null)
                {
                    var mat = reactionRenderer.material;
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", emissionColors[currentStep] * 2f);
                    }
                }
            }
            else if (step == StepType.MoveElement)
            {
                if (currentStep < moveOffsets.Length)
                {
                    Vector3 start = target.transform.position;
                    Vector3 end = start + moveOffsets[currentStep];
                    float moveTime = duration;
                    StartMove(target, start, end, moveTime);
                    return;
                }
            }
            else if (step == StepType.ShaderEffect)
            {
                if (reactionRenderer != null && currentStep < shaderProperties.Length && currentStep < shaderValues.Length)
                {
                    var mat = reactionRenderer.material;
                    string prop = shaderProperties[currentStep];
                    float val = shaderValues[currentStep];
                    if (!string.IsNullOrEmpty(prop) && mat.HasProperty(prop))
                        mat.SetFloat(prop, val);
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

    // パターン照合&生成
    private void CreateProducts(string[] elements, string[] tools, string condition)
    {
        string toolID = (tools != null && tools.Length > 0) ? tools[0] : null;
        Transform spawnPoint = GetSpawnPointForTool(toolID);
        if (spawnPoint == null)
        {
            if (monitor != null) monitor.Log("⚠️ 反応生成位置が不明です");
            return;
        }

        // PC/VR判定
        bool isVR = false;
#if UNITY_EDITOR
        isVR = false;
#else
        if (Networking.LocalPlayer != null) isVR = Networking.LocalPlayer.IsUserInVR();
#endif

        for (int i = 0; i < reaction_productPrefabs.Length; i++)
        {
            if (MatchPattern(elements, tools, condition, reaction_elementIDs[i], reaction_toolIDs[i], reaction_conditionIDs[i]))
            {
                for (int j = 0; j < reaction_productCounts[i]; j++)
                {
                    if (reaction_productPrefabs[i] != null)
                    {
                        Vector3 pos = spawnPoint.position + reaction_productOffsets[i] + Vector3.right * 0.07f * (j - (reaction_productCounts[i] - 1) * 0.5f);
                        GameObject obj = Instantiate(reaction_productPrefabs[i], pos, Quaternion.identity);
                        if (!isVR)
                        {
                            var pickup = obj.GetComponent("VRC_Pickup");
                            if (pickup != null) Destroy(pickup);
                        }
                    }
                }
                if (monitor != null && reaction_descriptions != null && i < reaction_descriptions.Length)
                    monitor.Log("🧪 " + reaction_descriptions[i]);
                return;
            }
        }
        if (monitor != null) monitor.Log("⚠️ 反応しませんでした");
    }

    private Transform GetSpawnPointForTool(string toolID)
    {
        for (int i = 0; i < toolIDs.Length; i++)
            if (toolIDs[i] == toolID && toolSpawnPoints[i] != null) return toolSpawnPoints[i];
        return null;
    }
    private bool MatchPattern(string[] selE, string[] selT, string selC, string[] patE, string[] patT, string patC)
    {
        return MatchArray(selE, patE) && MatchArray(selT, patT) && (string.IsNullOrEmpty(patC) || patC == selC);
    }
    private bool MatchArray(string[] a, string[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        bool[] used = new bool[a.Length];
        for (int i = 0; i < b.Length; i++)
        {
            bool found = false;
            for (int j = 0; j < a.Length; j++)
            {
                if (!used[j] && a[j] == b[i])
                {
                    used[j] = true; found = true; break;
                }
            }
            if (!found) return false;
        }
        return true;
    }
}
