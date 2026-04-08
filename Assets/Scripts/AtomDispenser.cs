using UnityEngine;

public class AtomDispenser : MonoBehaviour
{
    [Header("Settings")]
    public AtomRegistry atomRegistry; // Use your Global Registry from before
    public Transform spawnLocation;   // Where the atom appears
    public float checkRadius = 0.05f; // Radius to detect if an atom is still there
    public LayerMask atomLayer;       // Ensure your Atoms are on a specific layer

    public void RequestAtom(int atomTypeIndex)
    {
        // Convert the int from the UI Button to our Enum
        AtomType type = (AtomType)atomTypeIndex;

        if (IsSpawnPointClear())
        {
            GameObject prefab = atomRegistry.GetPrefab(type);
            if (prefab != null)
            {
                GameObject g =Instantiate(prefab, spawnLocation.position, spawnLocation.rotation);
                // Play a "click" or "spawn" sound here
                AudioManager.Instance.PlayAtomSpawn(g.transform.position);
            }
        }
        else
        {
            Debug.Log("Spawn point is blocked! Take the current atom first.");
        }
    }

    private bool IsSpawnPointClear()
    {
        // Use an OverlapSphere to see if any atom is currently at the spawnLocation
        Collider[] hitColliders = Physics.OverlapSphere(spawnLocation.position, checkRadius);

        foreach (var col in hitColliders)
        {
            // If any object has an AtomController or MoleculeController, the point is blocked
            if (col.GetComponent<AtomController>() != null || col.GetComponent<MoleculeController>() != null)
            {
                return false;
            }
        }

        return true;
    }

    // Draw a gizmo in the editor so you can see the detection radius
    private void OnDrawGizmosSelected()
    {
        if (spawnLocation != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(spawnLocation.position, checkRadius);
        }
    }
}