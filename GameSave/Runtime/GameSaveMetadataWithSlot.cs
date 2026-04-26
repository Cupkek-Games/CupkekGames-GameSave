namespace CupkekGames.Systems
{
    public struct GameSaveMetadataWithSlot<TSaveMetadata> where TSaveMetadata : GameSaveMetadata
    {
        public TSaveMetadata Metadata { get; private set; }
        public int SaveSlot { get; private set; }

        public GameSaveMetadataWithSlot(TSaveMetadata metadata, int saveSlot)
        {
            Metadata = metadata;
            SaveSlot = saveSlot;
        }
    }
  }