using UnityEngine;
using UnityEngine.UIElements;

namespace AppFlow.Sample
{
    public class SettingsView : ScreenView
    {
        Button[] m_Speed;
        Button m_HudOn, m_HudOff, m_FullOn, m_FullOff;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Speed = new[] { Root.Q<Button>("speed-slow"), Root.Q<Button>("speed-normal"), Root.Q<Button>("speed-fast") };
            for (var i = 0; i < m_Speed.Length; i++)
            {
                var speed = i;
                m_Speed[i].clicked += () => SampleSettings.Speed = speed;
            }
            m_HudOn = Root.Q<Button>("hud-on");
            m_HudOff = Root.Q<Button>("hud-off");
            m_FullOn = Root.Q<Button>("fullscreen-on");
            m_FullOff = Root.Q<Button>("fullscreen-off");
            m_HudOn.clicked += () => SampleSettings.Hud = true;
            m_HudOff.clicked += () => SampleSettings.Hud = false;
            m_FullOn.clicked += () => { Screen.fullScreen = true; Refresh(); };
            m_FullOff.clicked += () => { Screen.fullScreen = false; Refresh(); };
            SampleSettings.Changed += Refresh;
            Refresh();
        }

        protected override void OnDisable()
        {
            SampleSettings.Changed -= Refresh;
            base.OnDisable();
        }

        void Refresh()
        {
            for (var i = 0; i < m_Speed.Length; i++) m_Speed[i].EnableInClassList("pill--on", SampleSettings.Speed == i);
            m_HudOn.EnableInClassList("pill--on", SampleSettings.Hud);
            m_HudOff.EnableInClassList("pill--on", !SampleSettings.Hud);
            m_FullOn.EnableInClassList("pill--on", Screen.fullScreen);
            m_FullOff.EnableInClassList("pill--on", !Screen.fullScreen);
        }
    }
}
