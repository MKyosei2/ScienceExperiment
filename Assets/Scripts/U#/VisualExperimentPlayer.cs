using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class VisualExperimentPlayer : UdonSharpBehaviour
{
    public StepType[] stepTypes;
    public GameObject[] stepTargets;
    public float[] stepDurations;

    public Vector3[] moveOffsets;
    public Color[] emissionColors;
    public string[] shaderProperties;
    public float[] shaderValues;
    public GameObject[] resultPrefabs;
    public AudioClip[] stepSounds;

    private int currentStep = 0;
    private float timer = 0f;
    private bool isRunning = false;

    public void PlaySequence()
    {
        currentStep = 0;
        timer = 0f;
        isRunning = true;
    }

    void Update()
    {
        if (!isRunning || currentStep >= stepTypes.Length) return;

        timer += Time.deltaTime;

        if (timer >= stepDurations[currentStep])
        {
            ExecuteStep(currentStep);
            currentStep++;
            timer = 0f;

            if (currentStep >= stepTypes.Length)
            {
                isRunning = false;
            }
        }
    }

    void ExecuteStep(int index)
    {
        GameObject target = stepTargets[index];
        if (target == null) return;

        switch (stepTypes[index])
        {
            case StepType.MoveElement:
                target.transform.position += moveOffsets[index];
                break;

            case StepType.EmissionChange:
                Renderer renderer = target.GetComponent<Renderer>();
                if (renderer != null && renderer.material.HasProperty("_EmissionColor"))
                {
                    renderer.material.SetColor("_EmissionColor", emissionColors[index]);
                }
                break;

            case StepType.ShaderEffect:
                Renderer rend = target.GetComponent<Renderer>();
                if (rend != null && rend.material.HasProperty(shaderProperties[index]))
                {
                    rend.material.SetFloat(shaderProperties[index], shaderValues[index]);
                }
                break;

            case StepType.SpawnResult:
                if (resultPrefabs[index] != null)
                {
                    GameObject result = VRCInstantiate(resultPrefabs[index]);
                    result.transform.position = target.transform.position + Vector3.up * 0.2f;
                }
                break;

            case StepType.PlaySound:
                if (stepSounds[index] != null)
                {
                    AudioSource.PlayClipAtPoint(stepSounds[index], target.transform.position);
                }
                break;
        }
    }
}

public enum StepType
{
    MoveElement,
    EmissionChange,
    ShaderEffect,
    SpawnResult,
    PlaySound
}
