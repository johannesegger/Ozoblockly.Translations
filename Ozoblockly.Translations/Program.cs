using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

const string defaultClusterId = "default";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .ConfigureHttpClient((context, handler) =>
    {
        handler.AutomaticDecompression = DecompressionMethods.GZip;
    })
    .LoadFromMemory(
        new[] {
            new RouteConfig {
                RouteId = "translation",
                Match = new RouteMatch { Methods = new[] { "GET" }, Path = "/editor/generated/{lang}/compressed.js" },
                ClusterId = defaultClusterId
            },
            new RouteConfig {
                RouteId = "unmodified",
                Match = new RouteMatch { Path = "/{**catchall}" },
                ClusterId = defaultClusterId
            }
        },
        new[] {
            new ClusterConfig {
                ClusterId = defaultClusterId,
                Destinations = new Dictionary<string, DestinationConfig> {
                    { "default-destination", new DestinationConfig { Address = "https://ozoblockly.com" } }
                }
            }
        })
    .AddTransforms(context =>
    {
        if (context.Route.RouteId == "translation")
        {
            context.AddResponseTransform(async responseContext =>
            {
                if (responseContext.ProxyResponse != null)
                {
                    var body = await responseContext.ProxyResponse.Content.ReadAsStringAsync();
                    responseContext.SuppressResponseBody = true;
                    if (responseContext.HttpContext.GetRouteValue("lang") is string lang)
                    {
                        body = ApplyTranslations(body, lang);
                    }
                    var bytes = Encoding.UTF8.GetBytes(body);
                    responseContext.HttpContext.Response.ContentLength = bytes.Length;
                    await responseContext.HttpContext.Response.Body.WriteAsync(bytes);
                }
            });
        }
    });

var app = builder.Build();
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use((context, next) =>
    {
        if (context.Request.Path == "/")
        {
            var lang = context.RequestServices
                .GetRequiredService<IConfiguration>()
                .GetValue<string>("DefaultLanguage");
            var url = lang != null ? $"/editor?lang={lang}" : "/editor";
            context.Response.Redirect(url);
            return Task.CompletedTask;
        }

        return next();
    });
});
app.Run();

static string ApplyTranslations(string body, string lang)
{
    try
    {
        return File.ReadAllLines($"{lang}.txt")
            .Select(line => line.Split(" ==> "))
            .Where(lineParts => lineParts.Length == 2 && lineParts[1] != "")
            .Select(lineParts =>
            {
                try
                {
                    var pattern = new Regex(lineParts[0]);
                    var replacement = lineParts[1];
                    return new { Pattern = new Regex(lineParts[0]), Replacement = lineParts[1] };
                }
                catch
                {
                    return null;
                }
            })
            .Where(v => v != null)
            .Aggregate(body, (b, x) => x!.Pattern.Replace(b, x.Replacement));
    }
    catch
    {
        return body;
    }
}
