namespace CupkekGames.Systems
{
  public interface IGameSaveData
  {
    public GameSaveMetadata Metadata { get; set; }
    
    public void LoadFrom(IGameSaveData other, int saveSlot);
    public GameSaveMetadata CreateMetadata(string saveVersion, bool isAutosave);
  }
}
