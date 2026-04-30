# CupkekGames GameSave

Generic save-manager pattern built on `com.cupkekgames.data`. Slot-based metadata, lifecycle events, serialization-agnostic — pair with `com.cupkekgames.newtonsoft` (or your own `IDataSerializer`) to persist saves.

## What's inside

**Runtime** (`CupkekGames.GameSave.asmdef`)

- `GameSaveManager<TData, TMeta>` — orchestrates save/load lifecycle for a slot
- `IGameSaveData` / `GameSaveDataSO` — game data contract + ScriptableObject base
- `GameSaveMetadata` / `GameSaveMetadataWithSlot` — per-save metadata
- `GameSaveEvents` — event hooks for save/load/delete

## Dependencies

- `com.cupkekgames.data`
- `com.cupkekgames.services` (for ServiceLocator-driven serializer resolution)

Bring your own `IDataSerializer` implementation, or use `com.cupkekgames.newtonsoft`.
