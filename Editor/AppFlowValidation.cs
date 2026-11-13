using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace AppFlow.Editor
{
    public readonly struct AppFlowIssue
    {
        public readonly bool IsError;
        public readonly string Message;
        public readonly object Context;

        public AppFlowIssue(bool isError, string message, object context)
        {
            IsError = isError;
            Message = message;
            Context = context;
        }
    }

    public static class AppFlowValidation
    {
        public static IEnumerable<AppFlowIssue> Validate(AppFlowStateMachine sm)
        {
            var states = sm.GetStates().OfType<AppState>().ToList();

            var entries = states.Where(s => s.Get<bool>(AppState.OptEntry)).ToList();
            if (entries.Count == 0)
                yield return new AppFlowIssue(true, "No Entry state: tick 'Entry' on one state.", null);
            for (var i = 1; i < entries.Count; i++)
                yield return new AppFlowIssue(true, "Only one state can be Entry.", entries[i]);

            foreach (var group in states.GroupBy(s => (s.StateName ?? "").ToLowerInvariant()).Where(g => g.Count() > 1))
                foreach (var s in group)
                    yield return new AppFlowIssue(true, $"Duplicate state name '{s.StateName}': names must be unique for code and CLI lookup.", s);

            foreach (var s in states)
            {
                var scene = s.Get<SceneAsset>(AppState.OptScene);
                if (scene == null) continue;
                var path = AssetDatabase.GetAssetPath(scene);
                if (!EditorBuildSettings.scenes.Any(b => b.enabled && b.path == path))
                    yield return new AppFlowIssue(false, $"Scene '{path}' is not in the build scene list.", s);
                if (s.Get<bool>(AppState.OptUnloadOnExit) && s.Get<LoadSceneMode>(AppState.OptLoadMode) == LoadSceneMode.Single)
                    yield return new AppFlowIssue(false, "Unload on exit is ignored for Single-mode scenes.", s);
            }

            foreach (var s in states)
                foreach (var t in s.GetOutgoingTransitions())
                {
                    var conditions = ConditionsOf(t).ToList();
                    var customs = conditions.OfType<CustomTransitionCondition>().ToList();
                    if (customs.Count > 1)
                        yield return new AppFlowIssue(true, "Only one Custom Transition per edge.", t);
                    if (customs.Any(c => c.Value == null))
                        yield return new AppFlowIssue(true, "Custom Transition has no behaviour asset.", t);
                    if (customs.Count == 0 && !conditions.OfType<CrossfadeCondition>().Any())
                        yield return new AppFlowIssue(false, "Edge has no effect: add a Custom Transition or Crossfade condition.", t);
                }
        }

        /// <summary>User conditions of all enabled rules, recursing through groups; built-in variable conditions are skipped.</summary>
        internal static IEnumerable<Condition> ConditionsOf(ITransition transition)
        {
            foreach (var rule in transition.GetRules())
            {
                if (!rule.Enabled) continue;
                foreach (var c in Walk(rule.RootCondition)) yield return c;
            }
        }

        static IEnumerable<Condition> Walk(ICondition condition)
        {
            if (condition is IGroupCondition group)
            {
                foreach (var child in group.Get())
                    foreach (var c in Walk(child)) yield return c;
            }
            else if (condition is Condition c)
            {
                yield return c;
            }
        }
    }
}
