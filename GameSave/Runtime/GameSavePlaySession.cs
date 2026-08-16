using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace CupkekGames.GameSave
{
  /// <summary>
  /// Per-editor-play / per-player-launch session id. Uses <see cref="RuntimeInitializeLoadType.BeforeSceneLoad"/>
  /// so it still bumps when Enter Play Mode disables Domain Reload (same idea as DataSOPlaySession / SequencerSessionState).
  /// </summary>
  internal static partial class GameSavePlaySession
  {
    [NoAutoStaticsCleanup]
    internal static int Id;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BumpPlaySessionId() => Id++;
  }
}
