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
        // 1. EXIT if there are no loose atoms. 
        // This stops the infinite loop because a finished molecule isn't "loose".
        if (atomsInZone.Count == 0) return;

        // 2. Calculate the pool of LOOSE atoms only for the first check
        Dictionary<AtomType, int> loosePool = new Dictionary<AtomType, int>();
        foreach (var a in atomsInZone) AddAtomsToDict(loosePool, a.atomType, 1);

        // 3. Sort DB by complexity (CH4 > H2)
        var sortedRecipes = moleculeDatabase.allMolecules
            .OrderByDescending(m => m.requiredAtoms.Sum(a => a.count)).ToList();

        foreach (var recipe in sortedRecipes)
        {
            // Try to form the recipe using ONLY loose atoms first
            if (CanForm(recipe, loosePool))
            {
                CreateMolecule(recipe, true); // true = use loose atoms
                return;
            }
        }

        // 4. OPTIONAL: Chain Reactions (e.g., H2 + O)
        // Only if no loose-only match was found, check if loose atoms + existing molecules match something BIGGER.
        CheckForUpgrades(loosePool, sortedRecipes);
    }

    private void CheckForUpgrades(Dictionary<AtomType, int> loosePool, List<MoleculeData> sortedRecipes)
    {
        foreach (var recipe in sortedRecipes)
        {
            // 1. Find all molecules in the zone that could POTENTIALLY be part of this recipe
            // For H2O, this would find H2. It would NOT find NH3 because NH3 has Nitrogen.
            var potentialCandidates = moleculesInZone
                .Where(m => DoesRecipeContainMolecule(recipe, m.moleculeData))
                .OrderByDescending(m => m.moleculeData.requiredAtoms.Sum(a => a.count)) // Try upgrading largest possible first
                .ToList();

            foreach (var candidate in potentialCandidates)
            {
                // 2. Check if the Loose Atoms + this SPECIFIC candidate = the Recipe
                if (CanFormWithSpecificMolecule(recipe, candidate, loosePool))
                {
                    // We found a perfect match! Upgrade ONLY this candidate.
                    CreateUpgradeMolecule(recipe, candidate);
                    return;
                }
            }
        }
    }

    private void CreateUpgradeMolecule(MoleculeData recipe, MoleculeController moleculeToReplace)
    {
        Vector3 spawnPos = moleculeToReplace.transform.position;

        // 1. Remove the specific molecule from our tracking list and destroy it
        moleculesInZone.Remove(moleculeToReplace);
        Destroy(moleculeToReplace.gameObject);

        // 2. Consume only the loose atoms needed to finish the recipe
        // (Since the molecule already provided some atoms, we only need the rest)
        ConsumeLooseAtomsForUpgrade(recipe, moleculeToReplace.moleculeData);

        // 3. Spawn the new Molecule
        Instantiate(recipe.moleculePrefab, spawnPos + (Vector3.up * 0.1f), Quaternion.identity);

        // Feedback
        // audioManager.PlaySuccessSound();
        Debug.Log($"Upgraded to: {recipe.moleculeName}");
    }

    private void ConsumeLooseAtomsForUpgrade(MoleculeData recipe, MoleculeData oldData)
    {
        foreach (var req in recipe.requiredAtoms)
        {
            // Calculate how many MORE we need of this atom type
            int alreadyHave = oldData.requiredAtoms.FirstOrDefault(r => r.type == req.type).count;
            int stillNeed = req.count - alreadyHave;

            if (stillNeed <= 0) continue;

            var matchingLooseAtoms = atomsInZone.Where(a => a.atomType == req.type).Take(stillNeed).ToList();
            foreach (var a in matchingLooseAtoms)
            {
                atomsInZone.Remove(a);
                Destroy(a.gameObject);
            }
        }
    }

    private bool CanFormWithSpecificMolecule(MoleculeData targetRecipe, MoleculeController existingMol, Dictionary<AtomType, int> loosePool)
    {
        // 1. Start with the atoms inside the existing molecule
        Dictionary<AtomType, int> combinedPool = new Dictionary<AtomType, int>();
        foreach (var req in existingMol.moleculeData.requiredAtoms)
        {
            AddAtomsToDict(combinedPool, req.type, req.count);
        }

        // 2. Add the loose atoms available in the zone
        foreach (var entry in loosePool)
        {
            AddAtomsToDict(combinedPool, entry.Key, entry.Value);
        }

        // 3. Check if this combined pool satisfies the NEW recipe
        foreach (var req in targetRecipe.requiredAtoms)
        {
            if (!combinedPool.ContainsKey(req.type) || combinedPool[req.type] < req.count)
            {
                return false;
            }
        }

        return true;
    }

    private bool DoesRecipeContainMolecule(MoleculeData newRecipe, MoleculeData oldMolecule)
    {
        foreach (var oldReq in oldMolecule.requiredAtoms)
        {
            // Check if the new recipe even uses this atom type
            var newReq = newRecipe.requiredAtoms.FirstOrDefault(r => r.type == oldReq.type);

            // If the old molecule has Nitrogen (NH3) but the new recipe (H2O) doesn't,
            // newReq.count will be 0. This returns false and saves the Ammonia!
            if (newReq.count < oldReq.count)
                return false;
        }
        return true;
    }

    private bool CanForm(MoleculeData recipe, Dictionary<AtomType, int> pool)
    {
        foreach (var req in recipe.requiredAtoms)
        {
            if (!pool.ContainsKey(req.type) || pool[req.type] < req.count)
                return false;
        }
        return true;
    }

    private void CreateMolecule(MoleculeData data, bool looseOnly)
    {
        // Calculate spawn position before destroying
        Vector3 spawnPos = transform.position + Vector3.up * 0.1f;

        // Consume specifically what we need
        ConsumeIngredients(data);

        // Spawn the new Molecule
        GameObject newMol = Instantiate(data.moleculePrefab, spawnPos, Quaternion.identity);

        // Adding a tiny random offset prevents molecules from stacking perfectly
        newMol.transform.position += new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));

        Debug.Log($"Successfully Formed: {data.moleculeName}");

        // DO NOT call CheckForCombination() here.
    }

    private void ConsumeIngredients(MoleculeData recipe)
    {
        foreach (var req in recipe.requiredAtoms)
        {
            int needed = req.count;

            // 1. ALWAYS try to take from loose atoms in the zone first
            var matchAtoms = atomsInZone.Where(a => a.atomType == req.type).ToList();
            foreach (var a in matchAtoms)
            {
                if (needed <= 0) break;
                atomsInZone.Remove(a);
                Destroy(a.gameObject);
                needed--;
            }

            // 2. ONLY take from existing molecules if loose atoms weren't enough
            if (needed > 0)
            {
                var matchMols = moleculesInZone.Where(m => m.moleculeData.requiredAtoms.Any(ra => ra.type == req.type)).ToList();
                foreach (var m in matchMols)
                {
                    if (needed <= 0) break;
                    moleculesInZone.Remove(m);
                    Destroy(m.gameObject);
                    needed -= m.moleculeData.requiredAtoms.First(ra => ra.type == req.type).count;
                }
            }
        }
    }

    private void AddAtomsToDict(Dictionary<AtomType, int> dict, AtomType type, int amount)
    {
        if (dict.ContainsKey(type)) dict[type] += amount;
        else dict[type] = amount;
    }
}