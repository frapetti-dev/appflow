using UnityEngine;

namespace AppFlow.Sample
{
    /// <summary>Pause -> Game: only the source phases run. Game is not re-entered, so its scene is not reloaded; the machine then makes it Running.</summary>
    [CreateAssetMenu(menuName = "App Flow/Sample/Resume Transition")]
    public class ResumeTransition : AppTransitionBehaviour
    {
        public override async Awaitable Run(AppTransitionContext ctx)
        {
            await ctx.PreExit();
            await ctx.Exit();
            await ctx.PostExit();
        }
    }
}
