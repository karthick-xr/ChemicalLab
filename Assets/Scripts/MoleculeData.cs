// Atom types for easy comparison
using System.Collections.Generic;
using UnityEngine;

public enum AtomType { H, O, C, N }

[CreateAssetMenu(fileName = "NewMolecule", menuName = "Chemistry/Molecule")]
public class MoleculeData : ScriptableObject
{
    public string moleculeName;
    public string formula;
    public GameObject moleculePrefab; // The 3D visual of the combined molecule
    public List<AtomCount> requiredAtoms;
    public string bondType; // Single, Double, Triple
    public Sprite moleculeIcon;
}

[System.Serializable]
public struct AtomCount
{
    public AtomType type;
    public int count;
}