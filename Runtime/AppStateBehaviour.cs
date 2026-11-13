#pragma warning disable CS1998 // virtual hooks are async no-ops so overrides can await
using UnityEngine;

namespace AppFlow
{
    /// <summary>Logic of one state. Non-abstract: a state without a behaviour asset gets a plain instance (no-op hooks).</summary>
    public class AppStateBehaviour : ScriptableObject
    {
        public AppFlowMachine Machine { get; internal set; }
        public AppStateRef State { get; internal set; }

        public virtual async Awaitable OnPreEnter(AppTransitionContext ctx) { }
        public virtual async Awaitable OnEnter(AppTransitionContext ctx) { }
        public virtual async Awaitable OnPostEnter(AppTransitionContext ctx) { }
        public virtual async Awaitable OnPreExit(AppTransitionContext ctx) { }
        public virtual async Awaitable OnExit(AppTransitionContext ctx) { }
        public virtual async Awaitable OnPostExit(AppTransitionContext ctx) { }

        /// <summary>Called once when the state becomes Running.</summary>
        public virtual void OnRunning() { }

        /// <summary>Called every frame while Running.</summary>
        public virtual void OnRunningUpdate() { }
    }
}
