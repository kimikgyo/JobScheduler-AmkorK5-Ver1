using Common.DTOs.Rests.Nodes_Edges;

namespace JOB.Mappings.Jobs
{
    public class RoutesPlan_Mapping
    {
        public Request_Routes_Plan Request(string sourceId, string destinationId)
        {
            var request = new Request_Routes_Plan
            {
                startPositionId = sourceId,
                targetPositionId = destinationId
            };
            return request;
        }
    }
}