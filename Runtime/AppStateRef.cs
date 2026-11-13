using UnityEngine;

namespace AppFlow
{
    /// <summary>Handle to a state of an <see cref="AppFlowAsset"/>. Created by the importer as a sub-asset; drag it into serialized fields.</summary>
    public sealed class AppStateRef : ScriptableObject
    {
        [SerializeField] Hash128 m_StateId;

        public Hash128 StateId => m_StateId;

        internal void Init(Hash128 id, string stateName)
        {
            m_StateId = id;
            name = stateName;
        }
    }
}
