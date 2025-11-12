using Data.Interfaces;
using Data.Repositorys.Bases;
using JOB.JobQueues.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs;
using JOB.MQTTs.Interfaces;
using JobScheduler.Services;
using log4net;
using log4net.Config;
using Microsoft.OpenApi.Models;
using System.Reflection;

// log4net 설정 로딩
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
var logPath = Path.Combine(AppContext.BaseDirectory, "Config", "log4net.config");
XmlConfigurator.Configure(logRepository, new FileInfo(logPath));


var EventLogger = LogManager.GetLogger("Event");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 1) CORS 정책 등록 -----------------------------------------------------------
    builder.Services.AddCors(options =>
    {
        // "AllowAll" 이라는 이름의 정책을 등록
        options.AddPolicy("AllowAll", policy =>
        {
            // 모든 출처(Origin) 허용
            policy.AllowAnyOrigin()
                  // 모든 HTTP 메서드 허용 (GET, POST, PUT, DELETE 등)
                  .AllowAnyMethod()
                  // 모든 요청 헤더 허용 (Content-Type, Authorization 등)
                  .AllowAnyHeader();
        });
    });


    //Kestrel 설정을 appsettings.json에서 읽어오도록 설정
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Configure(builder.Configuration.GetSection("Kestrel"));
    });

    #region 의존성 주입 [DI설명 및 설정]

    //의존성 주입에 대한 설명
    //Singleton 애플리케이션 시작 시 1번	모든 요청에서 하나의 인스턴스 공유	설정 캐시, DB 커넥션 풀 등
    //Scoped	HTTP 요청마다 1번 생성	요청(Request) 동안 동일 객체	웹 API 서비스 등
    //Transient	매번 새로 생성	요청할 때마다 새 인스턴스	가벼운 서비스, 비상태 처리
    // DI 컨테이너에 싱글톤으로 등록

    builder.Services.AddSingleton<IUnitOfWorkRepository, UnitOfWorkRepository>();
    builder.Services.AddSingleton<IUnitOfWorkMapping, UnitOfWorkMapping>();
    builder.Services.AddSingleton<IUnitOfWorkJobMissionQueue, UnitOfWorkJobMissionQueue>();
    builder.Services.AddSingleton<IUnitofWorkMqttQueue, UnitofWorkMqttQueue>();
    builder.Services.AddSingleton<IMqttWorker, MqttWorker>();
    builder.Services.AddSingleton<MainService>();
    #endregion 의존성 주입 [DI설명 및 설정]

    // builder.Services.AddControllers():
    // ASP.NET Core 웹 API에서 컨트롤러를 사용하기 위해 서비스 컬렉션에 컨트롤러를 추가합니다. 이 메서드는 기본적으로 JSON 형식으로 응답을 반환하도록 설정합니다.
    // .AddJsonOptions(o => { ... }):
    // JSON 직렬화 옵션을 설정하기 위해 사용됩니다. 이 메서드는 JsonSerializerOptions 객체를 매개변수로 받습니다.
    // o.JsonSerializerOptions.IncludeFields = true;:
    // JSON 직렬화 시 필드를 포함하도록 설정합니다. 기본적으로 필드는 직렬화에서 제외됩니다. 이 옵션을 true로 설정하면 필드도 JSON 응답에 포함됩니다
    builder.Services.AddControllers().AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.IncludeFields = true;
    });
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    // "v1"은 SwaggerDoc 등록 이름과 정확히 일치해야 함
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "JobScheduler",
            Version = "1.0"
        });
        c.EnableAnnotations(); // Annotations 표시 활성화
    });

    var app = builder.Build();

    // AddSingleton<MainService>()만 등록하면 요청될 때까지 생성안됨 1번은 생성해줘야 진행됨.
    using (var scope = app.Services.CreateScope())
    {
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        //Config 데이터 Load
        ConfigData.Load(config);

        var Repository = scope.ServiceProvider.GetRequiredService<IUnitOfWorkRepository>();
        var Mapping = scope.ServiceProvider.GetRequiredService<IUnitOfWorkMapping>();
        var jobMissionQueue = scope.ServiceProvider.GetRequiredService<IUnitOfWorkJobMissionQueue>();

        //MQTT메세지 Queue
        var mqttMessageQueue = scope.ServiceProvider.GetRequiredService<IUnitofWorkMqttQueue>();
        //MQTT실행
        var MQTT = scope.ServiceProvider.GetRequiredService<IMqttWorker>();

        // new 금지!  DI에서 싱글톤 인스턴스를 꺼내 쓰세요.
        var main = scope.ServiceProvider.GetRequiredService<MainService>();
    }
    // Configure the HTTP request pipeline.
    //if (app.Environment.IsDevelopment())
    //{
    app.UseSwagger();
    //app.UseSwaggerUI();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        // Swagger JSON 엔드포인트 등록 (필수)
        // 이 부분에서 Swagger 문서의 URL과 표시 이름을 지정합니다.
        // "/swagger/v1/swagger.json"은 기본 경로이고,
        // "Sorted API v1"은 Swagger UI 상단 드롭다운에 보이는 이름입니다.
        c.SwaggerEndpoint("./v1/swagger.json", "v1");

        // operationsSorter는 Swagger UI에서 HTTP 메서드(GET, POST 등) 또는 이름 기준으로
        // API 엔드포인트를 정렬하도록 지정하는 옵션입니다.
        // 기본값은 등록 순서이며, 아래처럼 설정하지 않으면 정렬이 되지 않습니다.
        // method: GET → POST → PUT → DELETE 순서로 정렬
        // alpha:  경로 문자열을 알파벳 순으로 정렬
        // 최신 Swashbuckle에서는 아래와 같이 설정해야 정상 적용됩니다.
        c.ConfigObject.AdditionalItems["operationsSorter"] = "method";

        // 📌 Swagger UI의 <head> 영역에 HTML을 직접 삽입할 수 있도록 하는 속성입니다.
        // 아래는 HTML <style> 태그를 삽입하여 특정 UI 요소를 CSS로 숨깁니다.
        c.HeadContent = @"
        <style>
            /* Swagger UI에서 'Schemas(models)' 섹션을 통째로 숨깁니다 */

            /* .models는 왼쪽 하단에 자동 생성되는 스키마 목록 영역을 의미합니다 */
            .swagger-ui .models {
                display: none !important;
            }

            /* section.models는 'Schemas'라는 제목 텍스트 부분을 의미합니다 */
            .swagger-ui section.models {
                display: none !important;
            }
        </style>";
    });
    //}
    // 등록한 CORS 정책("AllowAll")을 전역으로 적용
    app.UseCors("AllowAll");
    // 2) 미들웨어 파이프라인 구성 ------------------------------------------------
    //app.UseHttpsRedirection();
    // 인증/인가가 필요하면 여기 추가
    app.UseAuthentication();
    app.UseAuthorization();
    // 컨트롤러 엔드포인트 매핑
    app.MapControllers();


    EventLogger.Info($"=== Program state ===");
    app.Run();
}
catch (Exception ex)
{
    EventLogger.Info($"=== Program Error Msg = {ex.Message} ===");

}
