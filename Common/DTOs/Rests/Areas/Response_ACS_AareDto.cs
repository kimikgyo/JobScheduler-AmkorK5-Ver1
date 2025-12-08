namespace Common.DTOs.Rests.Areas
{
    public class Response_ACS_AareDto
    {
        public string areaId { get; set; }
        public string mapId { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string subType { get; set; }
        public string groupId { get; set; }
        public string linkedNode { get; set; }
        public string linkedFacility { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double theta { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public bool isDisplayed { get; set; }
        public bool isEnabled { get; set; }
        public DateTime careatedAt { get; set; }
        public DateTime updatedAt { get; set; }


        public override string ToString()
        {
            return
                $"areaId = {areaId,-5}" +
                $",mapId = {mapId,-5}" +
                $",name = {name,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",groupId = {groupId,-5}" +
                $",linkedNode = {linkedNode,-5}" +
                $",linkedFacility = {linkedFacility,-5}" +
                $",x = {x,-5}" +
                $",y = {y,-5}" +
                $",theta = {theta,-5}" +
                $",width = {width,-5}" +
                $",height = {height,-5}" +
                $",isDisplayed = {isDisplayed,-5}" +
                $",isEnabled = {isEnabled,-5}" +
                $",careatedAt = {careatedAt,-5}" +
                $",updatedAt = {updatedAt,-5}";
        }
    }
}