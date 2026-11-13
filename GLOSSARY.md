# AppFlow

App-level state machine authored as a Graph Toolkit graph (`.appflow`): each state has an optional scene and a behaviour, edges customize how a pair of states transitions.

## Language

**App State**:
A screen-level mode of the app with its own optional scene.
_Avoid_: screen, page

**Phase**:
One step of entering or leaving a state: PreEnter, Enter, PostEnter, PreExit, Exit, PostExit, plus Running.

**Edge**:
A graph link A→B that changes how that pair transitions.
_Avoid_: connection

**Default flow**:
The phase order used when no edge applies.

**Crossfade**:
Edge kind that runs the source's Exit and the target's Enter together.

**Custom transition**:
Edge kind whose code orders the phases itself.
_Avoid_: transition script

**Running**:
The phase in which a state is settled and ticked every frame.
