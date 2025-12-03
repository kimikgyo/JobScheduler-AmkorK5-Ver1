using System.Text.Json.Serialization;

namespace Common.DTOs.Rests.Positions
{
    public class Response_PositionDto
    {
        [JsonPropertyOrder(1)] public string _id { get; set; }
        [JsonPropertyOrder(2)] public string positionId { get; set; }
        [JsonPropertyOrder(3)] public string source { get; set; }
        [JsonPropertyOrder(4)] public string mapId { get; set; }
        [JsonPropertyOrder(5)] public string name { get; set; }
        [JsonPropertyOrder(6)] public string type { get; set; }
        [JsonPropertyOrder(7)] public string subType { get; set; }
        [JsonPropertyOrder(8)] public double x { get; set; }
        [JsonPropertyOrder(9)] public double y { get; set; }
        [JsonPropertyOrder(10)] public double theta { get; set; }
        [JsonPropertyOrder(11)] public string groupId { get; set; }
        [JsonPropertyOrder(12)] public bool isDisplayed { get; set; }
        [JsonPropertyOrder(13)] public bool isEnabled { get; set; }
        [JsonPropertyOrder(14)] public bool isOccupied { get; set; }
        [JsonPropertyOrder(15)] public string linkedFacility { get; set; }
        [JsonPropertyOrder(16)] public string linkedRobotId { get; set; }
        [JsonPropertyOrder(17)] public bool hasCharger { get; set; }
        [JsonPropertyOrder(18)] public string nodeType { get; set; }
        [JsonPropertyOrder(19)] public DateTime createdAt { get; set; }
        [JsonPropertyOrder(20)] public DateTime updatedAt { get; set; }
        [JsonPropertyOrder(21)] public string createdBy { get; set; }
        [JsonPropertyOrder(22)] public string updatedBy { get; set; }

        public override string ToString()
        {
            return

                $" _id = {_id,-5}" +
                $",positionId = {positionId,-5}" +
                $",source = {source,-5}" +
                $",mapId = {mapId,-5}" +
                $",name = {name,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",x = {x,-5}" +
                $",y = {y,-5}" +
                $",theta = {theta,-5}" +
                $",groupId = {groupId,-5}" +
                $",isDisplayed = {isDisplayed,-5}" +
                $",isEnabled = {isEnabled,-5}" +
                $",isOccupied = {isOccupied,-5}" +
                $",linkedFacility = {linkedFacility,-5}" +
                $",linkedRobotId = {linkedRobotId,-5}" +
                $",hasCharger = {hasCharger,-5}" +
                $",nodeType = {nodeType,-5}" +
                $",createdAt = {createdAt,-5}" +
                $",updatedAt = {updatedAt,-5}" +
                $",createdBy = {createdBy,-5}" +
                $",updatedBy = {updatedBy,-5}";
        }

        //public string ToJson(bool indented = false)
        //{
        //    return JsonSerializer.Serialize(this, new JsonSerializerOptions
        //    {
        //        IncludeFields = true,
        //        WriteIndented = indented
        //    });
        //}
    }
}