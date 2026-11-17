# Changelog

All notable changes to this package are documented here. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [SemVer](https://semver.org/).

## [Unreleased]

### Added

- State **Features**: post-processing-style list on App States; any `AppStateFeature` subclass appears in "Add Feature". Built-in `SceneFeature` and `LogFeature`.

### Changed

- **Breaking:** the Scene / LoadMode / UnloadOnExit state options moved into `SceneFeature`; `AppStateData.ScenePath/LoadMode/UnloadOnExit` were removed. Graphs saved with 0.1.0 lose their scene settings when opened; re-add them as a Scene feature (or `unity command appflow.graph set_scene`).

## [0.1.0] - 2026-11-13

### Added

- Initial release: AppFlow runtime, Graph Toolkit editor, optional UniTask and Unity Pipeline CLI integrations, Showcase sample.
