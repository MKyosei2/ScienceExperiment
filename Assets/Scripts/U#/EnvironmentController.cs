using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public enum GravityMode { Normal, ZeroG }
public enum Hemisphere { Equator, North, South }

public class EnvironmentController : UdonSharpBehaviour
{
    public GravityMode currentGravity = GravityMode.Normal;
    public Hemisphere currentHemisphere = Hemisphere.Equator;

    public Rigidbody[] floatingObjects;
    public AudioSource environmentAudio;
    public GameObject equatorOverlay;

    public void SetGravityMode(int mode)
    {
        currentGravity = (GravityMode)mode;

        for (int i = 0; i < floatingObjects.Length; i++)
        {
            if (floatingObjects[i] != null)
            {
                floatingObjects[i].useGravity = (mode == 0);
            }
        }

        if (environmentAudio != null)
        {
            environmentAudio.pitch = (mode == 1) ? 0.6f : 1.0f;
        }
    }

    public void SetHemisphere(int mode)
    {
        currentHemisphere = (Hemisphere)mode;

        if (equatorOverlay != null)
        {
            equatorOverlay.SetActive(mode == 0);
        }
    }

    public string GetConditionString()
    {
        return currentGravity.ToString() + "," + currentHemisphere.ToString();
    }
}