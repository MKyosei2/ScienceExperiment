using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ExperimentController : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;
    public AIRequestSender requestSender;
    public Transform[] objectsToAnimate;
    public Renderer reactionRenderer;

    public Vector3 animationOffset = new Vector3(0, 0.15f, 0);
    public float animationDuration = 1.0f;

    private bool hasRun = false;
    private float requestTime = 0f;
    private const float timeout = 5f;

    void Update()
    {
        if (hasRun && Time.time - requestTime >= timeout)
        {
            hasRun = false;
            RunFallback();
        }
    }

    public void RunExperiment()
    {
        if (holder == null || requestSender == null)
        {
            Debug.LogWarning("⚠️ holder または requestSender が未設定です");
            return;
        }

        string elementID = holder.selectedElementID;
        string toolID = holder.selectedToolID;
        string condition = holder.selectedConditionID;

        if (string.IsNullOrWhiteSpace(elementID) || string.IsNullOrWhiteSpace(toolID) || string.IsNullOrWhiteSpace(condition))
        {
            Debug.LogWarning("⚠️ 実験に必要な選択が未完了");
            return;
        }

        Debug.Log("🧪 実験開始: " + elementID + " x " + toolID + " x " + condition);

        hasRun = true;
        requestTime = Time.time;

        requestSender.SendToAI(elementID, toolID, condition);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!Networking.LocalPlayer.IsUserInVR()) return;
        if (!hasRun) RunExperiment();
    }

    public void OnAIResponseReceived()
    {
        hasRun = false;
        ApplyVisualEffect();
    }

    public void RunFallback()
    {
        Debug.Log("⚠️ 通信応答なし → ローカル演出を適用");
        ApplyVisualEffect();
    }

    public void ApplyVisualEffect()
    {
        if (reactionRenderer != null && reactionRenderer.material != null)
        {
            Material mat = reactionRenderer.material;
            mat.SetFloat("_BubbleSpeed", 2.5f);
            mat.SetFloat("_WobbleAmount", 0.12f);
            mat.SetFloat("_HeatDistortion", 0.15f);
            mat.SetColor("_MainColor", new Color(0.2f, 0.6f, 1f, 1f));
        }

        for (int i = 0; i < objectsToAnimate.Length; i++)
        {
            Transform t = objectsToAnimate[i];
            if (t != null)
            {
                Vector3 targetPos = t.position + animationOffset;
                t.position = targetPos;
                SendCustomEventDelayedSeconds(nameof(ResetPosition), animationDuration);
            }
        }
    }

    public void ResetPosition()
    {
        for (int i = 0; i < objectsToAnimate.Length; i++)
        {
            Transform t = objectsToAnimate[i];
            if (t != null)
            {
                t.position -= animationOffset;
            }
        }
    }
}
