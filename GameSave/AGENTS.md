# CupkekGames.Systems.GameSave — AI Agent Instructions

## Package Overview

**CupkekGames.Systems.GameSave** is a generic slot-based save/load system for Unity games. It provides:
- **Multi-slot saves** with persistent metadata (save date, version, autosave flag)
- **Autosave rotation** (e.g., keep last 5 autosaves, discard oldest)
- **Type-safe save data** via `IGameSaveData` interface combined with `IData`
- **Metadata tracking** (save date, game version, autosave vs. manual save)
- **Version control** for save migration and compatibility

The system is **inherently generic**: you define your own save data structure (`TSaveData`), and the framework handles slots, file I/O, autosave logic, and events.

## Core Concepts

### 1. IGameSaveData Interface
Defines minimal contract for save-compatible data:
```csharp
public interface IGameSaveData
{
    GameSaveMetadata Metadata { get; set; }
    void LoadFrom(IGameSaveData other, int saveSlot);
    GameSaveMetadata CreateMetadata(string saveVersion, bool isAutosave);
}
```

**Rules:**
- **Metadata** → Timestamp, game version, autosave flag
- **LoadFrom** → Copy data from another save (used for overwriting, slot management)
- **CreateMetadata** → Game decides version string and autosave status

**Requirements:**
- Implement `IData` as well (for serialization via `DataSO<T>`)
- Implement default constructor (`new()`)

**Example:**
```csharp
[Serializable]
public class MyGameSaveData : IGameSaveData, IData
{
    public GameSaveMetadata Metadata { get; set; }
    public string PlayerName = "Player";
    public int Level = 1;
    public Dictionary<string, int> Inventory = new();
    
    public void LoadFrom(IGameSaveData other, int saveSlot)
    {
        if (other is MyGameSaveData otherSave)
        {
            PlayerName = otherSave.PlayerName;
            Level = otherSave.Level;
            Inventory = new(otherSave.Inventory);
        }
    }
    
    public GameSaveMetadata CreateMetadata(string saveVersion, bool isAutosave)
    {
        return new GameSaveMetadata(DateTime.Now, saveVersion, isAutosave);
    }
    
    public bool Validate() => !string.IsNullOrEmpty(PlayerName) && Level > 0;
    public void OnAfterDeserialize() { }
}
```

### 2. GameSaveMetadata
Immutable save metadata:
```csharp
[Serializable]
public class GameSaveMetadata
{
    public DateTime SaveDate;      // When saved
    public string SaveVersion;     // Game version at save time
    public bool IsAutosave;        // Autosave vs. manual save
}
```

**Subclass for custom metadata:**
```csharp
[Serializable]
public class MyGameSaveMetadata : GameSaveMetadata
{
    public string PlaythroughDifficulty = "Normal";
    public float PlayTime = 0f;
}
```

Then use in manager:
```csharp
public class MyGameSaveManager : GameSaveManager<MyGameSaveData, MyGameSaveMetadata> { }
```

### 3. GameSaveManager<TSaveData, TSaveMetadata>
Abstract generic manager for save/load orchestration:
```csharp
public abstract class GameSaveManager<TSaveData, TSaveMetadata> : ScriptableObject 
    where TSaveData : IGameSaveData, IData, new()
    where TSaveMetadata : GameSaveMetadata
{
    protected bool _enableAutosave = true;
    protected int _autosaveSlots = 5;
    public GameSaveDataSO<TSaveData> CurrentSave;
    
    // Core API
    public abstract string GetSaveVersion();
    public abstract TSaveData GetNewSave(string saveVersion);
    public abstract string GetSaveFileName(int saveSlot);
    public abstract TSaveData LoadFromFile(string fileName);
    public abstract void SaveToFile(int saveSlot, TSaveData data, bool isAutosave);
    
    // Public methods
    public void Autosave(TSaveData data);
    public TSaveData GetSave(int saveSlot);
    public List<GameSaveMetadataWithSlot<TSaveMetadata>> GetAllMetadata(bool includeAutosaves);
    public void DeleteSave(int saveSlot);
    public int GetFirstAvailableSlot();
}
```

**Template methods (override in your game):**
- **GetSaveVersion()** → Returns current game version (e.g., "1.0.0")
- **GetNewSave()** → Creates fresh save with default data
- **GetSaveFileName()** → Maps slot to file path (e.g., `$"{persistentPath}/save_{slot}.json"`)
- **LoadFromFile()** → Deserialize from disk
- **SaveToFile()** → Serialize to disk

**Automatic features:**
- **Autosave rotation** → When `_autosaveSlots` is exceeded, replaces oldest by date
- **First available slot detection** → For manual saves
- **Metadata tracking** → All saves include timestamp and version

### 4. GameSaveDataSO<TSaveData>
Wraps save data in a ScriptableObject (inherits from `DataSO<TSaveData>`):
```csharp
public abstract class GameSaveDataSO<TSaveData> : DataSO<TSaveData> 
    where TSaveData : IGameSaveData, IData, new()
{
}
```

Used to:
- Cache the current save in memory
- Provide Inspector persistence during development
- Integrate with `DataSO` inheritance chain

**Example:**
```csharp
public class MyGameSaveDataSO : GameSaveDataSO<MyGameSaveData>
{
    [MenuItem("Assets/Create/MyGame/Save Data")]
    public static void Create() => CreateInstance<MyGameSaveDataSO>();
}
```

### 5. GameSaveEvents
Optional event system for save/load lifecycle:
```csharp
public static class GameSaveEvents
{
    public static event Action<TSaveData> OnBeforeSave;
    public static event Action<TSaveData> OnAfterLoad;
    public static event Action<int> OnSaveDeleted;
    // ... (depending on implementation)
}
```

## Package Structure

```
CupkekGames.Systems.GameSave/
  Runtime/
    CupkekGames.Systems.GameSave.asmdef
    IGameSaveData.cs              ← Define your save data structure here
    GameSaveManager.cs            ← Extend for game-specific save logic
    GameSaveDataSO.cs             ← Base ScriptableObject wrapper
    GameSaveMetadata.cs           ← Save metadata (date, version, autosave flag)
    GameSaveMetadataWithSlot.cs   ← Metadata + slot number tuple
    GameSaveEvents.cs             ← Optional event hooks
```

## Usage Patterns

### Pattern 1: Define Your Game's Save Data

```csharp
namespace MyGame.Save
{
    [Serializable]
    public class GameSaveData : IGameSaveData, IData
    {
        public GameSaveMetadata Metadata { get; set; }
        
        // Game state
        public string PlayerName = "Player";
        public int CurrentLevel = 1;
        public Vector3 PlayerPosition = Vector3.zero;
        public List<InventoryItem> Inventory = new();
        
        // IGameSaveData implementation
        public void LoadFrom(IGameSaveData other, int saveSlot)
        {
            if (other is GameSaveData save)
            {
                PlayerName = save.PlayerName;
                CurrentLevel = save.CurrentLevel;
                PlayerPosition = save.PlayerPosition;
                Inventory = new(save.Inventory);
            }
        }
        
        public GameSaveMetadata CreateMetadata(string saveVersion, bool isAutosave)
            => new(DateTime.Now, saveVersion, isAutosave);
        
        // IData implementation
        public bool Validate() => !string.IsNullOrEmpty(PlayerName) && CurrentLevel > 0;
        public void OnAfterDeserialize() { }
    }
    
    public class GameSaveDataSO : GameSaveDataSO<GameSaveData>
    {
        [MenuItem("Assets/Create/MyGame/Game Save")]
        public static void Create() => CreateInstance<GameSaveDataSO>();
    }
}
```

### Pattern 2: Implement Game-Specific Manager

```csharp
namespace MyGame.Save
{
    [CreateAssetMenu(menuName = "MyGame/Save Manager")]
    public class MyGameSaveManager : GameSaveManager<GameSaveData, GameSaveMetadata>
    {
        private const string SAVE_FOLDER = "Saves";
        private const string VERSION = "1.0.0";
        
        public override string GetSaveVersion() => VERSION;
        
        public override GameSaveData GetNewSave(string saveVersion)
        {
            var save = new GameSaveData
            {
                Metadata = new GameSaveMetadata(DateTime.Now, saveVersion, false),
                PlayerName = "Player",
                CurrentLevel = 1
            };
            return save;
        }
        
        public override string GetSaveFileName(int saveSlot)
        {
            string folder = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, $"save_{saveSlot}.json");
        }
        
        public override GameSaveData LoadFromFile(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException($"Save file not found: {fileName}");
            
            string json = File.ReadAllText(fileName);
            var serializer = ServiceLocator.Get<IDataSerializer>();
            return serializer.Deserialize<GameSaveData>(json);
        }
        
        public override void SaveToFile(int saveSlot, GameSaveData data, bool isAutosave)
        {
            data.Metadata = data.CreateMetadata(GetSaveVersion(), isAutosave);
            
            var serializer = ServiceLocator.Get<IDataSerializer>();
            string json = serializer.Serialize(data);
            
            string fileName = GetSaveFileName(saveSlot);
            File.WriteAllText(fileName, json);
            Debug.Log($"Save slot {saveSlot} written to {fileName}");
        }
    }
}
```

### Pattern 3: Use in Game Loop

```csharp
public class GameController : MonoBehaviour
{
    [SerializeField] private MyGameSaveManager _saveManager;
    
    private GameSaveData _currentSave;
    
    private void Start()
    {
        // Load most recent save
        var allMetadata = _saveManager.GetAllMetadata(includeAutosaves: false);
        if (allMetadata.Count > 0)
        {
            var mostRecent = allMetadata.OrderByDescending(m => m.Metadata.SaveDate).First();
            _currentSave = _saveManager.GetSave(mostRecent.SaveSlot);
            Debug.Log($"Loaded save: {_currentSave.PlayerName} @ Level {_currentSave.CurrentLevel}");
        }
        else
        {
            _currentSave = _saveManager.GetNewSave(_saveManager.GetSaveVersion());
        }
    }
    
    public void OnPlayerLevelUp()
    {
        _currentSave.CurrentLevel++;
        _saveManager.Autosave(_currentSave);  // Rotates autosave slots
    }
    
    public void OnManualSave()
    {
        int slot = _saveManager.GetFirstAvailableSlot();
        _saveManager.SaveToFile(slot, _currentSave, isAutosave: false);
    }
    
    public void OnLoadSave(int slot)
    {
        _currentSave = _saveManager.GetSave(slot);
        // Reload game state from _currentSave
    }
}
```

### Pattern 4: Custom Metadata for Version Control

```csharp
[Serializable]
public class ExtendedSaveMetadata : GameSaveMetadata
{
    public int PlayTimeSeconds;
    public string Difficulty;
}

public class MyGameSaveManager : GameSaveManager<GameSaveData, ExtendedSaveMetadata>
{
    public override string GetSaveVersion() => "1.2.0";
    
    public override GameSaveData GetNewSave(string saveVersion)
    {
        var save = new GameSaveData();
        return save;
    }
    
    // Override LoadFromFile to handle version migration
    public override GameSaveData LoadFromFile(string fileName)
    {
        string json = File.ReadAllText(fileName);
        
        // Try to deserialize
        var serializer = ServiceLocator.Get<IDataSerializer>();
        var save = serializer.Deserialize<GameSaveData>(json);
        
        // Migrate from old version if needed
        if (save.Metadata.SaveVersion.StartsWith("1.0"))
        {
            save.CurrentLevel = Mathf.Max(save.CurrentLevel, 1);  // Patch old saves
        }
        
        return save;
    }
}
```

## Integration Points

### ServiceLocator Requirement

`GameSaveManager` uses `ServiceLocator.Get<IDataSerializer>()` for serialization:
```csharp
public override GameSaveData LoadFromFile(string fileName)
{
    var serializer = ServiceLocator.Get<IDataSerializer>();
    return serializer.Deserialize<GameSaveData>(json);
}
```

**Setup:**
1. Register `IDataSerializer` in ServiceLocator (e.g., `NewtonsoftDataSerializer` from Data.Newtonsoft)
2. Ensure ServiceLocator is initialized before calling save/load methods

### With Data Package

`GameSaveData` implements `IData`, so it works with:
- `DataSO<T>` serialization system
- `IDataSerializer` (JSON/custom formats)
- `OnAfterDeserialize()` hooks for post-load logic

### With Newtonsoft Integration

For full game data serialization (including complex types like `Dictionary<string, List<T>>`):
1. Use `Data.Newtonsoft` to register `NewtonsoftDataSerializer`
2. Configure Newtonsoft converters/binders for your types
3. Save system automatically uses configured JSON settings

## Coding Conventions

- **Namespaces:** Game saves in `MyGame.Save` or similar (not `CupkekGames.*`)
- **Save data structure:** Flat, serializable types only (int, string, List<T>, etc.)
- **Metadata:** Track version for save migration; always include timestamp
- **Autosave slots:** Typically 3–5 slots; manual saves unlimited (use separate slots)
- **File paths:** Use `Application.persistentDataPath` on mobile/console
- **Validation:** Implement `Validate()` in `IGameSaveData` for data integrity checks
- **No direct Unity object references:** SOs, GameObjects not serializable; use string keys or IDs

## Common Tasks

### Task: Add a New Field to Save Data

1. Add field to `GameSaveData`
2. Initialize in `GetNewSave()`
3. Copy in `LoadFrom()`
4. Update `Validate()` if needed
5. Existing saves still load (new fields get default values)

### Task: Migrate Old Save Format

Override `LoadFromFile()` to detect old version and transform:
```csharp
public override GameSaveData LoadFromFile(string fileName)
{
    var json = File.ReadAllText(fileName);
    var data = serializer.Deserialize<GameSaveData>(json);
    
    // If old version, patch it
    if (data.Metadata.SaveVersion.StartsWith("1.0"))
        data.CurrentLevel = Mathf.Max(data.CurrentLevel, 1);
    
    return data;
}
```

### Task: Query All Saves by Metadata

```csharp
var allSaves = _saveManager.GetAllMetadata(includeAutosaves: true);
foreach (var metadata in allSaves.OrderByDescending(m => m.Metadata.SaveDate))
{
    Debug.Log($"Slot {metadata.SaveSlot}: {metadata.Metadata.SaveDate}");
}
```

### Task: Implement Save File Encryption

Subclass manager, override `SaveToFile()` / `LoadFromFile()`:
```csharp
public override void SaveToFile(int slot, GameSaveData data, bool isAutosave)
{
    var serializer = ServiceLocator.Get<IDataSerializer>();
    string json = serializer.Serialize(data);
    string encrypted = MyEncryption.Encrypt(json);
    File.WriteAllText(GetSaveFileName(slot), encrypted);
}

public override GameSaveData LoadFromFile(string fileName)
{
    string encrypted = File.ReadAllText(fileName);
    string json = MyEncryption.Decrypt(encrypted);
    // Deserialize as usual
}
```

## Notes for AI Assistants

- **Template methods are required** → Each game must implement file I/O, version tracking, and save creation
- **Autosave is optional** → Disable with `_enableAutosave = false` if not needed
- **Metadata is immutable** → Always created at save time via `CreateMetadata()`
- **Slot-based architecture** → Slots are just integers; storage mechanism is game-defined
- **Serialization framework-agnostic** → Works with JSON, MessagePack, Protocol Buffers, etc. via `IDataSerializer`
- **No built-in UI** → Game provides UI for load/save menus; system only handles persistence
