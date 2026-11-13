using UnityEngine;
#if UNITY_EDITOR
using Unity.GraphToolkit.Editor.GraphVisualization;
#endif

namespace AppFlow
{
    /// <summary>Boots an <see cref="AppFlowAsset"/>: owns the machine, ticks it, and mirrors it into the open graph window in the Editor.</summary>
    [DisallowMultipleComponent]
    // Active is reset and PhaseChanged unsubscribed in OnDestroy; nothing for the automatic cleanup to do.
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
    public sealed class AppFlowRunner : MonoBehaviour
    {
        [SerializeField] AppFlowAsset m_Asset;

        public static AppFlowRunner Active { get; private set; }
        public AppFlowMachine Machine { get; private set; }

        void Awake()
        {
            if (Active != null && Active != this)
            {
                Destroy(gameObject);
                return;
            }
            if (m_Asset == null)
            {
                Debug.LogError("AppFlow: AppFlowRunner has no AppFlowAsset", this);
                enabled = false;
                return;
            }
            Active = this;
            DontDestroyOnLoad(gameObject);
            Machine = new AppFlowMachine(m_Asset);
#if UNITY_EDITOR
            AppFlowMachine.PhaseChanged += OnPhaseChanged;
            Machine.EdgeTransitionStarted += OnEdgeStarted;
            Machine.TransitionCompleted += OnTransitionCompleted;
#endif
        }

        void Start() => _ = Machine.Start();

        void Update()
        {
            Machine.Tick();
#if UNITY_EDITOR
            RefreshVisualization();
#endif
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
            if (Machine == null) return;
#if UNITY_EDITOR
            AppFlowMachine.PhaseChanged -= OnPhaseChanged;
            Machine.EdgeTransitionStarted -= OnEdgeStarted;
            Machine.TransitionCompleted -= OnTransitionCompleted;
            DisposeVisualization();
#endif
            Machine.Dispose();
        }

        public Awaitable<bool> TransitTo(AppStateRef target) => Machine.TransitTo(target);

#if UNITY_EDITOR
        Context m_Viz;
        bool m_OwnsViz;
        bool m_WasLoaded;
        Hash128 m_ActiveEdge;

        // Null until a graph window has the graph loaded.
        Context Viz()
        {
            if (m_Viz == null)
            {
                m_Viz = Registry.GetActiveContext(m_Asset.GraphId);
                if (m_Viz == null)
                {
                    m_Viz = Registry.CreateVisualizationContext(m_Asset.GraphId);
                    m_OwnsViz = true;
                }
            }
            return m_Viz is { IsValid: true, IsGraphLoaded: true } ? m_Viz : null;
        }

        void OnPhaseChanged(AppFlowMachine machine, AppStateRef state, AppPhase phase)
        {
            if (machine != Machine) return;
            var ctx = Viz();
            if (ctx == null) return;
            var reference = ctx.GetStateReference(state.StateId);
            switch (phase)
            {
                case AppPhase.PreEnter:
                case AppPhase.Enter:
                case AppPhase.PostEnter:
                    ctx.Motion.Play(reference, 3f);
                    break;
                case AppPhase.Running:
                    ctx.Motion.Play(reference, 1f);
                    break;
                case AppPhase.None:
                    ctx.Motion.Stop(reference);
                    reference.ClearCustomization();
                    break;
            }
        }

        void OnEdgeStarted(AppTransitionData edge)
        {
            var ctx = Viz();
            if (ctx == null) return;
            m_ActiveEdge = edge.Id;
            var t = ctx.GetTransitionReference(edge.Id);
            t.LineColor = DebugStyles.Pending;
            t.WidthOverride = 4f;
            ctx.Motion.Play(t, 1f);
        }

        void OnTransitionCompleted(AppTransitionRecord record)
        {
            if (!m_ActiveEdge.isValid) return;
            var id = m_ActiveEdge;
            m_ActiveEdge = default;
            var ctx = Viz();
            if (ctx == null) return;
            var t = ctx.GetTransitionReference(id);
            ctx.Motion.Stop(t);
            t.ClearCustomization();
        }

        // The graph window may be opened after Play started: re-apply the current state when it loads.
        void RefreshVisualization()
        {
            var loaded = Viz() != null;
            if (loaded && !m_WasLoaded && Machine.CurrentState != null && Machine.CurrentPhase == AppPhase.Running)
                OnPhaseChanged(Machine, Machine.CurrentState, AppPhase.Running);
            m_WasLoaded = loaded;
        }

        void DisposeVisualization()
        {
            if (m_Viz == null) return;
            if (m_Viz.IsValid) m_Viz.ClearAllVisualization();
            if (m_OwnsViz) m_Viz.Dispose();
            m_Viz = null;
        }
#endif
    }
}
