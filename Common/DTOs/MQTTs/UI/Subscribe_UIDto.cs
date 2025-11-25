using Common.Models.Bases;

namespace Common.DTOs.MQTTs.UI
{
    public class Subscribe_UIDto
    {
        public string id { get; set; }
        public List<Parameter> parameters { get; set; }
    }
}
