using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class MonitorControlUI : UdonSharpBehaviour
{
    public ObserverRoomAuthorityManager authorityManager;
    public GameObject controlPanel;

    void Start()
    {
        controlPanel.SetActive(authorityManager.IsLocalPlayerOwner());
    }

    public void RefreshUI()
    {
        controlPanel.SetActive(authorityManager.IsLocalPlayerOwner());
    }
}
