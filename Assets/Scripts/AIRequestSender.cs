using UnityEngine;
using System;
using UnityEngine.Networking;

public class AIRequestSender : MonoBehaviour
{
    [Header("Mode")]
    [SerializeField] private bool useMock = true;

    [Header("Endpoints (real)")]
    [SerializeField] private string endpointUrl;
    [SerializeField] private float timeoutSeconds = 5f;

    [Header("Mock")]
    [TextArea][SerializeField] private string mockResponse = "{\"effects\":[{\"type\":\"bubble\",\"color\":\"blue\"}]}";

    public void SetUseMock(bool v) => useMock = v;

    public void Send(SelectedObjectHolder selected, Action<string> onSuccess, Action<string> onError)
    {
        if (useMock)
        {
            onSuccess?.Invoke(mockResponse);
            return;
        }
        StartCoroutine(CoSend(selected, onSuccess, onError));
    }

    private System.Collections.IEnumerator CoSend(SelectedObjectHolder selected, Action<string> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(endpointUrl))
        {
            onError?.Invoke("Endpoint not set.");
            yield break;
        }

        string payload = selected != null ? selected.ToJsonPayload() : "{}";

        using (var req = new UnityWebRequest(endpointUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            req.SendWebRequest();
            float t = 0f;
            while (!req.isDone && t < timeoutSeconds)
            {
                t += Time.deltaTime;
                yield return null;
            }

#if UNITY_2020_2_OR_NEWER
            bool hasErr = req.result == UnityWebRequest.Result.ConnectionError ||
                          req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool hasErr = req.isNetworkError || req.isHttpError;
#endif
            if (t >= timeoutSeconds) { onError?.Invoke("Timeout"); yield break; }
            if (hasErr) { onError?.Invoke(req.error); yield break; }

            onSuccess?.Invoke(req.downloadHandler.text);
        }
    }
}
