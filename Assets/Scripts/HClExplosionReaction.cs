using UdonSharp;
using UnityEngine;

public class HClExplosionReaction : UdonSharpBehaviour
{
    public GameObject flaskH;
    public GameObject flaskCl;

    public Light explosionLight;
    public float flashSeconds = 0.15f;

    private bool hasH;
    private bool hasCl;
    private bool reacted;

    private void OnTriggerEnter(Collider other)
    {
        if (reacted || other == null) return;

        var go = other.gameObject;

        if (flaskH != null && (go == flaskH || other.transform.IsChildOf(flaskH.transform)))
            hasH = true;

        if (flaskCl != null && (go == flaskCl || other.transform.IsChildOf(flaskCl.transform)))
            hasCl = true;

        if (hasH && hasCl)
        {
            React();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (reacted || other == null) return;

        var go = other.gameObject;

        if (flaskH != null && (go == flaskH || other.transform.IsChildOf(flaskH.transform)))
            hasH = false;

        if (flaskCl != null && (go == flaskCl || other.transform.IsChildOf(flaskCl.transform)))
            hasCl = false;
    }

    private void React()
    {
        reacted = true;

        if (explosionLight != null)
        {
            explosionLight.enabled = true;
            SendCustomEventDelayedSeconds(nameof(EndFlash), flashSeconds);
        }
    }

    public void EndFlash()
    {
        if (explosionLight != null) explosionLight.enabled = false;
    }
}
