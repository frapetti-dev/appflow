# Unity Graph Toolkit (GTK) for a visual application state machine

Project: `C:/Users/f.rapetti/State machine`, originally Unity **6000.6.6f1**, being upgraded to **6000.7.0b9** (installed at `C:/Program Files/Unity/Hub/Editor/6000.7.0b9`), URP 17.6.
Research date: 2026-11-13. Verdict labels used throughout:

- **SUPPORTED**: public, documented API on the stated version.
- **WORKAROUND**: built only from public API, but not designed for this use, or partly unverified.
- **NOT POSSIBLE**: no public API on that version. Internals are not an option because they are `internal` and change between versions.
- **[UNVERIFIED]**: not confirmed by a primary source or by testing in an Editor.

---

## 0. Update: target is now 6000.7 (verified against the installed 6000.7.0b9 module)

Checked against `C:/Program Files/Unity/Hub/Editor/6000.7.0b9/Editor/Data/Managed/UnityEngine/UnityEditor.GraphToolkitModule.xml` (73 documented public types). Everything §4.2, §5.4 and §6.2 attribute to "6.7+" exists in 6000.7.0b9, so the 6.6 workarounds below are no longer needed.

| Need | 6000.7.0b9 API | Verdict |
|---|---|---|
| Author the state machine | `StateMachine` + `[StateMachine(ext, StateMachineOptions)]`, `State` + `[State(...)]` (`OnDefineOptions`, `GetIncomingTransitions`/`GetOutgoingTransitions`), `[Transition(...)]`, `SelfTransition`, `Condition` / `Condition<T>` (`Value`, `Comparison`), `Condition.CreateGroupCondition` (And/Or), `Condition.CreateVariableCondition`, `StateMachine.OnStateMachineChanged(StateMachineLogger)` for validation | **SUPPORTED** |
| Import to a runtime asset | `StateMachineDatabase.LoadStateMachineForImporter<T>(path)` in a `ScriptedImporter` | **SUPPORTED** |
| Mark the active state in Play mode | `Registry.GetActiveContext(id)` → `Context.GetStateReference(stateId)` → `Context.Motion.Play(StateReference, speed)` / `StateReference.FillAmount`; taken transition via `GetTransitionReference(id)` → `LineColor`, `FillColor`, `Icon`, `IsDashed`, `WidthOverride`, `Opacity`; condition results via `ConditionReference.Icon` + `DebugStyles.True/False/Pending` | **SUPPORTED** |
| Stronger marker (badge, outline, "ACTIVE" label) | `StateView<T>`: `OnViewBuilt` / `OnViewAttached`, add UI to `View.Root` (a `VisualElement`; docs: *"Add custom UI to IStateView.Root"*) and toggle it from the runner's state-change event | **SUPPORTED** (the toggle wiring is your code) |
| Force a state from a node | `[StateMachineMenu(typeof(MyStateMachine))] static void M(StateMachineMenuContext c)`: `c.ClickedObject` (state under the cursor, null on empty space) + `c.AppendAction("Force enter", () => runner.ForceState(id))` | **SUPPORTED** |
| Force button inside the node | `StateView<T>` adding a `Button` to `View.Root` | **SUPPORTED** |
| Toolbar debug toggle / state dropdown | `[GraphToolbarElement(id, type, order)]` + `IGraphWindow.StateMachine` | **SUPPORTED** |

Still open on 6000.7: subgraph visualization bug IN-155051 (§4.1); the API is beta and changed between 6.6 and 6.7, so expect changes again before 6000.7 final. The XML doc above does not show which property setters are public; settable `FillAmount`, `LineColor` etc. were taken from the 6.7 sample code ([s-gsm-code]) and are confirmed to compile on b9 (`TransitionReference.LineColor`/`WidthOverride`, `State.Title`, `State.Position`, `AddOption<UnityEngine.Object-derived>`, `Condition<ScriptableObject-derived>`).

---

## 1. Summary

**Q4: Can GTK mark the active node in the graph window during Play mode?**

| Capability on **6000.6** (this project) | Verdict | Evidence |
|---|---|---|
| Animate the accent bar of the active node (`Context.Motion.Play(NodeReference)`), or show a progress fill (`NodeReference.FillAmount`) | **SUPPORTED** | [Display progress bars on nodes][m-progress], [GraphMotion XML doc][xml] |
| Animate, thicken, dash or fade the wire of the transition just taken (`WireReference`, `Motion.Play(WireReference)`) | **SUPPORTED** | [Customize wire appearance][m-debug], [6.6 alpha post][d-66] |
| Show a text label next to a port (`PortReference.SetPreview`), for example "time in state" or "last trigger" | **SUPPORTED** | [6.6 alpha post][d-66], [VN Director debug view][s-vn] |
| Colour the active node's accent, or put "● ACTIVE" in its subtitle, by setting `Node.DefaultColor` / `Node.Subtitle` at runtime on the open graph | **WORKAROUND** | Both setters are public. In source they write `[NonSerialized]` fields inside `BlockAssetDirtyScope()`, so the asset is not dirtied ([UserNodeModelImp.gen.cs][src-usernode]). Whether the canvas repaints at once when set from outside a GTK command is **[UNVERIFIED]**. |
| Full highlight: outline, background colour, badge, extra UI inside the node | **NOT POSSIBLE** on 6.6 | `NodeReference` exposes only `FillAmount` and `ClearCustomization` ([XML][xml]). `NodeView<T>` arrives in 6.7 ([6.7 beta post][d-67]). A Unity user asked for an "active" outline and got no reply ([6.6 thread, post 20][d-66]). |
| Select or frame (focus) a node from code | **NOT POSSIBLE** on 6.6 | The public window API is only `IGraphWindow.Graph` ([IGraphWindow.cs][src-igw]). |
| State-machine–aware debug view: `StateReference`, `TransitionReference` (line/fill colours), `ConditionReference`, `DebugStyles` true/false/pending icons | **SUPPORTED on 6000.7+ only** | [Game State Machine sample][s-gsm-code]. These types are present in the locally installed 7000.0.0a16 module (metadata scan, §5.4). |

Short answer: **yes, through the GraphVisualization API, but on 6.6 the marker is the animated accent bar on the active node plus wire styling.** Unity users have called it subtle ([6.6 thread, posts 14 and 20][d-66]). A coloured accent through `Node.DefaultColor` is a reasonable add-on WORKAROUND. A full outline or badge needs 6.7 (`NodeView<T>` / `StateView<T>`).

**Q5: Can the graph window force a state change in the running game?**

| Extension point on **6000.6** | Verdict | Evidence |
|---|---|---|
| Graph window toolbar element (`[GraphToolbarElement]`; any `VisualElement`, including a dropdown) that reads `IGraphWindow.Graph`, lists the state nodes and calls the runtime | **SUPPORTED** (as an extension point; the runtime call is your code) | [Add custom toolbar actions][m-toolbar]; Unity staff dropdown example ([6.6 thread, post 15][d-66]); VN sample toolbar shown only in Play mode ([s-vn]) |
| Right-click context menu entry on a node ("Force enter") | **NOT POSSIBLE** on 6.6; **SUPPORTED** on 6.7 (`[GraphMenu]`, `[StateMachineMenu]`) | No menu attribute in 6.6 public types (§5.4). Staff: "in our backlog… top priorities" ([6.6 thread, post 5][d-66]); shipped in 6.7 ([6.7 beta post][d-67]) |
| Button drawn inside the node | **NOT POSSIBLE** on 6.6; **SUPPORTED** on 6.7 (`NodeView<T>` / `StateView<T>`) | [6.7 beta post][d-67] |
| "Force the **selected** node" from the toolbar | **NOT POSSIBLE** on 6.6 | No public selection API. Selecting a node does not set `UnityEditor.Selection`; only a double-click on the background selects the graph object ([GraphView.cs L2990][src-graphview]) |
| `GraphLogAction` button on a marker (from `OnGraphChanged`) used as a "Force" button | **WORKAROUND, not recommended** | Markers are created only inside `OnGraphChanged`, which runs on edits, not on Play-mode state changes ([graph processing][m-processing], [6.6 thread][d-66]) |
| Custom Inspector or EditorWindow on the runner `MonoBehaviour` (outside the graph window) | **SUPPORTED** (standard UnityEditor API) | n/a |

Short answer: **on 6.6, use a toolbar dropdown "Force State ▾" in the graph window that calls `runner.ForceState(nodeId)`.** Per-node right-click entries and buttons need 6.7.

---

## 2. Installation & status

- **Nothing to install.** GTK is an Editor module from 6000.4. The legacy package `com.unity.graphtoolkit` is now a shim: its bundled `package.json` reads `"version": "0.5.0-exp.1", "type": "shim"`, *"This package is no longer supported. Graph Toolkit is a module as of Unity 6000.4 and available in all projects."* (local file `C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.graphtoolkit/package.json`; its CHANGELOG entry 0.5.0-exp.1 says the same). Do **not** add it to `Packages/manifest.json`.
- Module binary and API docs: `C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Data/Managed/UnityEngine/UnityEditor.GraphToolkitModule.dll` and `.xml`. Namespaces: `Unity.GraphToolkit.Editor` and `Unity.GraphToolkit.Editor.GraphVisualization`. Assembly reported by the docs: `UnityEditor` ([Registry reference][r-registry]).
- Editor-only. The manual says: *"Although Graph Toolkit supports compilation to runtime models, it doesn't include runtime execution backends."* ([GTK manual index][m-index]).
- No experimental flag or define is needed. The 6.6 manual pages carry no experimental banner. The **samples** package `com.unity.graphtoolkit-samples` (0.6.7-exp.2) is marked experimental ([samples index][s-index]).
- `Library/PackageCache` contains no GTK package source. It does contain `com.unity.graph-authoring@1.0.0` (*"Shared graph tooling for Shader Graph and Visual Effect Graph"*), whose asmdef references `Unity.GraphToolkit.Editor`. That is a Shader Graph dependency, not something to build on.
- The C# reference source for the module is public, at `Modules/GraphToolkitEditor` on the `6000.6` branch of [UnityCsReference][src-root]. It was used below to check what is public and what is internal.
- **The API has changed between versions.** The 6.6 alpha changed untyped ports from `DataType == null` to `typeof(Untyped)` ([6.6 alpha post][d-66]). The State Machine API, `NodeView<T>` and context menus arrived in **6000.7** (beta as of Sept–Oct 2026; Unity says the state machine sample "ideally" needs 6000.7.0b4) ([6.7 beta post, posts 1 and 17][d-67]). The docs site lists **7000.0** as the latest version ([GTK index 7000.0][m-index-7]).

---

## 3. Core API (6000.6, names checked against the module XML docs and reflection metadata)

Public types in 6.6 (complete list from the module metadata): `Graph`, `GraphAttribute`, `GraphOptions` (`SupportsSubgraphs`, `DisableAutoInclusionOfNodesFromGraphAssembly`), `GraphDatabase`, `GraphLogger`, `GraphLogAction`, `Node`, `NodeAttribute`, `ContextNode`, `BlockNode`, `UseWithGraphAttribute`, `UseWithContextAttribute`, `SubgraphAttribute`, `INode`, `IPort`, `INodeOption`, `IVariable`, `IVariableNode`, `IConstantNode`, `ISubgraphNode`, port/option builder interfaces, `PortCapacity`, `PortConnectorUI`, `PortDirection`, `VariableKind`, `Wire`, `Untyped`, `DataTypeStyleMapper`, `GraphToolbarElementAttribute`, `IGraphWindow`, and `GraphVisualization.{Registry, Context, GraphMotion, NodeReference, PortReference, WireReference}` ([XML][xml]).

### 3.1 Graph, nodes, ports, options

```csharp
// Editor assembly (Editor folder or Editor-only asmdef)
using System; using System.Linq; using UnityEditor; using UnityEngine;
using Unity.GraphToolkit.Editor;

[Graph(AssetExtension, GraphOptions.SupportsSubgraphs)]
[Serializable]
class AppFlowGraph : Graph
{
    public const string AssetExtension = "appflow";

    [MenuItem("Assets/Create/App Flow/App Flow Graph")]
    static void Create() => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<AppFlowGraph>();

    // Validation only: mutating the graph here is forbidden (manual note + staff post)
    public override void OnGraphChanged(GraphLogger logger)
    {
        var entries = GetNodes().OfType<EntryNode>().ToList();
        if (entries.Count != 1) logger.LogError("Exactly one Entry node is required.");
        foreach (var t in GetNodes().OfType<TransitionNode>())
            if (!t.GetInputPortByName("From").IsConnected || !t.GetOutputPortByName("To").IsConnected)
                logger.LogWarning("Dangling transition.", t);   // context = node, so the marker appears on it
    }
}

[Serializable] abstract class StateNode : Node          // abstract: hidden from the Add menu
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("In").WithCapacity(PortCapacity.Multi).Build();      // untyped = flow
        context.AddOutputPort("Out").WithConnectorUI(PortConnectorUI.Arrowhead).Build();
    }
}

[Serializable, Node("States", null, "Gameplay")]
class GameplayStateNode : StateNode
{
    protected override void OnDefineOptions(IOptionDefinitionContext context)
        => context.AddOption<string>("Scene").WithDefaultValue("Gameplay").Delayed();
}
```

- Ports: `AddInputPort[<T>](name)` / `AddOutputPort[<T>](name)`, then `.WithDisplayName`, `.WithTooltip`, `.WithCapacity(PortCapacity.None|Single|Multi)`, `.WithConnectorUI(Circle|Arrowhead)`, `.AsVertical()`, `.WithDefaultValue`, `.Delayed()`, `.AsTextArea()`, `.WithDataType()`. Every chain ends with `.Build()` ([XML][xml]; capacity and type-cast added in 6.6 ([6.6 alpha post][d-66])).
- Connection rules: ports of different types refuse to connect by default. Override `Graph.IsConnectionAllowed(IPort output, IPort input)` to allow more ([6.6 alpha post][d-66]; [type-cast page in TOC][m-impl]).
- Options: `OnDefineOptions(IOptionDefinitionContext)` → `AddOption<T>(name)` with `.WithDisplayName/.WithTooltip/.WithDefaultValue/.Delayed/.AsTextArea/.ShowInInspectorOnly/.Build`. Read them with `GetNodeOptionByName(name).TryGetValue<T>(out v)` ([XML][xml]).
- Node look: `NodeAttribute(categoryPath, iconPath, title, stylesheet)`. The 4th argument is a USS file applied to the node's `VisualElement`; it is fixed per node type ([XML][xml], [6.6 alpha post][d-66]). Runtime-settable look properties: `Node.Title`, `Subtitle`, `Tooltip`, `DefaultColor` ("the highlight color of the node… upper border") and `FillAmount` ([XML][xml]). `INode.FillAmount` throws `NotSupportedException`; use `Node.FillAmount` or `NodeReference.FillAmount` ([visualization concepts][m-viz]).
- Context and block nodes: `ContextNode` contains `BlockNode`s (`[UseWithContext(typeof(...))]`) ([node intro][m-nodes]).
- Variables: `Graph.CreateVariable(...)`, `VariableKind.Local|Input|Output`. Subgraphs: `GraphOptions.SupportsSubgraphs` ([subgraph page][m-subgraph]). A subgraph node's ports mirror the subgraph's Input/Output variables (skill reference `graph-api.md` §6; **[UNVERIFIED]** in the Unity manual).
- Change callback: `Graph.OnGraphChanged(GraphLogger)` is *"Called after the graph has changed"* ([XML][xml]; [graph processing][m-processing]). On 6.6 it carries no change delta. `GraphLogger.GraphChanges` is 6.7+ ([6.7 beta post][d-67]).
- IDs: `Graph.ID`, `INode.ID`, `IPort.ID` (all `Hash128`), added in 6.6 *"to identify these graph elements when debugging your graph in the runtime"* ([6.6 alpha post][d-66]).

### 3.2 ScriptedImporter: authoring graph → runtime asset

GTK never runs at Play time. The official pattern, used by every runtime sample, is a `ScriptedImporter` registered on the graph's extension that writes a plain runtime `ScriptableObject` ([VN Director][s-vn], [Game State Machine][s-gsm-code], [ScriptedImporter manual][m-importer]). Inside importers, use `GraphDatabase.LoadGraphForImporter<T>`, which *"always returns a clean copy of the graph as it exists on disk"* ([LoadGraphForImporter][r-lgfi]).

```csharp
using UnityEditor.AssetImporters;

[ScriptedImporter(1, AppFlowGraph.AssetExtension)]   // bump version when the runtime format changes
class AppFlowImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var graph = GraphDatabase.LoadGraphForImporter<AppFlowGraph>(ctx.assetPath);
        if (graph == null) { ctx.LogImportError($"Cannot load {ctx.assetPath}"); return; }

        var asset = ScriptableObject.CreateInstance<AppFlowAsset>();   // runtime type, runtime asmdef
        asset.GraphId = graph.ID;                                      // needed for the debug view
        foreach (var s in graph.GetNodes().OfType<StateNode>())
            asset.States.Add(new AppStateData { Id = s.ID, Name = s.Title, Behaviour = s.CreateRuntime() });

        foreach (var t in graph.GetNodes().OfType<TransitionNode>())
        {
            IPort fromIn = t.GetInputPortByName("From"), toOut = t.GetOutputPortByName("To");
            var src = fromIn.FirstConnectedPort; var dst = toOut.FirstConnectedPort;
            if (src == null || dst == null) continue;
            t.GetNodeOptionByName("Trigger").TryGetValue<AppTrigger>(out var trigger);
            asset.Transitions.Add(new AppTransitionData {
                NodeId = t.ID, From = src.GetNode().ID, To = dst.GetNode().ID, Trigger = trigger,
                WireInOut = src.ID, WireInIn = fromIn.ID, WireOutOut = toOut.ID, WireOutIn = dst.ID });
        }
        var entry = graph.GetNodes().OfType<EntryNode>().FirstOrDefault();
        asset.EntryId = entry?.GetOutputPortByName("Out").FirstConnectedPort?.GetNode().ID ?? default;

        ctx.AddObjectToAsset("Runtime", asset);
        ctx.SetMainObject(asset);            // the .appflow file now behaves as an AppFlowAsset
    }
}
```

The runtime asset and its asmdef must not reference GTK types, except visualization code inside `#if UNITY_EDITOR`, as the samples do ([s-vn], [s-gsm-code]).

---

## 4. State machine design (for Boot → MainMenu → Loading → Gameplay → Pause)

### 4.1 On 6000.6: model it with the Graph API

The dedicated State Machine API does not exist on 6.6 (none of its types are in the 6.6 public metadata, §5.4). Mapping onto the Graph API:

| Concept | Representation | Notes |
|---|---|---|
| State | one concrete `Node` subclass per state kind (`BootStateNode`, `MainMenuStateNode`, …) deriving from abstract `StateNode` | Per-state settings are node options (scene name, fade time, …). `abstract` hides the base from the Add menu. |
| Entry state | `EntryNode` with one untyped output `.WithCapacity(PortCapacity.Single)`, wired to the initial state | Enforce "exactly one" in `OnGraphChanged`. GTK has no built-in entry concept; even the 6.7 sample uses a marker "Entry" self transition ([GSM intro][s-gsm-intro]). |
| Transition + trigger | **Option A (recommended):** a small `TransitionNode` (`From` in, `To` out, option `Trigger : AppTrigger` enum, optional `Condition`) between two states. **Option B:** a direct wire `State.Out → State.In`, trigger stored as an option on the source state (one output port per trigger, ports built from options in `OnDefinePorts`) | A gives each transition a selectable element with its own options, its own debug wire pair and its own accent. B is more compact, but a plain wire has no per-wire options in 6.6 (`Wire` exposes only style properties ([XML][xml])). |
| Conditions | options on `TransitionNode` (enum trigger, float timeout, bool flag), evaluated by your runtime | No condition framework on 6.6. |
| Sub-state machine | `GraphOptions.SupportsSubgraphs`, with a subgraph whose Input/Output variables provide the flow ports | Visualization with subgraphs throws `ArgumentException` on 6.6 and 6.7 (bug IN-155051, staff confirmed a fix is coming) ([6.7 thread, posts 8–9][d-67]). The context must use the **root** graph ID for nested nodes ([Registry XML][xml]). |
| Global transitions (for example "any → Pause") | an `AnyStateNode` with outputs, resolved by the importer | Design choice, not a GTK feature. |

### 4.2 On 6000.7+: the dedicated State Machine API

`StateMachine` / `State` / `ITransition` / `ITransitionRule` / `Condition<T>` / `SelfTransition`, built-in And/Or groups and variable conditions, `[StateMachineMenu]`, `StateView<T>`, plus state-aware visualization ([6.7 beta post][d-67], [Implementing a state machine (7000.0)][m-sm-7], [StateMachine reference 6000.7][r-sm-67]). Unity's guidance names *"game flow"* as a typical state-machine use ([Graphs vs state machines][m-gvs-7]).

The official **Game State Machine** sample covers almost exactly this project: Splash/Ready/Countdown/Main/Pause/GameOver states, an Entry marker self transition, `GameSignal` and `TimeInState` conditions, an importer to a `GameFlowRuntimeStateMachine`, a `GameFlowRunner`, a `GameFlowDebugVisualizer` and a `DebugViewToggle` toolbar element ([GSM intro][s-gsm-intro], [GSM code][s-gsm-code]). It needs 6000.7.0b4+ ([6.7 thread, post 17][d-67]).

---

## 5. Play-mode visualization

### 5.1 Supported API (6.6)

From [Introduction to graph visualization][m-viz] and [Display progress bars on nodes][m-progress]:

- `Registry.CreateVisualizationContext(graph.ID)` returns a `Context` (`IDisposable`). Feedback *"doesn't save to the graph asset"*. A context can be created before the window opens; the values apply when the canvas appears.
- **Contexts are released when entering or exiting Play mode.** Create them after the mode change ([m-viz]). In source, `RegistryService` calls `UnregisterAll()` on `ExitingEditMode` and `ExitingPlayMode` ([Registry.cs][src-registry]).
- `context.GetNodeReference(id)` → `FillAmount` (−100..100, **percent**), `ClearCustomization()`. `context.Motion.Play/Pause/Stop(nodeRef[, speed])` for a looping accent animation. `Play` throws `ArgumentException` if the ID is not a node in the current session ([m-progress]).
- `GetWireReference(outPortId, inPortId)` → `WidthOverride`, `Opacity`, `IsDashed`, plus `Motion.Play(wire)`. `GetPortReference(id).SetPreview(string)` ([XML][xml], [VN Director debug view][s-vn]).
- `NodeCustomizationEnabled`, `PortPreviewEnabled`, `WireCustomizationEnabled` hide feedback without discarding it. `ClearAllVisualization()` resets everything ([XML][xml]).
- Known issues: setting `FillAmount` on the first node right after entering Play mode may be ignored while the window refreshes, while `Motion.Play` still works ([6.6 thread, post 13][d-66]). The motion is hard to see on nodes without a colour in dark mode ([post 14][d-66]). Subgraph bug: see §4.1.

```csharp
// Runtime assembly. Visualization guarded exactly as in the official samples.
#if UNITY_EDITOR
using Unity.GraphToolkit.Editor.GraphVisualization;
#endif
public class AppStateRunner : MonoBehaviour
{
    public static AppStateRunner Active { get; private set; }
    public AppFlowAsset Flow;
    Hash128 m_Current;
#if UNITY_EDITOR
    Context m_Debug;
#endif
    void OnEnable()  { Active = this; }
    void OnDisable() { if (Active == this) Active = null;
#if UNITY_EDITOR
        m_Debug?.Dispose();
#endif
    }
    void Start()
    {
#if UNITY_EDITOR
        m_Debug = Registry.CreateVisualizationContext(Flow.GraphId);   // authoring Graph.ID, created after entering Play mode
#endif
        Enter(Flow.EntryId);
    }
    public void ForceState(Hash128 stateId) => Enter(stateId);      // called by editor tooling too
    void Enter(Hash128 id)
    {
        // ... exit current behaviour, enter new one ...
#if UNITY_EDITOR
        if (m_Debug is { IsValid: true, IsGraphLoaded: true })
        {
            if (m_Current.isValid) m_Debug.Motion.Stop(m_Debug.GetNodeReference(m_Current));
            m_Debug.Motion.Play(m_Debug.GetNodeReference(id));
        }
#endif
        m_Current = id;
    }
}
```

### 5.2 Workaround: coloured or labelled active node on 6.6

`Node.DefaultColor` and `Node.Subtitle` are public setters ([XML][xml]). In the 6.6 source, user-node implementations store them in `[NonSerialized]` fields and set them inside `GraphModel.BlockAssetDirtyScope()` with a `ChangeHint.Style` change ([UserNodeModelImp.gen.cs][src-usernode]). They therefore act as transient view styling: the asset is not dirtied, and the value is lost on reload. Reaching the node instance the window shows: `GraphDatabase.LoadGraph<T>(path)` resolves the in-memory graph object, while `LoadGraphForImporter` returns a fresh copy ([GraphDatabase.cs L135–142][src-gdb], [LoadGraphForImporter remarks][r-lgfi]). `IGraphWindow.Graph` also returns it, for example from a toolbar element.

```csharp
// Editor-only; restore original colours on exit. Immediate canvas refresh is [UNVERIFIED].
static void MarkActive(AppFlowGraph g, Hash128 active)
{
    foreach (var n in g.GetNodes().OfType<StateNode>())
    {
        n.Subtitle = n.ID == active ? "● ACTIVE" : null;
        n.DefaultColor = n.ID == active ? new Color(0.2f, 0.9f, 0.3f) : n.AuthoredColor;
    }
}
```

Drive it from an `[InitializeOnLoad]` editor class that subscribes to a static `AppStateRunner.StateEntered` event. The graph path is `AssetDatabase.GetAssetPath(runner.Flow)`, because the runtime asset is the main object of the `.appflow` file.

### 5.3 Not possible on 6.6

- Outline, background tint, badge or overlay on a node: there is no public property for it. The per-type USS stylesheet is static, and no public API toggles USS classes at runtime ([XML][xml]).
- Custom VisualElements inside nodes: `NodeView<T>` is 6.7+ ([d-67]).
- Programmatic selection or framing of a node: not in the public API (§5.4, [IGraphWindow.cs][src-igw]).
- Walking `EditorWindow.rootVisualElement` to find node elements and inject UI only relies on public UI Toolkit calls, but the element hierarchy and classes are internal GTK implementation (`GraphViewEditorWindow`, `ModelView` are `internal`). Fragile; not recommended.

### 5.4 Evidence: 6.6 vs 7000.0 public surface (local metadata scan)

Public types were listed with `System.Reflection.Metadata` from the installed DLLs:

- **6000.6.6f1** `UnityEditor.GraphToolkitModule.dll`: no `StateMachine`, `State`, `Condition`, `NodeView`, `GraphMenuAttribute` or `MenuContext`. `IGraphWindow` exposes only `get_Graph`. `NodeReference` exposes only `FillAmount`, `NodeID`, `Context`, `ClearCustomization`.
- **7000.0.0a16** (also installed): adds `StateMachine`, `State`, `IState`, `ITransition`, `ITransitionRule`, `Condition`, `Condition<T>`, `SelfTransition`, `StateMachineDatabase`, `NodeView<T>`, `StateView<T>`, `ConditionView<T>`, `GraphMenuAttribute`, `StateMachineMenuAttribute`, `BlackboardMenuAttribute`, `ConditionMenuAttribute`, `MenuContext.AppendAction`, `IGraphWindow.StateMachine`, `GraphVisualization.StateReference` (`FillAmount`), `TransitionReference` (`LineColor`, `FillColor`, `Icon`, `IsDashed`, `Opacity`, `WidthOverride`), `ConditionReference` (`Icon`) and `DebugStyles` (`True`/`False`/`Pending` icons).

---

## 6. Forcing state changes from the graph

GTK has no runtime, so "force state" always means editor code calling your runner's public method (`AppStateRunner.Active?.ForceState(id)`). The editor asmdef may reference the runtime asmdef, as the samples do ([GSM code][s-gsm-code]).

### 6.1 Supported on 6.6: toolbar dropdown

`[GraphToolbarElement(id, typeof(Graph), order)]` on any `VisualElement` that implements `IAccessContainerWindow` ([m-toolbar]). Unity staff confirmed dropdowns work: *"You are not limited to buttons in the toolbar. You can use any VisualElement"* ([6.6 thread, post 15][d-66]). The VN sample hides its toolbar items outside Play mode ([s-vn]).

```csharp
using UnityEditor.Toolbars;

[GraphToolbarElement(Id, typeof(AppFlowGraph), order: 300)]
class ForceStateDropdown : EditorToolbarDropdown, IAccessContainerWindow
{
    public const string Id = "AppFlow/ForceState";
    public EditorWindow containerWindow { get; set; }

    public ForceStateDropdown() { text = "Force State"; clicked += ShowMenu; }

    void ShowMenu()
    {
        if (containerWindow is not IGraphWindow { Graph: AppFlowGraph graph }) return;
        var runner = AppStateRunner.Active;
        var menu = new GenericMenu();
        foreach (var s in graph.GetNodes().OfType<StateNode>())
        {
            var id = s.ID;
            if (EditorApplication.isPlaying && runner != null)
                menu.AddItem(new GUIContent(s.Title), false, () => runner.ForceState(id));
            else
                menu.AddDisabledItem(new GUIContent(s.Title));
        }
        menu.DropDown(worldBound);
    }
}
```

Limit: the toolbar cannot target "the node I just clicked", because 6.6 has no public selection API (selection does not reach `UnityEditor.Selection` ([GraphView.cs][src-graphview])). Hence a list of states.

### 6.2 Supported on 6.7+: per-node entries

```csharp
// 6000.7+ only (Graph API variant). [StateMachineMenu] is the equivalent for StateMachine tools.
[GraphMenu(typeof(AppFlowGraph))]
static void Menu(GraphMenuContext context)
{
    if (EditorApplication.isPlaying && context.ClickedObject is StateNode s)
        context.AppendAction("Force enter (Play Mode)", () => AppStateRunner.Active?.ForceState(s.ID));
}
```

Shape from the [6.7 beta post][d-67]. On 6.7+ a `NodeView<T>` / `StateView<T>` can also add a button inside the node ([d-67]).

### 6.3 Other options

- **Custom Inspector / EditorWindow** for `AppStateRunner` with one button per state and live highlighting of the current one: SUPPORTED (standard UnityEditor API), independent of GTK.
- **`GraphLogAction` button**: markers with action buttons can only be created in `OnGraphChanged` ([6.6 alpha post][d-66]), which does not run when the game changes state. WORKAROUND, not recommended.
- **Editing a node option in Play mode to trigger a force**: edits the asset (dirty, undo, reimport). Not recommended **[UNVERIFIED]** behaviour.
- Note from source: when entering Play mode, the graph window can cancel Play mode if it cannot close cleanly (unsaved-changes prompt) ([GraphViewEditorWindow.cs L964–973][src-window]) [INFERENCE from source].

---

## 7. Limitations & risks

1. **Version gap.** The State Machine API, per-node menus and custom node UI are 6000.7+, still beta per Unity's Oct 2026 post ([d-67]). Moving a 6.6 Graph-API tool to `StateMachine` later rewrites the authoring classes. Keep the runtime asset and runner independent of GTK so only the editor layer changes.
2. **API churn.** There were breaking changes in each minor release (for example `Untyped`) ([d-66]), and the samples package is experimental ([s-index]).
3. **Visualization stability.** Staff: *"the visualization API is unstable in some regards"*. Subgraph visualization throws (IN-155051) ([6.7 thread, post 9][d-67]). Expect a FillAmount timing issue right after entering Play mode ([d-66]).
4. **Weak "active" marker on 6.6.** The accent animation alone is subtle; combine it with wire styling, a port preview, and optionally the `DefaultColor` workaround (§5.2).
5. **Window must be open.** `Context.IsGraphLoaded` is false otherwise; check it before calling `Play` to avoid `ArgumentException` ([m-progress]).
6. **Option/port names are lookup keys.** The importer depends on them; renaming node classes breaks saved assets without `[MovedFrom]` ([GSM code][s-gsm-code]).
7. **Editor-only module.** Any non-guarded reference from a runtime assembly breaks player builds ([s-vn]).

---

## 8. Alternatives

| Option | Fit | Source |
|---|---|---|
| **Visual Scripting state graphs** (`com.unity.visualscripting` 1.9.12, already in the manifest) | Built-in Play-mode debugging: *"the currently active nodes are highlighted in blue"*, plus live editing in Play mode. But logic lives in VS graphs (an execution system), not your own C# runtime. Forcing a state means VS events or variables. | Local: `Library/PackageCache/com.unity.visualscripting@4edfcc09512f/Documentation~/vs-debugging.md` L53, `vs-live.md`; online [vs-live][vs-live] |
| **GraphView** (`UnityEditor.Experimental.GraphView`) | Full control over `VisualElement` nodes (highlight, menus, buttons are easy), but frozen: Unity: *"we are not touching it. It is still experimental and will be until the day it retires."* | Unity staff quote (Unite 2024 animation update) reproduced in [discussions 1514369][d-gv] |
| **Custom UI Toolkit EditorWindow** | Most control, most work; good for a simple "state list + force buttons" debugger next to a GTK authoring graph | standard UI Toolkit |
| **GTK on 6000.7+** | Best fit long term: dedicated state machine plus a state-aware debug view and per-state menus | [d-67], [s-gsm-code] |
| Third-party graph frameworks | Not evaluated (no primary source checked) **[UNVERIFIED]** | n/a |

---

## 9. Recommended architecture

```
Assets/AppFlow/
  Runtime/  (AppFlow.Runtime.asmdef, all platforms)
    AppFlowAsset.cs        ScriptableObject: GraphId, EntryId, States[{Id:Hash128, Name, [SerializeReference] AppStateBehaviour}],
                           Transitions[{NodeId, From, To, Trigger, wire port IDs}]
    AppStateBehaviour.cs   abstract OnEnter/OnUpdate/OnExit (scene load, pause timeScale, ...)
    AppStateRunner.cs      MonoBehaviour: ticks, Fire(trigger), ForceState(id), static Active, event StateEntered;
                           #if UNITY_EDITOR GraphVisualization: Motion.Play on active node, wire style on taken transition,
                           port preview "t=3.2s"
  Editor/   (AppFlow.Editor.asmdef, Editor only, references AppFlow.Runtime)
    AppFlowGraph.cs        Graph + OnGraphChanged validation (one Entry, no dangling transitions, unreachable states)
    Nodes/*.cs             EntryNode, abstract StateNode + Boot/MainMenu/Loading/Gameplay/Pause, TransitionNode
    AppFlowImporter.cs     ScriptedImporter → AppFlowAsset (main object)
    Toolbar/ForceStateDropdown.cs, Toolbar/DebugViewToggle.cs (flip *CustomizationEnabled via Registry.GetActiveContext)
    ActiveNodeTint.cs      optional §5.2 workaround (DefaultColor/Subtitle), [InitializeOnLoad]
```

Decision:

- **6000.6** (superseded): use the layout above with the toolbar dropdown and accent animation.
- **6000.7 (current target)**: replace `Editor/` with `StateMachine`/`State`/`Condition<T>` classes modelled on the Game State Machine sample. Add `[StateMachineMenu]` "Force enter" on states, a `StateView<T>` active badge + force button, and use `StateReference`/`TransitionReference` for the debug view (§0). The runtime folder above carries over unchanged.

---

## 10. Sources

Unity manual / script reference (6000.6 unless noted):
- [m-index]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index
- [m-impl]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/implementing-a-graph-tool
- [m-nodes]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/implementing-a-graph-tool/node-introduction
- [m-subgraph]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/implementing-a-graph-tool/add-subgraph-support
- [m-toolbar]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/implementing-a-graph-tool/add-custom-toolbar-actions
- [m-processing]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/implementing-a-graph-tool/graph-processing
- [m-debug]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/landing-implementing-debug-graph-view
- [m-viz]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/landing-implementing-debug-graph-view/concept-graph-visualization
- [m-progress]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/landing-implementing-debug-graph-view/task-display-node-progress
- Errors/markers: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/landing-implementing-debug-graph-view/task-display-errors-in-graph
- [r-registry]: https://docs.unity.com/en-us/engine/6000.6/script-reference/unity/graphtoolkit/editor/graphvisualization/registry
- [r-lgfi]: https://docs.unity.com/en-us/engine/6000.6/script-reference/unity/graphtoolkit/editor/graphdatabase/loadgraphforimporter
- [m-importer]: https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/import-assets/scripted-importers (via Context7 `/websites/unity_en-us_engine_manual/6000.6`)
- [m-index-7]: https://docs.unity.com/en-us/engine/7000.0/manual/extending-the-editor/gtk-index
- [m-gvs-7]: https://docs.unity.com/en-us/engine/7000.0/manual/extending-the-editor/gtk-index/concept-graphs-vs-state-machines
- [m-sm-7]: https://docs.unity.com/en-us/engine/7000.0/manual/extending-the-editor/gtk-index/implementing-a-state-machine
- [r-sm-67]: https://docs.unity3d.com/6000.7/Documentation/ScriptReference/Unity.GraphToolkit.Editor.StateMachine.html

Samples (com.unity.graphtoolkit-samples 0.6.7-exp.2):
- [s-index]: https://docs.unity3d.com/Packages/com.unity.graphtoolkit-samples@0.6/manual/index.html
- [s-vn]: https://docs.unity3d.com/Packages/com.unity.graphtoolkit-samples@0.6/manual/visual-novel-director-explore-the-code.html
- [s-gsm-intro]: https://docs.unity3d.com/Packages/com.unity.graphtoolkit-samples@0.6/manual/game-state-machine-introduction.html
- [s-gsm-code]: https://docs.unity3d.com/Packages/com.unity.graphtoolkit-samples@0.6/manual/game-state-machine-explore-the-code.html

Unity Discussions (official posts by Unity staff jeanedouard_unity / Jo_at_Unity):
- [d-66]: https://discussions.unity.com/t/graph-toolkit-update-in-unity-6-6-alpha/1721970
- [d-67]: https://discussions.unity.com/t/graph-toolkit-update-in-unity-6-7-beta/1735612
- [d-gv]: https://discussions.unity.com/t/when-will-the-graph-view-api-be-replaced-and-how-different-will-it-be/1514369 (quotes Unity's post https://discussions.unity.com/t/animation-status-update-q3-2024-unite-announcement/1519289)
- [vs-live]: https://docs.unity3d.com/Packages/com.unity.visualscripting@1.9/manual/vs-live.html

Module source (UnityCsReference, branch 6000.6):
- [src-root]: https://github.com/Unity-Technologies/UnityCsReference/tree/6000.6/Modules/GraphToolkitEditor
- [src-usernode]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/GraphToolkitEditor/GraphToolkitPublic/Implementation/Nodes/UserNodes/UserNodeModelImp.gen.cs
- [src-igw]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/GraphToolkitEditor/GraphToolkitPublic/Window/IGraphWindow.cs
- [src-graphview]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/GraphToolkitEditor/GraphToolkitEditor/UI/Views/GraphView/GraphView.cs#L2990
- [src-gdb]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/GraphToolkitEditor/GraphToolkitPublic/Graph/GraphDatabase.cs#L135-L142
- [src-registry]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/GraphToolkitEditor/GraphToolkitCommonEditor/GraphVisualization/Registry.cs#L106-L114
- [src-window]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/GraphToolkitEditor/GraphToolkitEditor/UI/Windows/GraphViewEditorWindow.cs#L964-L973

Local files:
- [xml]: `C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Data/Managed/UnityEngine/UnityEditor.GraphToolkitModule.xml` (+ `.dll`, scanned with System.Reflection.Metadata)
- `C:/Program Files/Unity/Hub/Editor/7000.0.0a16/Editor/Data/Managed/UnityEngine/UnityEditor.GraphToolkitModule.dll` (scanned the same way)
- `C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.graphtoolkit/{package.json,CHANGELOG.md}`
- Lead only (verified above against Unity sources): `.agents/skills/build-gtk/SKILL.md` and `references/*.md`

[m-index]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index
[m-impl]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/implementing-a-graph-tool
[m-nodes]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/implementing-a-graph-tool/node-introduction
[m-subgraph]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/implementing-a-graph-tool/add-subgraph-support
[m-toolbar]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/implementing-a-graph-tool/add-custom-toolbar-actions
[m-processing]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/implementing-a-graph-tool/graph-processing
[m-debug]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/landing-implementing-debug-graph-view
[m-viz]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/landing-implementing-debug-graph-view/concept-graph-visualization
[m-progress]: https://docs.unity.com/en-us/engine/6000.6/manual/extending-the-editor/gtk-index/landing-implementing-debug-graph-view/task-display-node-progress
[r-registry]: https://docs.unity.com/en-us/engine/6000.6/script-reference/unity/graphtoolkit/editor/graphvisualization/registry
[r-lgfi]: https://docs.unity.com/en-us/engine/6000.6/script-reference/unity/graphtoolkit/editor/graphdatabase/loadgraphforimporter
[m-importer]: https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/import-assets/scripted-importers
[m-index-7]: https://docs.unity.com/en-us/engine/7000.0/manual/extending-the-editor/gtk-index
[m-gvs-7]: https://docs.unity.com/en-us/engine/7000.0/manual/extending-the-editor/gtk-index/concept-graphs-vs-state-machines
[m-sm-7]: https://docs.unity.com/en-us/engine/7000.0/manual/extending-the-editor/gtk-index/implementing-a-state-machine
[r-sm-67]: https://docs.unity3d.com/6000.7/Documentation/ScriptReference/Unity.GraphToolkit.Editor.StateMachine.html
[s-index]: https://docs.unity3d.com/Packages/com.unity.graphtoolkit-samples@0.6/manual/index.html
[s-vn]: https://docs.unity3d.com/Packages/com.unity.graphtoolkit-samples@0.6/manual/visual-novel-director-explore-the-code.html
[s-gsm-intro]: https://docs.unity3d.com/Packages/com.unity.graphtoolkit-samples@0.6/manual/game-state-machine-introduction.html
[s-gsm-code]: https://docs.unity3d.com/Packages/com.unity.graphtoolkit-samples@0.6/manual/game-state-machine-explore-the-code.html
[d-66]: https://discussions.unity.com/t/graph-toolkit-update-in-unity-6-6-alpha/1721970
[d-67]: https://discussions.unity.com/t/graph-toolkit-update-in-unity-6-7-beta/1735612
[d-gv]: https://discussions.unity.com/t/when-will-the-graph-view-api-be-replaced-and-how-different-will-it-be/1514369
[vs-live]: https://docs.unity3d.com/Packages/com.unity.visualscripting@1.9/manual/vs-live.html
[src-root]: https://github.com/Unity-Technologies/UnityCsReference/tree/6000.6/Modules/GraphToolkitEditor
[src-usernode]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/GraphToolkitEditor/GraphToolkitPublic/Implementation/Nodes/UserNodes/UserNodeModelImp.gen.cs
[src-igw]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/GraphToolkitEditor/GraphToolkitPublic/Window/IGraphWindow.cs
[src-graphview]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/GraphToolkitEditor/GraphToolkitEditor/UI/Views/GraphView/GraphView.cs#L2990
[src-gdb]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/GraphToolkitEditor/GraphToolkitPublic/Graph/GraphDatabase.cs#L135-L142
[src-registry]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/GraphToolkitEditor/GraphToolkitCommonEditor/GraphVisualization/Registry.cs#L106-L114
[src-window]: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6/Modules/GraphToolkitEditor/GraphToolkitEditor/UI/Windows/GraphViewEditorWindow.cs#L964-L973
[xml]: https://docs.unity.com/en-us/engine/6000.6/script-reference/unity/graphtoolkit/editor
