using Common.Models.Jobs;
using Common.Templates;

namespace Common.Models.Queues
{
    public class Create_Mission
    {
        public Job job { get; set; }
        public MissionTemplate missionTemplate { get; set; }
        public Position position { get; set; } 
        public Worker worker{ get; set; }
        public int seq { get; set; }
    }
}