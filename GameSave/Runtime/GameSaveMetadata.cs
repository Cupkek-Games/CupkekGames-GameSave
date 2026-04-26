using System;

namespace CupkekGames.Systems
{
  [Serializable]
  public class GameSaveMetadata
  {
    public DateTime SaveDate;
    public string SaveVersion;
    public bool IsAutosave;
    public GameSaveMetadata()
    {
      SaveDate = DateTime.Now;
      SaveVersion = "1.0.0";
      IsAutosave = false;
    }
    public GameSaveMetadata(DateTime saveDate, string saveVersion, bool isAutosave)
    {
      SaveDate = saveDate;
      SaveVersion = saveVersion;
      IsAutosave = isAutosave;
    }

    public GameSaveMetadata(GameSaveMetadata other)
    {
      if (other == null)
        return;
      SaveDate = other.SaveDate;
      SaveVersion = other.SaveVersion;
      IsAutosave = other.IsAutosave;
    }
  }
}
