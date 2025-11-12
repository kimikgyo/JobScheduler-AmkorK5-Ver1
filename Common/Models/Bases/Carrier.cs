using Common.Interfaces;
using System.Reflection.Emit;
using System.Xml.Linq;

namespace Common.Models.Bases
{
    public class Carrier
    {
        public string carrierId { get; set; }
        public string name { get; set; }
        public string location { get; set; }
        public string installedTime { get; set; }
        public string workerId { get; set; }

        public override string ToString()
        {
            return
                $"carrierId = {carrierId,-5}" +
                $"name = {name,-5}" +
                $"location = {location,-5}" +
                $"installedTime = {installedTime,-5}" +
                $"workerId = {workerId,-5}";
        }
    }
}