using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class ObserverRoomControlPanelUI : UdonSharpBehaviour
{
    public ObserverRoomauthorityManager authorityManager;

    public Slider observerLimitSlider;
    public Text observerLimitText;
    public Toggle roomLockToggle;
    public Dropdown experimentTargetDropdown;

    public MonitorSwitchRouter monitorSwitcher;

    void Start()
    {
        UpdateUIInteractable();
    }

    public void UpdateUIInteractable()
    {
        bool isOwner = authorityManager != null && authorityManager.IsLocalPlayerOwner();
        observerLimitSlider.interactable = isOwner;
        roomLockToggle.interactable = isOwner;
        experimentTargetDropdown.interactable = isOwner;
    }

    public void OnLimitSliderChanged()
    {
        if (!authorityManager.IsLocalPlayerOwner()) return;

        authorityManager.maxObservers = Mathf.RoundToInt(observerLimitSlider.value);
        if (observerLimitText != null)
            observerLimitText.text = $"最大人数: {authorityManager.maxObservers}";
    }

    public void OnLockToggleChanged()
    {
        if (!authorityManager.IsLocalPlayerOwner()) return;
        authorityManager.isRoomLocked = roomLockToggle.isOn;
    }

    public void OnExperimentTargetChanged()
    {
        if (!authorityManager.IsLocalPlayerOwner()) return;

        string selectedRoom = experimentTargetDropdown.options[experimentTargetDropdown.value].text;
        if (monitorSwitcher != null)
        {
            monitorSwitcher.SwitchMonitorToRoom(selectedRoom);
        }
    }
}
