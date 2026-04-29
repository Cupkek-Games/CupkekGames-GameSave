using CupkekGames.Data;

namespace CupkekGames.GameSave
{
  public abstract class GameSaveDataSO<TSaveData> : DataSO<TSaveData> where TSaveData : IGameSaveData, IData, new()
  {
  }
}