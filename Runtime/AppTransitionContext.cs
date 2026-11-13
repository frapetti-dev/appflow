#pragma warning disable CS1998 // helpers are async so a null From is a cheap no-op
using UnityEngine;

namespace AppFlow
{
    /// <summary>Passed to every phase hook and custom transition. Step helpers run one phase of From or To.</summary>
    public sealed class AppTransitionContext
    {
        readonly AppFlowMachine.StateEntry m_From;
        readonly AppFlowMachine.StateEntry m_To;

        public AppStateBehaviour From => m_From?.Behaviour;
        public AppStateBehaviour To => m_To.Behaviour;
        public AppStateRef FromRef => m_From?.Ref;
        public AppStateRef ToRef => m_To.Ref;
        public bool Crossfade { get; }
        public AppFlowMachine Machine { get; }

        internal AppTransitionRecord Record { get; }

        internal AppTransitionContext(AppFlowMachine machine, AppFlowMachine.StateEntry from, AppFlowMachine.StateEntry to, bool crossfade, AppTransitionRecord record)
        {
            Machine = machine;
            m_From = from;
            m_To = to;
            Crossfade = crossfade;
            Record = record;
        }

        public async Awaitable PreExit() { if (m_From != null) await Machine.RunPhase(m_From, AppPhase.PreExit, this); }
        public async Awaitable Exit() { if (m_From != null) await Machine.RunPhase(m_From, AppPhase.Exit, this); }
        public async Awaitable PostExit() { if (m_From != null) await Machine.RunPhase(m_From, AppPhase.PostExit, this); }
        public async Awaitable PreEnter() { await Machine.RunPhase(m_To, AppPhase.PreEnter, this); }
        public async Awaitable Enter() { await Machine.RunPhase(m_To, AppPhase.Enter, this); }
        public async Awaitable PostEnter() { await Machine.RunPhase(m_To, AppPhase.PostEnter, this); }

        /// <summary>Default flow: A.PreExit, B.PreEnter, A.Exit, B.Enter, A.PostExit, B.PostEnter. Crossfade starts A.Exit and B.Enter together.</summary>
        public async Awaitable RunDefault(bool crossfade = false)
        {
            await PreExit();
            await PreEnter();
            if (crossfade)
            {
                var a = Exit();
                var b = Enter();
                await a;
                await b;
            }
            else
            {
                await Exit();
                await Enter();
            }
            await PostExit();
            await PostEnter();
        }
    }
}
