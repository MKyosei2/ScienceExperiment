using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

public class AICompoundNarrator : UdonSharpBehaviour
{
    public Text subtitleText;
    public float subtitleDuration = 8f;

    public AudioSource audioSource;
    public AudioClip[] preGeneratedTTSClips;
    public string[] clipKeys;

    private float subtitleTimer = 0f;
    private bool isSubtitleActive = false;

    void Update()
    {
        if (isSubtitleActive)
        {
            subtitleTimer -= Time.deltaTime;
            if (subtitleTimer <= 0f)
            {
                subtitleText.text = "";
                isSubtitleActive = false;
            }
        }
    }

    public void PlayNarration(string compoundKey, string funFact)
    {
        if (subtitleText != null)
        {
            subtitleText.text = funFact;
            subtitleTimer = subtitleDuration;
            isSubtitleActive = true;
        }

        AudioClip clip;
        if (TryFindClip(compoundKey, out clip))
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    private bool TryFindClip(string key, out AudioClip clip)
    {
        for (int i = 0; i < clipKeys.Length && i < preGeneratedTTSClips.Length; i++)
        {
            if (clipKeys[i] == key)
            {
                clip = preGeneratedTTSClips[i];
                return true;
            }
        }
        clip = null;
        return false;
    }
}