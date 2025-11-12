using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.DTOs.Bases
{
    public class MissionTemplateDto
    {
        [JsonPropertyOrder(1)] public string service { get; set; }
        [JsonPropertyOrder(2)] public string name { get; set; }
        [JsonPropertyOrder(3)] public string type { get; set; }
        [JsonPropertyOrder(4)] public string subType { get; set; }
        [JsonPropertyOrder(5)] public bool isLook { get; set; }
        [JsonPropertyOrder(6)] public List<ParametaDto> parameters { get; set; }
        [JsonPropertyOrder(7)] public List<PreReportDto> preReports { get; set; }
        [JsonPropertyOrder(8)] public List<PostReportDto> postReports { get; set; }

        public override string ToString()
        {
            string parametersStr;
            string preReportsStr;
            string postReportsStr;

            if (parameters != null && parameters.Count > 0)
            {
                // 리스트 안의 Parameta 각각을 { ... } 모양으로 변환
                var items = parameters
                    .Select(p => $"{{ key={p.key}, value={p.value} }}");

                // 여러 개 항목을 ", " 로 이어붙임
                parametersStr = string.Join(", ", items);
            }
            else
            {
                // 값이 없으면 빈 중괄호로 표시
                parametersStr = "{}";
            }

            if (preReports != null && preReports.Count > 0)
            {
                // 리스트 안의 Parameta 각각을 { ... } 모양으로 변환
                var items = preReports
                    .Select(p => $"{{ ceid={p.ceid}, eventName={p.eventName},rptid = {p.rptid} }}");

                // 여러 개 항목을 ", " 로 이어붙임
                preReportsStr = string.Join(", ", items);
            }
            else
            {
                preReportsStr = "{}";
            }

            if (postReports != null && postReports.Count > 0)
            {
                // 리스트 안의 Parameta 각각을 { ... } 모양으로 변환
                var items = postReports
                    .Select(p => $"{{ ceid={p.ceid}, eventName={p.eventName},rptid = {p.rptid} }}");

                // 여러 개 항목을 ", " 로 이어붙임
                postReportsStr = string.Join(", ", items);
            }
            else
            {
                postReportsStr = "{}";
            }

            return
                $"service = {service,-5}" +
                $",name = {name,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",isLook = {isLook,-5}" +
                $",parameters = {parametersStr,-5}" +
                $",preReports = {preReportsStr,-5}" +
                $",postReports = [{postReportsStr,-5}]";
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

    public class ParametaDto
    {
        [JsonPropertyOrder(1)] public string key { get; set; }
        [JsonPropertyOrder(2)] public string value { get; set; }

        public override string ToString()
        {
            return
                $"key = {key,-5}" +
                $",value = {value,-5}";
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

    public class PreReportDto
    {
        [JsonPropertyOrder(1)] public int ceid { get; set; }
        [JsonPropertyOrder(2)] public string eventName { get; set; }
        [JsonPropertyOrder(3)] public int rptid { get; set; }

        public override string ToString()
        {
            return
                $"ceid = {ceid,-5}" +
                $",eventName = {eventName,-5}" +
                $",rptid = {rptid,-5}";
        }
    }

    public class PostReportDto
    {
        [JsonPropertyOrder(1)] public int ceid { get; set; }
        [JsonPropertyOrder(2)] public string eventName { get; set; }
        [JsonPropertyOrder(3)] public int rptid { get; set; }

        public override string ToString()
        {
            return
                $"ceid = {ceid,-5}" +
                $",eventName = {eventName,-5}" +
                $",rptid = {rptid,-5}";
        }
    }
}