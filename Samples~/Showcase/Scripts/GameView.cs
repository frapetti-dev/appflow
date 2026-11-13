using UnityEngine;
using UnityEngine.UIElements;

namespace AppFlow.Sample
{
    /// <summary>Tiny endless runner scene. Tick only runs while the Game state is Running, so Pause freezes it without any pause logic.</summary>
    public class GameView : ScreenView
    {
        const float CoinEvery = 1.2f;

        VisualElement m_Clouds, m_Hero;
        Label m_Score;
        float m_Time, m_CoinTimer, m_Score01;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Clouds = Root.Q("clouds");
            m_Hero = Root.Q("hero");
            m_Score = Root.Q<Label>("score");
        }

        public void SetFrozen(bool frozen) => Root.EnableInClassList("game--frozen", frozen);

        public override void Tick(float dt)
        {
            m_Time += dt;
            m_Score01 += 5 * dt;

            var half = m_Clouds.resolvedStyle.width / 2;
            if (half > 0) m_Clouds.style.translate = new Translate(-((m_Time * 80) % half), 0);
            m_Hero.style.translate = new Translate(0, -Mathf.Abs(Mathf.Sin(m_Time * 4)) * 40);

            m_CoinTimer += dt;
            if (m_CoinTimer >= CoinEvery)
            {
                m_CoinTimer = 0;
                m_Score01 += 10;
                SpawnCoin();
            }
            m_Score.text = $"Score {(int)m_Score01}";
        }

        void SpawnCoin()
        {
            var coin = new VisualElement();
            coin.AddToClassList("coin");
            coin.style.left = Length.Percent(50);
            coin.style.marginLeft = -32;
            coin.style.bottom = 440;
            Root.Add(coin);
            coin.schedule.Execute(() => coin.AddToClassList("coin--pop")).ExecuteLater(30);
            coin.schedule.Execute(() => coin.RemoveFromHierarchy()).ExecuteLater(750);
        }
    }
}
