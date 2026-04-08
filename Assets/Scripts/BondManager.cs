using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BondManager : MonoBehaviour
{
    [Header("Data References")]
    public MoleculeDatabase moleculeDatabase;
    public UIManager uiManager;

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
        // 1. Calculate the TOTAL pool of everything in the zone
        Dictionary<AtomType, int> totalPool = new Dictionary<AtomType, int>(loosePool);
        foreach (var mol in moleculesInZone)
        {
            foreach (var req in mol.moleculeData.requiredAtoms)
                AddAtomsToDict(totalPool, req.type, req.count);
        }

        foreach (var recipe in sortedRecipes)
        {
            // If we can make it from loose atoms, skip the "Upgrade" checks and just make it
            Dictionary<AtomType, int> loosePoolOnly = new Dictionary<AtomType, int>();
            foreach (var a in atomsInZone) AddAtomsToDict(loosePoolOnly, a.atomType, 1);

            if (CanForm(recipe, loosePoolOnly))
            {
                CreateMolecule(recipe, true);
                return;
            }

            // Otherwise, proceed with the "Total Pool" upgrade logic...
            if (CanForm(recipe, totalPool))
            {
                if (IsIllegalDowngrade(recipe) || IsAlreadyPresent(recipe)) continue;
                CreateMultiComponentMolecule(recipe);
                return;
            }
        }
    }
    private bool IsIllegalDowngrade(MoleculeData newRecipe)
    {
        int newSize = newRecipe.requiredAtoms.Sum(a => a.count);
        foreach (var mol in moleculesInZone)
        {
            if (mol.moleculeData.requiredAtoms.Sum(a => a.count) > newSize)
                return true;
        }
        return false;
    }
    private bool IsAlreadyPresent(MoleculeData recipe)
    {
        // 1. How many of this molecule do we ALREADY have?
        int existingCount = moleculesInZone.Count(m => m.moleculeData == recipe);

        // 2. How many of this molecule COULD we make using ONLY the loose atoms?
        // (We use a temporary pool to check this)
        Dictionary<AtomType, int> loosePool = new Dictionary<AtomType, int>();
        foreach (var a in atomsInZone) AddAtomsToDict(loosePool, a.atomType, 1);

        bool canMakeFromLoose = CanForm(recipe, loosePool);

        // 3. THE LOGIC:
        // If we already have the molecule AND we can't make a NEW one from just the loose atoms,
        // then any "match" found by the Total Pool is just a redundant loop.
        if (existingCount > 0 && !canMakeFromLoose)
        {
            // One exception: If the loose atoms help make a BIGGER molecule (like CH4), 
            // this function won't be called for H2 because CheckForUpgrades sorts by complexity.
            return true;
        }

        return false;
    }

    private void CreateMultiComponentMolecule(MoleculeData recipe)
    {
        // 1. Calculate Spawn Position (Average of ingredients)
        Vector3 spawnPos = transform.position + Vector3.up * 0.1f;

        // 2. Use your existing "Smart" ConsumeIngredients logic!
        // Since ConsumeIngredients already handles both loose atoms AND molecules,
        // it will naturally take 1 Carbon, the first H2, and then the second H2.
        ConsumeIngredients(recipe);

        // 3. Spawn the CH4
        GameObject newMol = Instantiate(recipe.moleculePrefab, spawnPos, Quaternion.identity);
        newMol.transform.position += new Vector3(Random.Range(-0.05f, 0.05f), 0, Random.Range(-0.05f, 0.05f));

        // Trigger Sound!
        AudioManager.Instance.PlayMoleculeCreated(spawnPos);

        Debug.Log($"Successfully Formed Multi-Component: {recipe.moleculeName}");

        if (uiManager != null) uiManager.OnMoleculeDiscovered(recipe);
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

        // Trigger Sound!
        AudioManager.Instance.PlayMoleculeCreated(spawnPos);

        if (uiManager != null)
        {
            uiManager.OnMoleculeDiscovered(data);
        }
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