using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class VisualExperimentPlayer : UdonSharpBehaviour
{
    public GameObject[] elementObjects;
    public string[] elementIDs;

    public GameObject[] toolObjects;
    public string[] toolIDs;

    public string[] patternElementIDs;
    public string[] patternToolIDs;
    public string[] patternConditionIDs;
    public Color[] patternEmissionColors;
    public GameObject[] patternResultPrefabs;

    public Transform spawnPoint;

    private float timer = 0f;
    private int step = 0;
    private GameObject movingElement;
    private Vector3 moveStart;
    private Vector3 moveEnd;
    private Renderer targetRenderer;
    private GameObject resultPrefab;
    private Color emissionColor;

    private bool isRunning = false;

    // ✅ 実験演出を再生
    public void PlaySequence(string[] elements, string[] tools, string condition)
    {
        if (isRunning || elements.Length == 0 || tools.Length == 0) return;

        string e = elements[0];
        string t = tools[0];

        movingElement = null;
        targetRenderer = null;
        resultPrefab = null;
        emissionColor = Color.red;

        for (int i = 0; i < elementIDs.Length; i++)
        {
            if (elementIDs[i] == e)
            {
                movingElement = elementObjects[i];
                break;
            }
        }

        for (int i = 0; i < toolIDs.Length; i++)
        {
            if (toolIDs[i] == t)
            {
                Transform toolT = toolObjects[i].transform;
                moveEnd = toolT.position + Vector3.up * 0.1f;
                targetRenderer = toolObjects[i].GetComponent<Renderer>();
                break;
            }
        }

        for (int i = 0; i < patternElementIDs.Length; i++)
        {
            if (patternElementIDs[i] == e && patternToolIDs[i] == t && patternConditionIDs[i] == condition)
            {
                resultPrefab = patternResultPrefabs[i];
                emissionColor = patternEmissionColors[i];
                break;
            }
        }

        if (movingElement != null)
        {
            moveStart = movingElement.transform.position;
        }

        timer = 0f;
        step = 0;
        isRunning = true;
    }

    void Update()
    {
        if (!isRunning) return;

        timer += Time.deltaTime;

        if (step == 0 && movingElement != null)
        {
            float t = Mathf.Clamp01(timer / 1f);
            movingElement.transform.position = Vector3.Lerp(moveStart, moveEnd, t);
            if (t >= 1f)
            {
                timer = 0f;
                step++;
            }
        }
        else if (step == 1 && timer >= 1f)
        {
            if (targetRenderer != null && targetRenderer.material.HasProperty("_EmissionColor"))
            {
                targetRenderer.material.SetColor("_EmissionColor", emissionColor);
            }

            timer = 0f;
            step++;
        }
        else if (step == 2 && timer >= 1f)
        {
            if (resultPrefab != null && spawnPoint != null)
            {
                GameObject result = VRCInstantiate(resultPrefab);
                result.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }

            isRunning = false;
        }
    }

    // ✅ ZoneSpawnButtonから呼び出す登録関数
    public void RegisterElement(string id, GameObject obj)
    {
        for (int i = 0; i < elementIDs.Length; i++)
        {
            if (elementIDs[i] == id)
            {
                elementObjects[i] = obj;
                return;
            }
        }
    }

    public void RegisterTool(string id, GameObject obj)
    {
        for (int i = 0; i < toolIDs.Length; i++)
        {
            if (toolIDs[i] == id)
            {
                toolObjects[i] = obj;
                return;
            }
        }
    }
}
