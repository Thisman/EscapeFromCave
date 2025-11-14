# Scripts Directory Structure

- `Scenes/`: Scene-specific MonoBehaviours and UI assets grouped by screen; use it for anything tightly coupled to a particular scene setup.
  - `Scenes/Battle/`: Battle scene managers, lifetime scope, payload, and battle-only UI documents/controllers.
  - `Scenes/Dungeon/`: Dungeon scene wiring, lifetime scope, and the dungeon HUD layout/controllers.
  - `Scenes/Preparation/`: Preparation menu scene bootstrap and its UI documents/controllers.
  - `Scenes/MainMenu/`: Main menu scene wiring plus its visual layout and controller scripts.
  - `Scenes/Root/`: Root scene bootstrap components that host global composition before switching to gameplay scenes.
- `Gameplay/`: Reusable gameplay systems decoupled from individual scenes.
  - `Gameplay/Battle/`: Core battle mechanics, data models, controllers, effects, abilities, and AI utilities.
  - `Gameplay/Interactions/`: Interaction contexts, resolvers, and ScriptableObject effects shared across exploration.
- `Entities/`: Domain models and controllers for persistent game entities.
  - `Entities/Units/`: Unit, squad, and army definitions plus their read-only interfaces.
  - `Entities/Player/`: Player-specific controllers for movement, interactions, combat input, and resource management.
  - `Entities/Objects/`: Environment object and resource models/controllers with their access interfaces.
- `Services/`: Global services such as session, scene loading, input, audio, dialogs, and shared interfaces.
- `UICommon/`: Shared UI Toolkit assets (e.g., global styles) reused across scenes.
- `Utilities/`: Cross-cutting helper classes like cooldown/state utilities and scene helpers.
