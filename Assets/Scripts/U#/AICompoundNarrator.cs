using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using System.Collections;

public class AICompoundNarrator : UdonSharpBehaviour
{
    public Text subtitleText;
    public float subtitleDuration = 8f;

    public AudioSource audioSource;
    public AudioClip[] preGeneratedTTSClips;
    public string[] clipKeys;

    private Coroutine subtitleCoroutine;

    public void PlayNarration(string compoundKey, string funFact)
    {
        if (subtitleCoroutine != null) StopCoroutine(subtitleCoroutine);
        subtitleCoroutine = StartCoroutine(ShowSubtitle(funFact));

        if (TryFindClip(compoundKey, out AudioClip clip))
        {
            audioSource?.PlayOneShot(clip);
        }
    }

    private IEnumerator ShowSubtitle(string text)
    {
        subtitleText.text = text;
        yield return new WaitForSeconds(subtitleDuration);
        subtitleText.text = "";
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
