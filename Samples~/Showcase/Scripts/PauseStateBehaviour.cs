using UnityEngine;

namespace AppFlow.Sample
{
    [CreateAssetMenu(menuName = "App Flow/Sample/Pause State")]
    public class PauseStateBehaviour : ScreenStateBehaviour
    {
        // Game stays loaded behind the pause card; leaving to anything but Game must unload it.
        public override async Awaitable OnPostExit(AppTransitionContext ctx)
        {
            if (ctx.ToRef != Back) await GameStateBehaviour.UnloadSceneOf(Machine, Back);
        }
    }
}
