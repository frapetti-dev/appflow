using System;
using Unity.GraphToolkit.Editor;

namespace AppFlow.Editor
{
    [Serializable]
    [Condition("Custom Transition")]
    public sealed class CustomTransitionCondition : Condition<AppTransitionBehaviour>
    {
        public override string Tooltip => "Runs this transition behaviour instead of the default flow.";
    }
}
