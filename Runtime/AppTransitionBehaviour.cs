using UnityEngine;

namespace AppFlow
{
    /// <summary>Custom code for one graph edge A->B. Receives both states and drives the whole phase sequence.</summary>
    public abstract class AppTransitionBehaviour : ScriptableObject
    {
        public abstract Awaitable Run(AppTransitionContext ctx);
    }
}
