using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AppFlow
{
    /// <summary>Runs an <see cref="AppFlowAsset"/>. Plain C#: usable from tests without a scene.</summary>
    // The only static is an event raised for editor visualization; subscribers unsubscribe themselves.
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
    public sealed class AppFlowMachine
    {
        public const int HistoryCapacity = 32;

        internal sealed class StateEntry
        {
            public AppStateData Data;
            public AppStateBehaviour Behaviour;
            public AppStateRef Ref;
            public bool OwnsRef;
        }

        /// <summary>Raised on every phase start; <see cref="AppPhase.None"/> for the left state after its PostExit.</summary>
        public static event Action<AppFlowMachine, AppStateRef, AppPhase> PhaseChanged;

        public event Action<AppTransitionRecord> TransitionCompleted;

        /// <summary>Raised when a transition that follows a graph edge begins (used for graph visualization).</summary>
        public event Action<AppTransitionData> EdgeTransitionStarted;
        readonly Dictionary<Hash128, StateEntry> m_States = new();
        readonly List<AppTransitionRecord> m_History = new();
        AppTransitionRecord m_Active;

        public AppFlowAsset Asset { get; }
        public AppStateRef CurrentState { get; private set; }
        public AppStateBehaviour CurrentBehaviour { get; private set; }
        public AppPhase CurrentPhase { get; private set; }
        public bool IsTransitioning { get; private set; }
        /// <summary>The running transition (null when idle); its record is still being filled.</summary>
        public AppTransitionRecord ActiveTransition => m_Active;
        public IReadOnlyList<AppTransitionRecord> History => m_History;

        public AppFlowMachine(AppFlowAsset asset)
        {
            Asset = asset;
            foreach (var data in asset.States)
            {
                var e = new StateEntry
                {
                    Data = data,
                    Behaviour = data.Behaviour != null ? Object.Instantiate(data.Behaviour) : ScriptableObject.CreateInstance<AppStateBehaviour>(),
                    Ref = data.Ref,
                };
                if (e.Ref == null)
                {
                    // Assets built in memory (tests) have no importer-made refs.
                    e.Ref = ScriptableObject.CreateInstance<AppStateRef>();
                    e.Ref.Init(data.Id, data.Name);
                    e.OwnsRef = true;
                }
                e.Behaviour.Machine = this;
                e.Behaviour.State = e.Ref;
                m_States[data.Id] = e;
            }
        }

        public async Awaitable<bool> Start()
        {
            if (!m_States.TryGetValue(Asset.EntryStateId, out var entry))
            {
                Debug.LogError($"AppFlow: no Entry state in {Asset.name}");
                return false;
            }
            return await Run(entry);
        }

        public Awaitable<bool> TransitTo(AppStateRef target)
        {
            if (target == null || !m_States.ContainsKey(target.StateId))
                return Fail($"AppFlow: state '{(target != null ? target.name : "null")}' is not part of {Asset.name}");
            return TransitTo(target.StateId);
        }

        public async Awaitable<bool> TransitTo(Hash128 stateId)
        {
            if (!m_States.TryGetValue(stateId, out var entry))
            {
                Debug.LogError($"AppFlow: state '{stateId}' is not part of {Asset.name}");
                return false;
            }
            return await Run(entry);
        }

        static async Awaitable<bool> Fail(string message)
        {
            Debug.LogError(message);
            return false;
        }

        async Awaitable<bool> Run(StateEntry to)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning($"AppFlow: transition to '{to.Data.Name}' rejected: '{m_Active.From}' -> '{m_Active.To}' in progress");
                return false;
            }

            IsTransitioning = true;
            var from = CurrentState != null ? m_States[CurrentState.StateId] : null;
            var edge = from != null ? Asset.FindTransition(from.Data.Id, to.Data.Id) : null;
            var record = new AppTransitionRecord
            {
                From = from?.Data.Name,
                To = to.Data.Name,
                Custom = edge?.Behaviour != null,
                Crossfade = edge != null && edge.Crossfade,
                StartedAt = Time.realtimeSinceStartupAsDouble,
            };
            m_Active = record;
            if (edge != null) EdgeTransitionStarted?.Invoke(edge);
            var ctx = new AppTransitionContext(this, from, to, record.Crossfade, record);

            try
            {
                if (record.Custom)
                {
                    var instance = Object.Instantiate(edge.Behaviour);
                    try { await instance.Run(ctx); }
                    catch (Exception e) { Debug.LogException(e); record.Exceptions++; }
                    finally { DestroyObject(instance); }
                }
                else
                {
                    await ctx.RunDefault(record.Crossfade);
                }

                // Running is always reached, even if a custom transition skipped every target phase.
                Switch(to);
                CurrentPhase = AppPhase.Running;
                RaisePhase(to.Ref, AppPhase.Running);
                try { to.Behaviour.OnRunning(); }
                catch (Exception e) { Debug.LogException(e); record.Exceptions++; }

                record.Duration = Time.realtimeSinceStartupAsDouble - record.StartedAt;
                m_History.Add(record);
                if (m_History.Count > HistoryCapacity) m_History.RemoveAt(0);
                TransitionCompleted?.Invoke(record);
                return true;
            }
            finally
            {
                m_Active = null;
                IsTransitioning = false;
            }
        }

        void Switch(StateEntry entry)
        {
            CurrentState = entry.Ref;
            CurrentBehaviour = entry.Behaviour;
        }

        internal async Awaitable RunPhase(StateEntry entry, AppPhase phase, AppTransitionContext ctx)
        {
            var isEnter = phase is AppPhase.PreEnter or AppPhase.Enter or AppPhase.PostEnter;
            // The target becomes current when its first phase starts; the source stays current until then.
            if (isEnter && CurrentState != entry.Ref) Switch(entry);
            CurrentPhase = phase;
            RaisePhase(entry.Ref, phase);

            var start = Time.realtimeSinceStartupAsDouble;
            var data = entry.Data;
            var b = entry.Behaviour;
            try
            {
                switch (phase)
                {
                    case AppPhase.PreEnter: await b.OnPreEnter(ctx); break;
                    case AppPhase.Enter:
                        if (!string.IsNullOrEmpty(data.ScenePath))
                            await SceneManager.LoadSceneAsync(data.ScenePath, data.LoadMode);
                        await b.OnEnter(ctx);
                        break;
                    case AppPhase.PostEnter: await b.OnPostEnter(ctx); break;
                    case AppPhase.PreExit: await b.OnPreExit(ctx); break;
                    case AppPhase.Exit:
                        await b.OnExit(ctx);
                        if (data.UnloadOnExit && data.LoadMode == LoadSceneMode.Additive && !string.IsNullOrEmpty(data.ScenePath)
                            && SceneManager.GetSceneByPath(data.ScenePath).isLoaded)
                            await SceneManager.UnloadSceneAsync(data.ScenePath);
                        break;
                    case AppPhase.PostExit: await b.OnPostExit(ctx); break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ctx.Record.Exceptions++;
            }

            ctx.Record.Phases.Add(new AppPhaseTiming { State = data.Name, Phase = phase, Seconds = Time.realtimeSinceStartupAsDouble - start });

            // Re-enter keeps the same state, so it must not look "left".
            if (phase == AppPhase.PostExit && ctx.FromRef != ctx.ToRef)
                RaisePhase(entry.Ref, AppPhase.None);
        }

        void RaisePhase(AppStateRef state, AppPhase phase)
        {
            try { PhaseChanged?.Invoke(this, state, phase); }
            catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>Call once per frame; runs OnRunningUpdate of the current state while Running.</summary>
        public void Tick()
        {
            if (CurrentPhase != AppPhase.Running || CurrentBehaviour == null) return;
            try { CurrentBehaviour.OnRunningUpdate(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        public bool TryFindState(string nameOrId, out AppStateData state, out string error)
        {
            state = null;
            error = null;
            List<AppStateData> matches = null;
            foreach (var s in Asset.States)
                if (string.Equals(s.Name, nameOrId, StringComparison.OrdinalIgnoreCase))
                    (matches ??= new()).Add(s);

            if (matches is { Count: 1 }) { state = matches[0]; return true; }
            if (matches != null)
            {
                error = $"Name '{nameOrId}' is ambiguous: " + string.Join(", ", matches.ConvertAll(m => $"{m.Name} ({m.Id})"));
                return false;
            }

            var id = Hash128.Parse(nameOrId ?? "");
            if (id.isValid && (state = Asset.FindState(id)) != null) return true;
            error = $"No state named or with id '{nameOrId}'";
            return false;
        }

        public void Dispose()
        {
            foreach (var e in m_States.Values)
            {
                DestroyObject(e.Behaviour);
                if (e.OwnsRef) DestroyObject(e.Ref);
            }
            m_States.Clear();
        }

        static void DestroyObject(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }
    }
}
