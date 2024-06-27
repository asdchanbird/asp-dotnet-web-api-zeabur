using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ChitChit.Areas.Identity.Data;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;
using Serilog;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.CodeAnalysis.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ChitChit.Hubs;
using StackExchange.Redis;
using ChitChit.Models;

var builder = WebApplication.CreateBuilder(args);

// 註冊服務: 序列化器設定
builder.Services.AddControllers()
        .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);


//var connectionString = builder.Configuration.GetConnectionString("ChitChitContextConnection") ?? throw new InvalidOperationException("Connection string 'ChitChitContextConnection' not found.");
//builder.Services.AddDbContext<ChitChitContext>(options =>
//    options.UseSqlServer(connectionString)
//);

var connectionString = builder.Configuration.GetConnectionString("MySQLContextConnection") ?? throw new InvalidOperationException("Connection string 'ChitChitContextConnection' not found.");
builder.Services.AddDbContext<chitchitContext>(options =>
    options.UseMySQL(connectionString)
);
// 註冊服務: Identity
builder.Services.AddDefaultIdentity<ChitChitUser>(options =>
{
    // 設置為 短驗證碼 不為長驗證碼
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;

    options.SignIn.RequireConfirmedAccount = true;
    // 密碼是否包含數字
    options.Password.RequireDigit = false;
    // 密碼是否包含小寫英文
    options.Password.RequireLowercase = false;
    // 密碼是否包含一個非字母數字字符
    options.Password.RequireNonAlphanumeric = false;
    // 密碼是否包含大寫字母
    options.Password.RequireUppercase = false;
    // 密碼是否包含不同字符數量
    options.Password.RequiredUniqueChars = 1;
    options.Password.RequiredLength = 1;
}).AddEntityFrameworkStores<chitchitContext>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
// 註冊服務： Swagger
builder.Services.AddSwaggerGen(s =>
{
    s.SwaggerDoc("ChatManagement", new OpenApiInfo
    {
        Title = "ChatManagement",
        Version = "v1",
        Description = "聊天室模組API"
    });
    s.SwaggerDoc("FriendManagement", new OpenApiInfo
    {
        Title = "好友管理",
        Version = "v1",
        Description = "好友模組API"
    });
    s.SwaggerDoc("SocialManagement", new OpenApiInfo
    {
        Title = "社群管理",
        Version = "v1",
        Description = "社群模組API"

    });
    s.SwaggerDoc("PersonalSettingManagement", new OpenApiInfo
    {
        Title = "個人化管理",
        Version = "v1",
        Description = "個人化模組API"

    });

    s.MapType<DateTime>(() => new OpenApiSchema { Type = "DateTime", Format = "date-time", Example = new OpenApiDateTime(DateTimeOffset.Now) });

    // 加入xml檔案到swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    s.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);


    // 加入Bearer Token：說明api如何受到保護
    s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        //選擇類型，type選擇http時，透過swagger畫面做認證時可以省略Bearer前綴詞(如下圖)
        Type = SecuritySchemeType.Http,
        //採用Bearer token
        Scheme = "Bearer",
        //bearer格式使用jwt
        BearerFormat = "JWT",
        //認證放在http request的header上
        In = ParameterLocation.Header,
        //描述
        Description = "JWT驗證描述"
    });
    s.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer"}
                },
            new string[] {}
        }
    });
});


// Serilog工具設置
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
    // 正常 Log 紀錄
    .WriteTo.File("Log/normalLog_.log",
        outputTemplate: "{Timestamp:yyyy/MM/dd HH:mm:ss} {Application} [{Level}] {Message}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 24 * 90
    )
    // 錯誤 Log 紀錄
    .WriteTo.Logger(c => c.Filter.ByIncludingOnly(e => e.Level == Serilog.Events.LogEventLevel.Error)
        .WriteTo.File("Log/errorLog_.log",
            outputTemplate: "{Timestamp:yyyy/MM/dd HH:mm:ss} {Application} [{Level}] {Message}{NewLine}{Exception}",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 24 * 90
        ))
    .CreateLogger();

// 模型驗證失敗 http 400 錯誤響應自訂義格式
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            string message = "DataStructureFail";
            // 自定義 HTTP 400 錯誤響應
            var problemDetails = new
            {
                state = "normal",
                message,
                result = new { }
            };
            // 設置
            var result = new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" },
            };
            // 回傳結果 - 資料結構錯誤
            Log.Information($"{context.HttpContext.Request.Path}->{message}");
            return result;
        };
    });


//// 註冊服務: Cookie 身份驗證機制
//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//.AddCookie(options =>
//{
//    // Cookie 設定為 HttpOnly 模式，增強安全性
//    options.Cookie.HttpOnly = true;
//    // 登入路徑設定
//    //options.LoginPath = new PathString("/PlayBackManagement/Auth/View");
//    // 存取被拒絕時的路徑設定
//    options.AccessDeniedPath = "/PlayBackManagement/User/View";
//    // 設定 Cookie 的過期時間為     
//    options.ExpireTimeSpan = TimeSpan.FromHours(8);
//})
// 啟用 JWT 驗證
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    // 當驗證失敗時，回應標頭會包含 WWW-Authenticate 標頭，這裡會顯示失敗的詳細錯誤原因
    options.IncludeErrorDetails = true; // 預設值為 true，有時會特別關閉

    // 允許 Token 的配置
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // 一般我們都會驗證 Issuer
        ValidateIssuer = false, // 發行者是否受驗證
        ValidIssuer = builder.Configuration.GetValue<string>("JWT:Issuer"),

        // 通常不驗證 Audience
        ValidateAudience = false, // 讀者是否受驗證

        // 一般都會驗證 Token 的有效期限
        ValidateLifetime = true, // 是否驗證到期時間

        // 如果 Token 中包含 Key 才需要驗證，一般都只有簽章而已
        ValidateIssuerSigningKey = false, // 是否驗證發行者的簽章金鑰

        // 取得或設定要用來驗證簽章的 key
        ClockSkew = TimeSpan.Zero, // 強制期限到期失去憑證
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("JWT:SignKey") ?? ""))
    };

    // 事件設置
    options.Events = new JwtBearerEvents
    {

        OnMessageReceived = context =>
        {
            // 取得從字符串參數的 token
            var accessToken = context.Request.Query["access_token"];

            // 要求路徑
            var path = context.HttpContext.Request.Path;
            // 如果
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs/chat")))
            {
                // 將查詢字符串獲取的訪問令牌設置為身分驗證令牌token
                context.Token = accessToken;
            }
            // 返回一個已完成任務
            return Task.CompletedTask;
        }
    };
});

// 註冊服務: Session
builder.Services.AddSession(options =>
{
    // 過期時間
    options.IdleTimeout = TimeSpan.FromMinutes(5);
    // 僅通過HTTP訪問Session Cookie，防止客戶端指令碼訪問
    options.Cookie.HttpOnly = true;
    // 確保Cookie即使使用者未同意也可用
    options.Cookie.IsEssential = true;
});

// 註冊服務: SignalR
builder.Services.AddSignalR();

// 註冊服務: CORS 跨源共用
builder.Services.AddCors(options =>
{
    // 開放所有Origins存取
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();

    });

});

// 註冊服務: Redis
try
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(
            new ConfigurationOptions()
            {
                EndPoints = { { "10.255.255.10", 6379 } }
            }
        )
     );

}
catch (Exception ex)
{
    Console.WriteLine( ex );
}


var app = builder.Build();

// 啟用 CORS 跨源共用
app.UseCors(); 

// Session中介軟體
app.UseSession();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();

    // 啟動Swagger
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/ChatManagement/swagger.json", "聊天室管理");
        c.SwaggerEndpoint("/swagger/FriendManagement/swagger.json", "好友管理");
        c.SwaggerEndpoint("/swagger/SocialManagement/swagger.json", "社群管理");
        c.SwaggerEndpoint("/swagger/PersonalSettingManagement/swagger.json", "個人化管理");

    });
}

app.UseHttpsRedirection();
app.UseAuthentication();;

app.UseAuthorization();

app.MapControllers();

app.MapHub<SampleHub>("/chatHub").AllowAnonymous();

// Web服務: 映射到 MVC 控制器和操作方法
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=View}/{id?}");


app.Run();
