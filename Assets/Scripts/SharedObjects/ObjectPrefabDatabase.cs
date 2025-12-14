using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mapping between object type IDs and their prefabs
/// </summary>
[CreateAssetMenu(fileName = "ObjectPrefabDatabase", menuName = "edu_MRSIVE/Data/Object Prefab Database")]
public class ObjectPrefabDatabase : ScriptableObject
{
    [System.Serializable]
    public class PrefabEntry
    {
        public string typeId;
        public GameObject prefab;
    }

    [SerializeField] private List<PrefabEntry> prefabMappings = new List<PrefabEntry>();
    
    private Dictionary<string, GameObject> _prefabCache;

    public void Initialize()
    {
        _prefabCache = new Dictionary<string, GameObject>();
        
        foreach (var entry in prefabMappings)
        {
            if (string.IsNullOrEmpty(entry.typeId))
            {
                Debug.LogWarning("ObjectPrefabDatabase: Found entry with empty ID");
                continue;
            }

            if (entry.prefab == null)
            {
                Debug.LogWarning($"ObjectPrefabDatabase: No prefab assigned for ID '{entry.typeId}'");
                continue;
            }

            if (_prefabCache.ContainsKey(entry.typeId))
            {
                Debug.LogWarning($"ObjectPrefabDatabase: Duplicate ID '{entry.typeId}' found");
                continue;
            }

            _prefabCache[entry.typeId] = entry.prefab;
        }
    }

    public GameObject GetPrefab(ObjectTypeId typeId)
    {
        if (_prefabCache == null)
            Initialize();

        _prefabCache.TryGetValue(typeId.value, out GameObject prefab);
        return prefab;
    }

    public bool HasPrefab(ObjectTypeId typeId)
    {
        if (_prefabCache == null)
            Initialize();

        return _prefabCache.ContainsKey(typeId.value);
    }
}