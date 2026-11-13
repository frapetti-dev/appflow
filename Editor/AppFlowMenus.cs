using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace AppFlow.Editor
{
    static class AppFlowMenus
    {
        [StateMachineMenu(typeof(AppFlowStateMachine))]
        static void Menu(StateMachineMenuContext ctx)
        {
            var machine = AppFlowRunner.Active != null ? AppFlowRunner.Active.Machine : null;
            if (ctx.ClickedObject is IState s && EditorApplication.isPlaying && machine != null && machine.Asset.GraphId == ctx.StateMachine.ID)
                ctx.AppendAction("App Flow/Force enter", () => _ = machine.TransitTo(s.ID));
        }
    }
}
