using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace AppFlow.Editor
{
    [Serializable]
    [State("App Flow", AppFlowIcons.State, "App State")]
    public sealed class AppState : State
    {
        public const string OptName = "Name";
        public const string OptEntry = "Entry";
        public const string OptBehaviour = "Behaviour";
        public const string OptScene = "Scene";
        public const string OptLoadMode = "LoadMode";
        public const string OptUnloadOnExit = "UnloadOnExit";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // State.Title set from code is not persisted by Graph Toolkit, so the name lives in an option.
            context.AddOption<string>(OptName).WithTooltip("State name used by code and CLI; shown as the state title").Build();
            context.AddOption<bool>(OptEntry).WithTooltip("Initial state entered on Start").Build();
            context.AddOption<AppStateBehaviour>(OptBehaviour).WithTooltip("Lifecycle hooks of this state").Build();
            context.AddOption<SceneAsset>(OptScene).WithTooltip("Scene loaded in Enter").Build();
            context.AddOption<LoadSceneMode>(OptLoadMode).WithDefaultValue(LoadSceneMode.Additive).Build();
            context.AddOption<bool>(OptUnloadOnExit).WithDefaultValue(true).Build();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            var name = Get<string>(OptName);
            if (!string.IsNullOrEmpty(name)) Title = name;
        }

        /// <summary>The Name option, else the title typed in the graph.</summary>
        public string StateName
        {
            get
            {
                var name = Get<string>(OptName);
                return string.IsNullOrEmpty(name) ? Title : name;
            }
        }

        public static string NameOf(IState state) => state is AppState a ? a.StateName : state.Title;

        public T Get<T>(string option)
        {
            T value = default;
            GetOptionByName(option)?.TryGetValue(out value);
            return value;
        }
    }
}
