using System.Collections.Generic;
using UnityEngine;

namespace AppFlow
{
    /// <summary>Runtime form of an .appflow graph, produced by the importer.</summary>
    public sealed class AppFlowAsset : ScriptableObject
    {
        public Hash128 GraphId;
        public Hash128 EntryStateId;
        public List<AppStateData> States = new();
        public List<AppTransitionData> Transitions = new();

        public AppStateData FindState(Hash128 id)
        {
            foreach (var s in States)
                if (s.Id == id) return s;
            return null;
        }

        public AppTransitionData FindTransition(Hash128 from, Hash128 to)
        {
            foreach (var t in Transitions)
                if (t.From == from && t.To == to) return t;
            return null;
        }
    }
}
