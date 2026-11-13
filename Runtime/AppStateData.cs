using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AppFlow
{
    [Serializable]
    public sealed class AppStateData
    {
        public Hash128 Id;
        public string Name;
        public AppStateRef Ref;
        public AppStateBehaviour Behaviour;
        public string ScenePath;
        public LoadSceneMode LoadMode;
        public bool UnloadOnExit;
    }
}
