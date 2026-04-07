using UnityEngine;

[CreateAssetMenu(fileName = "AtomRegistry", menuName = "Chemistry/Atom Registry")]
public class AtomRegistry : ScriptableObject
{
    public GameObject prefabH;
    public GameObject prefabO;
    public GameObject prefabC;
    public GameObject prefabN;

    public GameObject GetPrefab(AtomType type)
    {
        return type switch
        {
            AtomType.H => prefabH,
            AtomType.O => prefabO,
            AtomType.C => prefabC,
            AtomType.N => prefabN,
            _ => null
        };
    }
}