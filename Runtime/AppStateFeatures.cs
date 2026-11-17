using System;
using System.Collections.Generic;
using UnityEngine;

namespace AppFlow
{
    /// <summary>Graph option value holding a state's features (a distinct type so a PropertyDrawer can target it).</summary>
    [Serializable]
    public sealed class AppStateFeatures
    {
        [SerializeReference] public List<AppStateFeature> Items = new();
    }
}
