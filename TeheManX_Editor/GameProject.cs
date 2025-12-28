using System.Collections.Generic;

namespace TeheManX_Editor
{
    public class GameProject
    {
        public int EnemyOffset { get; set; }
        public List<Enemy>[] Enemies {  get; set; }
        public int CheckpointOffset { get; set; }
        public List<List<Checkpoint>> Checkpoints {  get; set; }
        public int CameraTriggersOffset { get; set; }
        public List<List<CameraTrigger>> CameraTriggers { get; set; }
        public int CameraBordersOffset { get; set; }
        public int[] CameraBorderSettings { get; set; }
        public int BackgroundTilesInfoOffset { get; set; }
        public List<List<BGSetting>> BGSettings { get; set; }
        public int ObjectTilesInfoOffset { get; set; }
        public List<List<ObjectSetting>> ObjectSettings { get; set; }
    }
}
