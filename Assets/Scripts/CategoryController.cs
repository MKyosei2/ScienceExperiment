using UnityEngine;

public class CategoryController : MonoBehaviour
{
    [System.Serializable]
    public class CategoryView
    {
        public string name;
        public GameObject[] showObjects;
        public GameObject[] hideObjects;
    }

    [SerializeField] private CategoryView[] categories;
    [SerializeField] private GenericSelector selector; // ”CˆÓ

    private int currentIndex = -1;

    public void SetCategoryByName(string name)
    {
        for (int i = 0; i < categories.Length; i++)
            if (categories[i].name == name) { Apply(i); return; }
        Debug.LogWarning($"[CategoryController] Category '{name}' not found.");
    }

    public void SetCategoryByIndex(int i) => Apply(i);

    private void Apply(int i)
    {
        if (i < 0 || i >= categories.Length) return;
        currentIndex = i;
        var c = categories[i];

        ToggleArray(c.showObjects, true);
        ToggleArray(c.hideObjects, false);

        if (selector && System.Enum.TryParse(typeof(GenericSelector.Category), c.name, out var e))
            selector.SetCategory((GenericSelector.Category)e);
    }

    private void ToggleArray(GameObject[] arr, bool state)
    {
        if (arr == null) return;
        foreach (var go in arr) if (go) go.SetActive(state);
    }
}
