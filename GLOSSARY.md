# AppFlow

App-level state machine authored as a Graph Toolkit graph (`.appflow`): each state has a behaviour and a list of Features (e.g. its scene), edges customize how a pair of states transitions.

## Language

**App State**:
A screen-level mode of the app, with a behaviour and Features.
_Avoid_: screen, page

**Feature**:
Reusable lifecycle logic added to an App State from the inspector (an `AppStateFeature` subclass, e.g. Scene, Log). Features wrap the behaviour: they run before it while entering/running and after it while exiting.
_Avoid_: component, override, module

**Entry state**:
The App State the flow starts in; exactly one per graph.

**Transition**:
Moving the app from one App State to another, as Phases ordered by the Default flow, a Crossfade or a Custom transition. A Custom transition may skip Phases (e.g. resuming a paused state without entering it again).

**Phase**:
One step of entering or leaving a state: PreEnter, Enter, PostEnter, PreExit, Exit, PostExit, plus Running.

**Edge**:
A graph link A→B that can change how that pair transitions. An Edge with no Crossfade and no Custom transition uses the Default flow.
_Avoid_: connection

**Default flow**:
The phase order used when no Edge applies or the Edge adds nothing.

**Crossfade**:
Edge kind that runs the source's Exit and the target's Enter together.

**Custom transition**:
Edge kind whose code orders the phases itself.
_Avoid_: transition script

**Running**:
The phase in which a state is settled and ticked every frame.
