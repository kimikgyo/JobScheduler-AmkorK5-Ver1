using Common.Templates;

namespace JOB.Mappings.Templates
{
    public class MissionTemplate_Mapping
    {
        public MissionTemplate Create(object value)
        {
            if (value is MissionTemplate_Single single)
            {
                return CreateFrom(single);
            }
            else if (value is MissionTemplate_Group group)
            {
                return CreateFrom(group);
            }
            else
            {
                throw new ArgumentException("지원하지 않는 타입입니다.");
            }
        }

        private MissionTemplate CreateFrom(dynamic value)
        {
            return new MissionTemplate()
            {
                name = value.name,
                service = value.service,
                type = value.type,
                subType = value.subType,
                isLook = value.isLook,
                parameters = value.parameters,
                preReports = value.preReports,
                postReports = value.postReports
            };
        }
    }
}