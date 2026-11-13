using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AppFlow.Editor
{
    /// <summary>Shows the live phase of a state as a badge while the game runs.</summary>
    public sealed class AppStateView : StateView<AppState>
    {
        Label m_Badge;

        public override void OnViewBuilt()
        {
            m_Badge = new Label { name = "appflow-badge", pickingMode = PickingMode.Ignore };
            m_Badge.AddToClassList("appflow-badge");
            var s = m_Badge.style;
            s.unityFontStyleAndWeight = FontStyle.Bold;
            s.color = Color.white;
            s.backgroundColor = (Color)new Color32(0x2E, 0x7D, 0x32, 0xFF);
            s.borderTopLeftRadius = s.borderTopRightRadius = s.borderBottomLeftRadius = s.borderBottomRightRadius = 4;
            s.paddingLeft = s.paddingRight = 6;
            s.alignSelf = Align.FlexStart;
            s.display = DisplayStyle.None;
            View.Root.Add(m_Badge);
        }

        public override void OnCullingChanged(bool culled)
        {
            if (!culled && m_Badge != null && m_Badge.parent != View.Root) View.Root.Add(m_Badge);
        }

        public override void OnViewAttached()
        {
            AppFlowMachine.PhaseChanged += OnPhaseChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            // The window may have been hidden or opened after the state became Running: show it now.
            var machine = AppFlowRunner.Active != null ? AppFlowRunner.Active.Machine : null;
            if (machine != null && machine.CurrentState != null && machine.CurrentPhase == AppPhase.Running)
                OnPhaseChanged(machine, machine.CurrentState, AppPhase.Running);
        }

        public override void OnViewDetached()
        {
            AppFlowMachine.PhaseChanged -= OnPhaseChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        void OnPhaseChanged(AppFlowMachine machine, AppStateRef stateRef, AppPhase phase)
        {
            if (machine.Asset.GraphId != State.StateMachine.ID) return;
            if (stateRef.StateId == State.ID)
            {
                if (phase == AppPhase.None) Hide();
                else Show(phase);
            }
            else if (phase == AppPhase.Running)
            {
                Hide(); // another state took over
            }
        }

        void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode) Hide();
        }

        void Show(AppPhase phase)
        {
            m_Badge.text = phase.ToString().ToUpperInvariant();
            m_Badge.style.display = DisplayStyle.Flex;
        }

        void Hide() => m_Badge.style.display = DisplayStyle.None;
    }
}
