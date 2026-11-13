using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace AppFlow.Editor
{
    [Serializable]
    [StateMachine(AssetExtension)]
    public sealed class AppFlowStateMachine : StateMachine
    {
        public const string AssetExtension = "appflow";

        [MenuItem("Assets/Create/App Flow/App Flow Graph")]
        static void Create() => StateMachineDatabase.PromptInProjectBrowserToCreateNewAsset<AppFlowStateMachine>("New App Flow");

        public override void OnStateMachineChanged(StateMachineLogger logger)
        {
            foreach (var issue in AppFlowValidation.Validate(this))
            {
                if (issue.IsError) logger.LogError(issue.Message, issue.Context);
                else logger.LogWarning(issue.Message, issue.Context);
            }
        }
    }
}
