using CupkekGames.Data;

namespace CupkekGames.Systems
{
  public abstract class GameSaveDataSO<TSaveData> : DataSO<TSaveData> where TSaveData : IGameSaveData, IData, new()
  {
  }
}