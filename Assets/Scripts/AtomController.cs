using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine;

public class AtomController : MonoBehaviour
{
    public AtomType atomType;
    private XRGrabInteractable grabInteractable;

    void Awake() => grabInteractable = GetComponent<XRGrabInteractable>();

    // Called via BondManager when combined
    public void DisableInteraction()
    {
        grabInteractable.enabled = false;
        GetComponent<Collider>().enabled = false;
    }
}