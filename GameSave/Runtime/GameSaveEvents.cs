using System;
using Unity.Scripting.LifecycleManagement;

namespace CupkekGames.GameSave
{
  public static partial class GameSaveEvents
  {
    [AutoStaticsCleanup]
    public static Action AutosaveStart;
    [AutoStaticsCleanup]
    public static Action AutosaveComplete;
  }
}
