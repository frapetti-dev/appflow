using System.IO;
using System.Linq;
using Unity.GraphToolkit.Editor;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AppFlow.Editor
{
    /// <summary>`unity command appflow.graph ...`: inspect and author .appflow graphs in edit mode.</summary>
    static class AppFlowGraphCommands
    {
        const string Group = "appflow.graph";
        const string Saves = " Saves the asset.";

        // ---- helpers ----

        static ExitCode Open(CommandContext cmd, string asset, out AppFlowStateMachine sm)
        {
            sm = null;
            var path = ResolveAssetPath(cmd, asset, out var code);
            if (path == null) return code;
            sm = StateMachineDatabase.LoadStateMachine<AppFlowStateMachine>(path);
            if (sm == null)
            {
                cmd.Log.Error($"No app flow graph at '{path}'.");
                return ExitCode.NotFound;
            }
            return ExitCode.Success;
        }

        static string ResolveAssetPath(CommandContext cmd, string asset, out ExitCode code)
        {
            code = ExitCode.Success;
            var path = ProjectPaths.Resolve(asset, out var error);
            if (path == null)
            {
                cmd.Log.Error(error);
                code = ExitCode.InvalidUsage;
                return null;
            }
            var ext = "." + AppFlowStateMachine.AssetExtension;
            return path.EndsWith(ext) ? path : path + ext;
        }

        static ExitCode FindState(CommandContext cmd, AppFlowStateMachine sm, string nameOrId, out AppState state)
        {
            state = null;
            var states = sm.GetStates().OfType<AppState>().ToList();
            var byName = states.Where(s => string.Equals(s.StateName, nameOrId, System.StringComparison.OrdinalIgnoreCase)).ToList();
            if (byName.Count == 1) { state = byName[0]; return ExitCode.Success; }
            if (byName.Count > 1)
            {
                cmd.Log.Error($"Name '{nameOrId}' is ambiguous: " + string.Join(", ", byName.Select(s => $"{s.StateName} ({s.ID})")));
                return ExitCode.InvalidUsage;
            }
            var id = Hash128.Parse(nameOrId ?? "");
            if (id.isValid) state = states.FirstOrDefault(s => s.ID == id);
            if (state != null) return ExitCode.Success;
            cmd.Log.Error($"No state named or with id '{nameOrId}'");
            return ExitCode.NotFound;
        }

        // Empty path -> null object (clears). Missing or wrongly typed asset -> NotFound.
        static ExitCode LoadObject<T>(CommandContext cmd, string assetPath, out T obj) where T : Object
        {
            obj = null;
            if (string.IsNullOrEmpty(assetPath)) return ExitCode.Success;
            var path = ProjectPaths.Resolve(assetPath, out var error);
            if (path == null)
            {
                cmd.Log.Error(error);
                return ExitCode.InvalidUsage;
            }
            obj = AssetDatabase.LoadAssetAtPath<T>(path);
            if (obj != null) return ExitCode.Success;
            cmd.Log.Error($"No {typeof(T).Name} at '{path}'.");
            return ExitCode.NotFound;
        }

        static string PathOf(Object o) => o != null ? AssetDatabase.GetAssetPath(o) : null;

        // ---- commands ----

        [CliGroupCommand(Group, "List the states, transitions and validation issues of an app flow graph.", Effect = CommandEffect.ReadOnly, MainThreadRequired = true)]
        internal static ExitCode Describe(CommandContext cmd, [CliArg("asset", "Path of the .appflow asset", Required = true)] string asset)
        {
            var code = Open(cmd, asset, out var sm);
            if (code != ExitCode.Success) return code;

            foreach (var s in sm.GetStates().OfType<AppState>())
                cmd.Result.WriteRecord(new
                {
                    kind = "state",
                    name = s.StateName,
                    id = s.ID.ToString(),
                    entry = s.Get<bool>(AppState.OptEntry),
                    behaviour = PathOf(s.Get<AppStateBehaviour>(AppState.OptBehaviour)),
                    scene = PathOf(s.Get<SceneAsset>(AppState.OptScene)),
                    loadMode = s.Get<LoadSceneMode>(AppState.OptLoadMode).ToString(),
                    unloadOnExit = s.Get<bool>(AppState.OptUnloadOnExit),
                });

            foreach (var s in sm.GetStates().OfType<AppState>())
                foreach (var t in s.GetOutgoingTransitions())
                {
                    var conditions = AppFlowValidation.ConditionsOf(t).ToList();
                    cmd.Result.WriteRecord(new
                    {
                        kind = "transition",
                        id = t.ID.ToString(),
                        from = AppState.NameOf(t.FromState),
                        to = AppState.NameOf(t.ToState),
                        customTransition = PathOf(conditions.OfType<CustomTransitionCondition>().FirstOrDefault()?.Value),
                        crossfade = conditions.OfType<CrossfadeCondition>().Any(),
                    });
                }

            foreach (var issue in AppFlowValidation.Validate(sm))
                cmd.Result.WriteRecord(new
                {
                    kind = "issue",
                    severity = issue.IsError ? "error" : "warning",
                    message = issue.Message,
                    context = issue.Context switch
                    {
                        IState st => AppState.NameOf(st),
                        ITransition tr => $"{AppState.NameOf(tr.FromState)} -> {AppState.NameOf(tr.ToState)}",
                        _ => null,
                    },
                });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Create an empty app flow graph." + Saves, Effect = CommandEffect.Additive, MainThreadRequired = true)]
        internal static ExitCode Create(CommandContext cmd, [CliArg("asset", "Path of the .appflow asset to create", Required = true)] string asset)
        {
            var path = ResolveAssetPath(cmd, asset, out var code);
            if (path == null) return code;
            if (File.Exists(path))
            {
                cmd.Log.Error($"Asset already exists: '{path}'");
                return ExitCode.InvalidUsage;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var sm = StateMachineDatabase.CreateStateMachine<AppFlowStateMachine>(path);
            if (sm == null)
            {
                cmd.Log.Error($"Could not create '{path}'.");
                return ExitCode.Failed;
            }
            cmd.Result.Write(new { asset = path });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Add a state to an app flow graph." + Saves, Effect = CommandEffect.Additive, MainThreadRequired = true)]
        internal static ExitCode AddState(
            CommandContext cmd,
            [CliArg("asset", "Path of the .appflow asset", Required = true)] string asset,
            [CliArg("name", "Unique state name", Required = true)] string name,
            [CliArg("x", "Graph x position")] float x = 0,
            [CliArg("y", "Graph y position")] float y = 0,
            [CliArg("entry", "Make this the Entry state")] bool entry = false)
        {
            var code = Open(cmd, asset, out var sm);
            if (code != ExitCode.Success) return code;
            if (sm.GetStates().Any(s => string.Equals(AppState.NameOf(s), name, System.StringComparison.OrdinalIgnoreCase)))
            {
                cmd.Log.Error($"A state named '{name}' already exists.");
                return ExitCode.InvalidUsage;
            }

            var state = new AppState { Position = new Vector2(x, y) };
            sm.UndoBeginRecordStateMachine("add_state");
            try
            {
                sm.AddState(state);
                state.GetOptionByName(AppState.OptName).TrySetValue(name);
                state.Title = name; // shows immediately in an open window; the option is what persists
                state.GetOptionByName(AppState.OptEntry).TrySetValue(entry);
                StateMachineDatabase.SaveStateMachine(sm);
            }
            finally { sm.UndoEndRecordStateMachine(); }
            cmd.Result.Write(new { name, id = state.ID.ToString() });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Connect two states and set what the edge does (custom transition and/or crossfade)." + Saves, Effect = CommandEffect.Idempotent, MainThreadRequired = true)]
        internal static ExitCode Connect(
            CommandContext cmd,
            [CliArg("asset", "Path of the .appflow asset", Required = true)] string asset,
            [CliArg("from", "Source state name or id", Required = true)] string from,
            [CliArg("to", "Target state name or id", Required = true)] string to,
            [CliArg("transition", "Asset path of an AppTransitionBehaviour; empty for none")] string transition = "",
            [CliArg("crossfade", "Start source Exit and target Enter together")] bool crossfade = false)
        {
            var code = Open(cmd, asset, out var sm);
            if (code != ExitCode.Success) return code;
            if ((code = FindState(cmd, sm, from, out var a)) != ExitCode.Success) return code;
            if ((code = FindState(cmd, sm, to, out var b)) != ExitCode.Success) return code;
            if ((code = LoadObject<AppTransitionBehaviour>(cmd, transition, out var behaviour)) != ExitCode.Success) return code;

            var existing = sm.GetTransitions(a, b).FirstOrDefault();
            var oldConditions = existing != null ? AppFlowValidation.ConditionsOf(existing).ToArray() : new Condition[0];
            sm.UndoBeginRecordStateMachine("connect", oldConditions);
            ITransition t;
            try
            {
                t = existing ?? sm.Connect(a, b);
                var root = t.GetRule(0).RootCondition;
                foreach (var c in root.Get().Where(c => c is CustomTransitionCondition or CrossfadeCondition).ToList())
                    root.Remove(c);
                if (behaviour != null) root.Add(new CustomTransitionCondition { Value = behaviour });
                if (crossfade) root.Add(new CrossfadeCondition());
                StateMachineDatabase.SaveStateMachine(sm);
            }
            finally { sm.UndoEndRecordStateMachine(); }
            cmd.Result.Write(new { id = t.ID.ToString(), from = a.StateName, to = b.StateName, customTransition = PathOf(behaviour), crossfade });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Set the AppStateBehaviour of a state (empty clears)." + Saves, Effect = CommandEffect.Idempotent, MainThreadRequired = true)]
        internal static ExitCode SetBehaviour(
            CommandContext cmd,
            [CliArg("asset", "Path of the .appflow asset", Required = true)] string asset,
            [CliArg("state", "State name or id", Required = true)] string state,
            [CliArg("behaviour", "Asset path of an AppStateBehaviour; empty clears")] string behaviour = "")
        {
            var code = Open(cmd, asset, out var sm);
            if (code != ExitCode.Success) return code;
            if ((code = FindState(cmd, sm, state, out var s)) != ExitCode.Success) return code;
            if ((code = LoadObject<AppStateBehaviour>(cmd, behaviour, out var obj)) != ExitCode.Success) return code;

            sm.UndoBeginRecordStateMachine("set_behaviour");
            try
            {
                s.GetOptionByName(AppState.OptBehaviour).TrySetValue(obj);
                StateMachineDatabase.SaveStateMachine(sm);
            }
            finally { sm.UndoEndRecordStateMachine(); }
            cmd.Result.Write(new { state = s.StateName, behaviour = PathOf(obj) });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Set the scene a state loads (empty clears)." + Saves, Effect = CommandEffect.Idempotent, MainThreadRequired = true)]
        internal static ExitCode SetScene(
            CommandContext cmd,
            [CliArg("asset", "Path of the .appflow asset", Required = true)] string asset,
            [CliArg("state", "State name or id", Required = true)] string state,
            [CliArg("scene", "Asset path of a scene; empty clears")] string scene = "",
            [CliArg("load-mode", "Single or Additive")] LoadSceneMode loadMode = LoadSceneMode.Additive,
            [CliArg("unload-on-exit", "Unload an additive scene when the state exits")] bool unloadOnExit = true)
        {
            var code = Open(cmd, asset, out var sm);
            if (code != ExitCode.Success) return code;
            if ((code = FindState(cmd, sm, state, out var s)) != ExitCode.Success) return code;
            if ((code = LoadObject<SceneAsset>(cmd, scene, out var obj)) != ExitCode.Success) return code;

            sm.UndoBeginRecordStateMachine("set_scene");
            try
            {
                s.GetOptionByName(AppState.OptScene).TrySetValue(obj);
                s.GetOptionByName(AppState.OptLoadMode).TrySetValue(loadMode);
                s.GetOptionByName(AppState.OptUnloadOnExit).TrySetValue(unloadOnExit);
                StateMachineDatabase.SaveStateMachine(sm);
            }
            finally { sm.UndoEndRecordStateMachine(); }
            cmd.Result.Write(new { state = s.StateName, scene = PathOf(obj), loadMode = loadMode.ToString(), unloadOnExit });
            return ExitCode.Success;
        }
    }
}
