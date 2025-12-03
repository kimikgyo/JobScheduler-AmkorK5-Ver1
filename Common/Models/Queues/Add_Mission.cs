using Common.Models.Jobs;
using Common.Templates;

namespace Common.Models.Queues
{
    public class Add_Mission
    {
        public Job job { get; set; }
        public MissionTemplate missionTemplate { get; set; }
        public Position position { get; set; } 
        public int seq { get; set; }
    }
}