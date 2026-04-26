using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CupkekGames.Data;
using UnityEngine;
using System.Linq;

namespace CupkekGames.Systems
{
  public abstract class GameSaveManager<TSaveData, TSaveMetadata> : ScriptableObject where TSaveData : IGameSaveData, IData, new() where TSaveMetadata : GameSaveMetadata
  {
    [Header("Autosave")]
    [SerializeField] protected bool _enableAutosave = true;
    [SerializeField] protected int _autosaveSlots = 5;
    public GameSaveDataSO<TSaveData> CurrentSave;

    public void Autosave(TSaveData data)
    {
      if (!_enableAutosave)
      {
        Debug.LogWarning("Autosave is disabled.");
        return;
      }
      if (_autosaveSlots <= 0)
      {
        Debug.LogWarning("Autosave slots count is less than 0.");
        return;
      }

      // Get all metadata
      List<GameSaveMetadataWithSlot<TSaveMetadata>> allMetadata = GetAllMetadata(true);

      // Filter autosaves
      List<GameSaveMetadataWithSlot<TSaveMetadata>> autosaves = allMetadata
          .Where(m => m.Metadata != null && m.Metadata.IsAutosave)
          .ToList();

      int autosaveSlot = -1;

      // If we have fewer autosaves than the limit, find the first available slot
      if (autosaves.Count < _autosaveSlots)
      {
        autosaveSlot = GetFirstAvailableSlot();
      }
      // Otherwise, replace the oldest autosave
      else if (autosaves.Count > 0)
      {
        // Sort by date (oldest first)
        autosaves.Sort((x, y) => x.Metadata.SaveDate.CompareTo(y.Metadata.SaveDate));
        // Get the slot of the oldest autosave
        autosaveSlot = autosaves[0].SaveSlot;
      }

      if (autosaveSlot == -1)
      {
        Debug.LogWarning("No valid autosave slot found.");
        return;
      }

      Debug.Log($"Autosaving to slot {autosaveSlot}...");
      SaveToFile(autosaveSlot, data, true);
    }

    public TSaveData GetSave(int saveSlot)
    {
      if (saveSlot == -1)
      {
        throw new ArgumentException("Save slot is invalid.");
      }

      string fileName = GetSaveFileName(saveSlot);
      TSaveData data = GetNewSave(GetSaveVersion());
      try
      {
        data.LoadFrom(LoadFromFile(fileName), saveSlot);
      }
      catch (Exception e)
      {
        Debug.LogException(e, this);
        throw new ArgumentException("Failed to load save data.");
      }

      return data;
    }
    public TSaveMetadata GetMetadata(int saveSlot)
    {
      if (saveSlot == -1)
      {
        throw new ArgumentException("Save slot is invalid.");
      }

      string fileName = GetSaveFileName(saveSlot);
      TSaveMetadata metadata;
      try
      {
        metadata = LoadMetadataFromFile(fileName);
      }
      catch (Exception e)
      {
        Debug.LogException(e, this);
        throw new ArgumentException("Failed to load metadata.");
      }

      return metadata;
    }
    public List<TSaveData> GetAllSave(bool sortByDate)
    {
      List<string> fileNames = GetAllFileNames();

      List<TSaveData> list = new List<TSaveData>();

      foreach (string fileName in fileNames)
      {
        int saveSlot = ExtractSaveSlot(fileName);
        try
        {
          TSaveData saveData = GetSave(saveSlot);
          if (saveData != null)
          {
            list.Add(saveData);
          }
        }
        catch (Exception e)
        {
          Debug.LogException(e, this);
        }
      }

      if (sortByDate)
      {
        list.Sort((x, y) => y.Metadata.SaveDate.CompareTo(x.Metadata.SaveDate));
      }

      return list;
    }
    public List<GameSaveMetadataWithSlot<TSaveMetadata>> GetAllMetadata(bool sortByDate)
    {
      List<string> fileNames = GetAllFileNames();
      List<GameSaveMetadataWithSlot<TSaveMetadata>> list = new List<GameSaveMetadataWithSlot<TSaveMetadata>>();

      foreach (string fileName in fileNames)
      {
        int saveSlot = ExtractSaveSlot(fileName);
        try
        {
          TSaveMetadata metadata = GetMetadata(saveSlot);
          if (metadata != null)
          {
            list.Add(new GameSaveMetadataWithSlot<TSaveMetadata>(metadata, saveSlot));
          }
        }
        catch (Exception e)
        {
          Debug.LogException(e, this);
        }
      }

      if (sortByDate)
      {
        list.Sort((x, y) => y.Metadata.SaveDate.CompareTo(x.Metadata.SaveDate));
      }

      return list;
    }

    public GameSaveMetadataWithSlot<TSaveMetadata> GetLastMetadata()
    {
      List<string> fileNames = GetAllFileNames();
      if (fileNames.Count == 0)
      {
        return new GameSaveMetadataWithSlot<TSaveMetadata>(null, -1);
      }

      TSaveMetadata lastMetadata = null;
      int lastSlot = -1;
      DateTime lastDate = DateTime.MinValue;

      foreach (string fileName in fileNames)
      {
        int saveSlot = ExtractSaveSlot(fileName);
        try
        {
          TSaveMetadata metadata = GetMetadata(saveSlot);
          if (metadata != null && metadata.SaveDate > lastDate)
          {
            lastDate = metadata.SaveDate;
            lastMetadata = metadata;
            lastSlot = saveSlot;
          }
        }
        catch (Exception e)
        {
          Debug.LogException(e, this);
        }
      }

      return new GameSaveMetadataWithSlot<TSaveMetadata>(lastMetadata, lastSlot);
    }

    public string GetSaveFileName(int slot)
    {
      return "save" + slot.ToString() + "." + GetFileExtenstion();
    }

    public int ExtractSaveSlot(string saveString)
    {
      // Regular expression pattern for "save" followed by digits
      string formatPattern = Regex.Escape(GetFileExtenstion());
      Regex regex = new Regex(@"^save(\d+)\." + formatPattern + "$");
      Match match = regex.Match(saveString);

      if (match.Success)
      {
        string numberString = match.Groups[1].Value;
        return int.Parse(numberString);
      }

      return -1; // Return -1 to indicate an invalid format
    }

    public void SaveToFile(int slot, TSaveData data, bool autosave = false)
    {
      string fileName = GetSaveFileName(slot);

      OnSaveRequest(slot, fileName, data, autosave);
    }

    public void DeleteFile(int slot)
    {
      string fileName = GetSaveFileName(slot);

      OnDeleteRequest(slot, fileName);
    }

    public int GetFirstAvailableSlot()
    {
      List<string> fileNames = GetAllFileNames();
      if (fileNames.Count == 0)
      {
        return 0; // Always start with slot 0 if no saves exist
      }

      // Extract slot numbers and find the first available one
      HashSet<int> usedSlots = new HashSet<int>(
        fileNames.Select(ExtractSaveSlot).Where(slot => slot != -1)
      );

      // Find the first non-negative integer that's not in the set
      int slot = 0;
      while (usedSlots.Contains(slot))
      {
        slot++;
      }

      return slot;
    }

    protected abstract string GetFileExtenstion();
    protected abstract TSaveData GetNewSave(string saveVersion);
    protected abstract TSaveData LoadFromFile(string fileName);
    protected abstract TSaveMetadata LoadMetadataFromFile(string fileName);
    protected abstract void OnSaveRequest(int saveSlot, string fileName, TSaveData data, bool autosave);
    protected abstract void OnDeleteRequest(int saveSlot, string fileName);
    protected abstract List<string> GetAllFileNames();
    protected abstract string GetSaveVersion();
  }
}
