using UnityEngine;

public class ResetZone : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out MoleculeController molecule))
        {
            molecule.Decompose();
        }
    }
}
