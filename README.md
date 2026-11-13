# AppFlow

App-level state machine (Startup, MainMenu, Gameplay, ...) authored as a Graph Toolkit state machine (`.appflow`). Unity package `com.fuji97.appflow`.

## Install

Package Manager > `+` > "Install package from git URL" > `https://github.com/fuji97/appflow.git#v0.1.0`. Requires Unity 6000.7+.

## Use

1. `Assets > Create > App Flow > App Flow Graph`. Add `App State` nodes. Per state, in the Graph Inspector:
   - **Name**: unique, used by code and CLI (Graph Toolkit does not persist `State.Title` set from code, so the name is an option and drives the title).
   - **Entry**: exactly one. **Behaviour**: an `AppStateBehaviour` asset. **Scene** / **LoadMode** / **UnloadOnExit**: loaded in `Enter`, unloaded after `Exit`.
2. An edge A→B can customize how that pair transitions. Add a `Custom Transition` condition (an `AppTransitionBehaviour` asset) and/or `Crossfade`. An edge with neither uses the default flow.
3. Put an `AppFlowRunner` in the boot scene and assign the `.appflow` asset (its main object is an `AppFlowAsset`).
4. Move by reference: `[SerializeField] AppStateRef next;` (sub-asset of the `.appflow`), then `await AppFlowRunner.Active.Machine.TransitTo(next)`.

## Phases

`AppPhase`: PreEnter, Enter, PostEnter, Running, PreExit, Exit, PostExit. Default flow A→B: `A.PreExit, B.PreEnter, A.Exit, B.Enter, A.PostExit, B.PostEnter`, then `B.Running`. Crossfade starts `A.Exit` and `B.Enter` together. A custom transition receives `AppTransitionContext` (both states) and calls the step helpers (`ctx.PreExit()`, `ctx.Enter()`, `ctx.RunDefault()` ...) in any order. A second `TransitTo` while one runs is rejected (returns false, warning). Hook exceptions are logged and the transition continues.

## CLI (optional)

Requires `com.unity.pipeline`; the `AppFlow.Pipeline*` assemblies compile only when it is installed.

- Play mode / development Player: `unity command appflow status | list_states | transit --state X [--wait false] | history`.
- Edit mode: `unity command appflow.graph describe | create | add_state | connect | set_behaviour | set_scene` (all take `--asset`).

## Play mode graph view

Open the `.appflow` window before or during Play: the active state animates and shows a phase badge; right-click a state > `App Flow/Force enter`.

## UniTask

`AppFlow.UniTask` compiles only when `com.cysharp.unitask` is installed (`TransitToUniTask`). Hooks stay `Awaitable`.

## Samples

Package Manager > AppFlow > Samples > Import "Showcase". Add the imported `Scenes/*.unity` to the Build Profile scene list, open `Scenes/Boot.unity`, press Play. UI Toolkit showcase (Fredoka font, playful style). Flow: Splash > Main menu > Settings / Credits / Game > Pause > back to the menu. A top-right HUD shows the current state, phase and the last transition.

| Edge | Kind | Visible effect |
|---|---|---|
| Splash > MainMenu, MainMenu > Credits, Game > Pause, ... (no edge) | default flow | sequential: the old screen pops out, then the new one pops in |
| MainMenu <> Settings | crossfade | both screens slide sideways at the same time |
| MainMenu > Game, Pause > MainMenu | custom (`CurtainTransition`) | curtain drops, loading bar fills, curtain lifts |
| Pause > Game | custom (`ResumeTransition`) | only Pause's exit phases run: Game is not re-entered, its scene is not reloaded |

Keys: `Esc` goes back (Game opens Pause, Pause resumes), `F1` toggles the HUD, any key or click skips the splash. The Game score only advances in the `Running` phase, so it freezes by itself while Pause is active. Settings: animation speed (scales every transition), HUD, fullscreen.

## Releasing

Bump `package.json` version, add a CHANGELOG entry, commit, `git tag vX.Y.Z`, `git push origin main vX.Y.Z`, then `gh release create vX.Y.Z --notes-from-tag` (or paste the CHANGELOG section). The tag must equal `v` + the `package.json` version.
