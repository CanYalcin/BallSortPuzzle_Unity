using System;
using UnityEngine;

namespace HyperBase.VFX
{
    [Serializable]
    public class VFXEntry
    {
        public VFXType    Type;
        public GameObject Prefab;
        [Min(0)] public int PrewarmCount = 3;
    }

    [CreateAssetMenu(fileName = "VFXConfig", menuName = "HyperBase/VFX Config")]
    public class VFXConfig : ScriptableObject
    {
        public VFXEntry[] Effects;
    }
}
