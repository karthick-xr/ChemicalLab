using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BondManager : MonoBehaviour
{
    [Header("Data References")]
    public MoleculeDatabase moleculeDatabase;
    //public UIManager uiManager;
    //public AudioManager audioManager;

    [Header("Tracking")]
    private List<AtomController> atomsInZone = new List<AtomController>();
    private List<MoleculeController> moleculesInZone = new List<MoleculeController>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out AtomController atom))
        {
            if (!atomsInZone.Contains(atom)) atomsInZone.Add(atom);
            CheckForCombination();
        }
        else if (other.TryGetComponent(out MoleculeController mol))
        {
            if (!moleculesInZone.Contains(mol)) moleculesInZone.Add(mol);
            CheckForCombination();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out AtomController atom))
            atomsInZone.Remove(atom);
        else if (other.TryGetComponent(out MoleculeController mol))
            moleculesInZone.Remove(mol);
    }

    private void CheckForCombination()
    {
        // 1. Calculate the TOTAL atom count from both loose atoms and molecules
        Dictionary<AtomType, int> totalCounts = new Dictionary<AtomType, int>();

        // Count loose atoms
        foreach (var atom in atomsInZone)
            AddAtomsToDict(totalCounts, atom.atomType, 1);

        // Count atoms inside existing molecules
        foreach (var mol in moleculesInZone)
        {
            foreach (var req in mol.moleculeData.requiredAtoms)
                AddAtomsToDict(totalCounts, req.type, req.count);
        }

        // 2. See if the total matches a recipe in the database
        MoleculeData match = moleculeDatabase.CheckMatch(totalCounts);

        if (match != null)
        {
            // SAFETY CHECK: If the zone ONLY contains one molecule and it's already the match, 
            // don't destroy/respawn it (prevents infinite loop).
            if (moleculesInZone.Count == 1 && atomsInZone.Count == 0 && moleculesInZone[0].moleculeData == match)
                return;

            CreateMolecule(match);
        }
    }

    private void CreateMolecule(MoleculeData data)
    {
        // Calculate average spawn position of all objects in the zone
        Vector3 spawnPos = Vector3.zero;
        int totalObjects = atomsInZone.Count + moleculesInZone.Count;

        foreach (var a in atomsInZone) spawnPos += a.transform.position;
        foreach (var m in moleculesInZone) spawnPos += m.transform.position;
        spawnPos /= totalObjects;

        // 3. Destroy all ingredients (Atoms & Old Molecules)
        foreach (var a in atomsInZone) Destroy(a.gameObject);
        foreach (var m in moleculesInZone) Destroy(m.gameObject);

        atomsInZone.Clear();
        moleculesInZone.Clear();

        // 4. Spawn new Molecule
        GameObject newMol = Instantiate(data.moleculePrefab, spawnPos, Quaternion.identity);

        // 5. Feedback
        //audioManager.PlaySuccessSound();
        //uiManager.OnMoleculeDiscovered(data);
    }

    private void AddAtomsToDict(Dictionary<AtomType, int> dict, AtomType type, int amount)
    {
        if (dict.ContainsKey(type)) dict[type] += amount;
        else dict[type] = amount;
    }
}