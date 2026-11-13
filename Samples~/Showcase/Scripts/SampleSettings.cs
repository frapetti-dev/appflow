using System;
using UnityEngine;

namespace AppFlow.Sample
{
    /// <summary>Persistent sample options (PlayerPrefs). Fullscreen is not stored here: Unity persists Screen.fullScreen itself.</summary>
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
    public static class SampleSettings
    {
        const string SpeedKey = "appflow.sample.speed";
        const string HudKey = "appflow.sample.hud";

        public static event Action Changed;

        /// <summary>0 slow, 1 normal, 2 fast.</summary>
        public static int Speed
        {
            get => PlayerPrefs.GetInt(SpeedKey, 1);
            set { PlayerPrefs.SetInt(SpeedKey, value); Changed?.Invoke(); }
        }

        public static bool Hud
        {
            get => PlayerPrefs.GetInt(HudKey, 1) != 0;
            set { PlayerPrefs.SetInt(HudKey, value ? 1 : 0); Changed?.Invoke(); }
        }

        public static float DurationScale => Speed switch { 0 => 1.75f, 2 => 0.5f, _ => 1f };
    }
}
