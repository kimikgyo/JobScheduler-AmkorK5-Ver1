using Common.Interfaces;
using Common.Models.Jobs;
using System.Xml.Linq;

namespace Common.Models.Bases
{
    public class linkedDevice
    {
        public string id { get; set; }

        public override string ToString()
        {
            return
                $"id = {id,-5}";
        }
    }
}