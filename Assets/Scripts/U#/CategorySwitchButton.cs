using UdonSharp;
using UnityEngine;

public class CategorySwitchButton : UdonSharpBehaviour
{
    public ObjectSpawnerButton spawner;
    public string categoryToSet;

    public override void Interact()
    {
        spawner.SetCategory(categoryToSet);
    }
}