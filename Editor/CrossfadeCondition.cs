using System;
using Unity.GraphToolkit.Editor;

namespace AppFlow.Editor
{
    [Serializable]
    [Condition("Crossfade")]
    public sealed class CrossfadeCondition : Condition
    {
        public override string Tooltip => "Starts the source Exit and the target Enter together.";
    }
}
