using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SelectionZone : UdonSharpBehaviour
{
    [Tooltip("最大格納数（Zone内オブジェクト）")]
    public int maxCount = 8;

    [HideInInspector]
    public GameObject[] objectsInZone;

    private int count = 0;

    void Start()
    {
        objectsInZone = new GameObject[maxCount];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (count >= maxCount) return;

        GameObject go = other.gameObject;
        if (!Contains(go))
        {
            objectsInZone[count] = go;
            count++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject go = other.gameObject;
        int index = IndexOf(go);
        if (index != -1)
        {
            for (int i = index; i < count - 1; i++)
                objectsInZone[i] = objectsInZone[i + 1];
            objectsInZone[count - 1] = null;
            count--;
        }
    }

    public GameObject GetFirstObject()
    {
        return count > 0 ? objectsInZone[0] : null;
    }

    private bool Contains(GameObject target)
    {
        for (int i = 0; i < count; i++) if (objectsInZone[i] == target) return true;
        return false;
    }

    private int IndexOf(GameObject target)
    {
        for (int i = 0; i < count; i++) if (objectsInZone[i] == target) return i;
        return -1;
    }
}
