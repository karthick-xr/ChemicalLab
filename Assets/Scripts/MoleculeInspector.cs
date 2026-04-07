using UnityEngine;
using TMPro;

public class MoleculeInspector : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject infoPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI formulaText;
    public TextMeshProUGUI bondInfoText;

    private Transform mainCameraTransform;

    void Start()
    {
        mainCameraTransform = Camera.main.transform;

        // Hide by default
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    void Update()
    {
        // If the panel is visible, make it look at the player
        if (infoPanel != null && infoPanel.activeSelf)
        {
            infoPanel.transform.LookAt(infoPanel.transform.position + mainCameraTransform.rotation * Vector3.forward,
                                      mainCameraTransform.rotation * Vector3.up);
        }
    }

    public void ShowInfo(MoleculeData data)
    {
        nameText.text = data.moleculeName;
        formulaText.text = data.formula;
        bondInfoText.text = data.bondType; // Add this field to your MoleculeData SO

        infoPanel.SetActive(true);
    }

    public void HideInfo()
    {
        infoPanel.SetActive(false);
    }
}