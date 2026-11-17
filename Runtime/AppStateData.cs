using System;
using System.Collections.Generic;
using UnityEngine;

namespace AppFlow
{
    [Serializable]
    public sealed class AppStateData
    {
        public Hash128 Id;
        public string Name;
        public AppStateRef Ref;
        public AppStateBehaviour Behaviour;
        [SerializeReference] public List<AppStateFeature> Features = new();
    }
}
