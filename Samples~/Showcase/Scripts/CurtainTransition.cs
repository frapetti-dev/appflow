using UnityEngine;

namespace AppFlow.Sample
{
    /// <summary>Custom edge: a curtain drops, the source leaves, the target loads behind a loading bar, the curtain lifts.</summary>
    [CreateAssetMenu(menuName = "App Flow/Sample/Curtain Transition")]
    public class CurtainTransition : AppTransitionBehaviour
    {
        [SerializeField] string m_Label = "Loading...";
        [SerializeField] float m_MinSeconds = 1f;

        public override async Awaitable Run(AppTransitionContext ctx)
        {
            var overlay = OverlayView.Instance;
            if (overlay == null)
            {
                await ctx.RunDefault();
                return;
            }
            await ctx.PreExit();
            await overlay.Close(m_Label);
            await ctx.Exit();
            await ctx.PostExit();
            await ctx.PreEnter();
            var enter = ctx.Enter();
            var bar = overlay.FillBar(m_MinSeconds);
            await enter;
            await bar;
            await overlay.Open();
            await ctx.PostEnter();
        }
    }
}
