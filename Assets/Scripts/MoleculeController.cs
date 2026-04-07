using UnityEngine;

public class MoleculeController : MonoBehaviour
{
    public MoleculeData moleculeData;
    public AtomRegistry atomRegistry;

    // Call this via a VR Button, a 'Reset Zone', or a Controller Input
    public void Decompose()
    {
        if (moleculeData == null || atomRegistry == null) return;

        foreach (var atomReq in moleculeData.requiredAtoms)
        {
            for (int i = 0; i < atomReq.count; i++)
            {
                SpawnAtom(atomReq.type);
            }
        }

        // Play a "break" sound or particle effect here
        Destroy(gameObject);
    }

    private void SpawnAtom(AtomType type)
    {
        GameObject prefabToSpawn = atomRegistry.GetPrefab(type);
        if (prefabToSpawn != null)
        {
            // Spawn with a slight random offset so they don't overlap perfectly
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * 0.1f;
            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        }
    }
}