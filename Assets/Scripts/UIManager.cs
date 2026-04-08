using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public MoleculeDatabase database;
    public GameObject entryPrefab;
    public Transform container;

    // Keep track of which UI text belongs to which molecule
    private Dictionary<MoleculeData, TextMeshProUGUI> uiMap = new Dictionary<MoleculeData, TextMeshProUGUI>();
    private Dictionary<MoleculeData, Image> imageMap = new Dictionary<MoleculeData, Image>();

    void Start()
    {
        InitializeLibrary();
    }

    private void InitializeLibrary()
    {
        // Clear existing UI items
        foreach (Transform child in container) Destroy(child.gameObject);

        // Create a slot for every molecule in your database
        foreach (var molecule in database.allMolecules)
        {
            GameObject newEntry = Instantiate(entryPrefab, container);
            TextMeshProUGUI label = newEntry.GetComponentInChildren<TextMeshProUGUI>();
            Image icon = newEntry.transform.Find("Icon").GetComponent<Image>();

            // Set icon to a "locked" silhouette or just transparent initially
            icon.sprite = molecule.moleculeIcon;
            icon.color = Color.black; // Shows a black silhouette initially

            label.text = "????"; // Initially hidden

            uiMap.Add(molecule, label);
            imageMap.Add(molecule, icon);
        }
    }

    public void OnMoleculeDiscovered(MoleculeData discoveredData)
    {
        if (uiMap.ContainsKey(discoveredData))
        {
            // Update the text to the actual name and formula
            uiMap[discoveredData].text = $"{discoveredData.moleculeName} ({discoveredData.formula})";
            uiMap[discoveredData].color = Color.green; // Visual feedback for discovery

            // Reveal Image
            imageMap[discoveredData].color = Color.white; // Restore full color

            // Optional: Play a "New Discovery" sound effect
            Debug.Log($"UI Updated: {discoveredData.moleculeName} is now visible!");
        }
    }
}