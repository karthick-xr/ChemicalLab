using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MoleculeDatabase", menuName = "Chemistry/Database")]
public class MoleculeDatabase : ScriptableObject
{
    public List<MoleculeData> allMolecules;

    public MoleculeData CheckMatch(Dictionary<AtomType, int> currentAtoms)
    {
        foreach (var molecule in allMolecules)
        {
            if (IsMatch(molecule, currentAtoms)) return molecule;
        }
        return null;
    }

    private bool IsMatch(MoleculeData data, Dictionary<AtomType, int> currentAtoms)
    {
        if (data.requiredAtoms.Count != currentAtoms.Count) return false;
        foreach (var req in data.requiredAtoms)
        {
            if (!currentAtoms.TryGetValue(req.type, out int val) || val != req.count)
                return false;
        }
        return true;
    }
}