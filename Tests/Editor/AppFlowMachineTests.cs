using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AppFlow.Tests
{
    public class AppFlowMachineTests
    {
        static readonly Hash128 IdA = Hash128.Compute("A");
        static readonly Hash128 IdB = Hash128.Compute("B");

        readonly List<Object> m_Created = new();
        AppFlowMachine m_Machine;

        [SetUp]
        public void SetUp()
        {
            RecordingBehaviour.Log = new List<string>();
            RecordingBehaviour.ExitGate = null;
            RecordingBehaviour.ThrowInEnterOf = null;
            RecordingTransition.LastFrom = RecordingTransition.LastTo = null;
        }

        [TearDown]
        public void TearDown()
        {
            m_Machine?.Dispose();
            foreach (var o in m_Created) Object.DestroyImmediate(o);
            m_Created.Clear();
        }

        AppFlowAsset MakeAsset(AppTransitionData edge = null, params string[] extraNames)
        {
            var asset = ScriptableObject.CreateInstance<AppFlowAsset>();
            m_Created.Add(asset);
            asset.name = "TestFlow";
            asset.EntryStateId = IdA;
            AddState(asset, IdA, "A");
            AddState(asset, IdB, "B");
            foreach (var n in extraNames) AddState(asset, Hash128.Compute(n + asset.States.Count), n);
            if (edge != null) asset.Transitions.Add(edge);
            return asset;
        }

        void AddState(AppFlowAsset asset, Hash128 id, string name)
        {
            var behaviour = ScriptableObject.CreateInstance<RecordingBehaviour>();
            m_Created.Add(behaviour);
            asset.States.Add(new AppStateData { Id = id, Name = name, Behaviour = behaviour });
        }

        AppFlowMachine Machine(AppFlowAsset asset) => m_Machine = new AppFlowMachine(asset);

        [Test]
        public async Task InitialEntry_RunsEnterPhasesThenRunning()
        {
            var m = Machine(MakeAsset());
            Assert.IsTrue(await m.Start());
            CollectionAssert.AreEqual(new[] { "A.PreEnter", "A.Enter", "A.PostEnter", "A.Running" }, RecordingBehaviour.Log);
        }

        [Test]
        public async Task DefaultFlow_IsInterleavedSequential()
        {
            var m = Machine(MakeAsset());
            await m.Start();
            RecordingBehaviour.Log.Clear();
            Assert.IsTrue(await m.TransitTo(IdB));
            CollectionAssert.AreEqual(
                new[] { "A.PreExit", "B.PreEnter", "A.Exit", "B.Enter", "A.PostExit", "B.PostEnter", "B.Running" },
                RecordingBehaviour.Log);
        }

        [Test]
        public async Task CrossfadeEdge_StartsExitAndEnterTogether()
        {
            var m = Machine(MakeAsset(new AppTransitionData { From = IdA, To = IdB, Crossfade = true }));
            await m.Start();
            RecordingBehaviour.Log.Clear();
            RecordingBehaviour.ExitGate = new AwaitableCompletionSource();

            var transition = m.TransitTo(IdB);
            Assert.Contains("B.Enter", RecordingBehaviour.Log, "B.Enter must start while A.Exit is still pending");
            Assert.IsTrue(m.IsTransitioning);

            RecordingBehaviour.ExitGate.SetResult();
            Assert.IsTrue(await transition);
            Assert.AreEqual("B.Running", RecordingBehaviour.Log[^1]);
        }

        [Test]
        public async Task CustomTransition_ReplacesDefaultFlow()
        {
            var custom = ScriptableObject.CreateInstance<RecordingTransition>();
            m_Created.Add(custom);
            var m = Machine(MakeAsset(new AppTransitionData { From = IdA, To = IdB, Behaviour = custom }));
            await m.Start();
            RecordingBehaviour.Log.Clear();

            Assert.IsTrue(await m.TransitTo(IdB));

            CollectionAssert.AreEqual(new[] { "custom A->B", "B.Enter", "B.Running" }, RecordingBehaviour.Log);
            Assert.AreEqual("A", RecordingTransition.LastFrom.State.name);
            Assert.AreEqual("B", RecordingTransition.LastTo.State.name);
            Assert.AreSame(RecordingTransition.LastTo, m.CurrentBehaviour);
            Assert.IsTrue(m.History[^1].Custom);
        }

        [Test]
        public async Task TransitWhileBusy_ReturnsFalse()
        {
            var m = Machine(MakeAsset());
            await m.Start();
            RecordingBehaviour.ExitGate = new AwaitableCompletionSource();
            var first = m.TransitTo(IdB);

            LogAssert.Expect(LogType.Warning, new Regex("rejected"));
            Assert.IsFalse(await m.TransitTo(IdA));

            RecordingBehaviour.ExitGate.SetResult();
            Assert.IsTrue(await first);
        }

        [Test]
        public async Task TransitToCurrent_ReEnters()
        {
            var m = Machine(MakeAsset());
            await m.Start();
            RecordingBehaviour.Log.Clear();

            Assert.IsTrue(await m.TransitTo(IdA));

            CollectionAssert.AreEqual(
                new[] { "A.PreExit", "A.PreEnter", "A.Exit", "A.Enter", "A.PostExit", "A.PostEnter", "A.Running" },
                RecordingBehaviour.Log);
        }

        [Test]
        public async Task HookException_ContinuesToTarget()
        {
            var m = Machine(MakeAsset());
            await m.Start();
            RecordingBehaviour.ThrowInEnterOf = "B";
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            Assert.IsTrue(await m.TransitTo(IdB));

            Assert.AreEqual("B", m.CurrentState.name);
            Assert.AreEqual(AppPhase.Running, m.CurrentPhase);
            Assert.AreEqual(1, m.History[^1].Exceptions);
        }

        [Test]
        public void TryFindState_ByNameIdAndDuplicate()
        {
            var asset = MakeAsset(null, "dup", "DUP");
            var m = Machine(asset);

            Assert.IsTrue(m.TryFindState("b", out var s, out _));
            Assert.AreEqual(IdB, s.Id);

            Assert.IsTrue(m.TryFindState(IdA.ToString(), out s, out _));
            Assert.AreEqual("A", s.Name);

            Assert.IsFalse(m.TryFindState("dup", out s, out var error));
            Assert.IsNull(s);
            StringAssert.Contains(asset.States[2].Id.ToString(), error);
            StringAssert.Contains(asset.States[3].Id.ToString(), error);

            Assert.IsFalse(m.TryFindState("Nope", out _, out error));
            StringAssert.Contains("Nope", error);
        }
    }
}
