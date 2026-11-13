using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AppFlow.Sample
{
    static class UiAnim
    {
        public static StyleList<TimeValue> Time(float seconds) => new(new List<TimeValue> { new(seconds) });

        /// <summary>Runs a class toggle with a USS transition of <paramref name="seconds"/> (scaled by the Speed option) and waits for it.</summary>
        public static async Awaitable Play(VisualElement e, Action toggle, float seconds)
        {
            var scaled = seconds * SampleSettings.DurationScale;
            e.style.transitionDuration = Time(scaled);
            toggle();
            await Awaitable.WaitForSecondsAsync(scaled);
        }
    }
}
