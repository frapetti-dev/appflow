using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AppFlow.Sample
{
    /// <summary>Base of the UI of one state scene: binds buttons to transitions and animates the screen in and out.</summary>
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
    public class ScreenView : MonoBehaviour
    {
        [Serializable]
        public struct Link
        {
            public string Button;
            public AppStateRef Target;
        }

        // Entries are removed in OnDisable.
        static readonly Dictionary<Hash128, ScreenView> s_Views = new();

        [SerializeField] UIDocument m_Document;
        [SerializeField] AppStateRef m_State;
        [SerializeField] List<Link> m_Links = new();

        List<VisualElement> m_Stagger;

        protected VisualElement Root { get; private set; }

        public static ScreenView For(AppStateRef state) => state != null && s_Views.TryGetValue(state.StateId, out var v) ? v : null;

        protected virtual void OnEnable()
        {
            s_Views[m_State.StateId] = this;
            Root = m_Document.rootVisualElement.Q(className: "screen");
            Root.AddToClassList("off-pop"); // hidden until the state's Enter phase shows it
            m_Stagger = Root.Query(className: "stagger").ToList();
            foreach (var s in m_Stagger) s.AddToClassList("stagger--off");

            foreach (var link in m_Links)
            {
                var target = link.Target;
                Root.Q<Button>(link.Button).clicked += () => Go(target);
            }
            var quit = Root.Q<Button>("quit");
            if (quit != null) quit.clicked += Quit;
        }

        protected virtual void OnDisable()
        {
            if (m_State != null && s_Views.TryGetValue(m_State.StateId, out var v) && v == this) s_Views.Remove(m_State.StateId);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // Ignores clicks that the machine would reject (double clicks, clicks during a transition).
        void Go(AppStateRef target)
        {
            var runner = AppFlowRunner.Active;
            if (runner == null || runner.Machine.IsTransitioning || runner.Machine.CurrentState != m_State) return;
            _ = runner.Machine.TransitTo(target);
        }

        public virtual async Awaitable Show(string offClass)
        {
            Root.style.transitionDuration = UiAnim.Time(0);
            Root.RemoveFromClassList("off-pop");
            Root.AddToClassList(offClass);
            await Awaitable.NextFrameAsync(); // a transition needs a resolved previous style

            var scale = SampleSettings.DurationScale;
            for (var i = 0; i < m_Stagger.Count; i++)
            {
                var item = m_Stagger[i];
                item.style.transitionDuration = UiAnim.Time(0.4f * scale);
                item.style.transitionDelay = UiAnim.Time(i * 0.07f * scale);
                item.RemoveFromClassList("stagger--off");
            }
            await UiAnim.Play(Root, () => Root.RemoveFromClassList(offClass), 0.45f);
            await Awaitable.WaitForSecondsAsync(m_Stagger.Count * 0.07f * scale);
            foreach (var item in m_Stagger)
            {
                item.style.transitionDuration = StyleKeyword.Null; // back to the USS values (hover feedback)
                item.style.transitionDelay = StyleKeyword.Null;
            }
        }

        public virtual Awaitable Hide(string offClass) => UiAnim.Play(Root, () => Root.AddToClassList(offClass), 0.35f);

        public virtual void OnRunning() { }

        public virtual void Tick(float dt) { }
    }
}
