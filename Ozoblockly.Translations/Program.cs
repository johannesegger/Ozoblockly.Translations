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
                RouteId = "translation-de",
                Match = new RouteMatch { Methods = new[] { "GET" }, Path = "/editor/generated/de/compressed.js" },
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
        if (context.Route.RouteId == "translation-de")
        {
            context.AddResponseTransform(async responseContext =>
            {
                if (responseContext.ProxyResponse != null)
                {
                    var body = await responseContext.ProxyResponse.Content.ReadAsStringAsync();
                    responseContext.SuppressResponseBody = true;
                    body = ApplyTranslations(body);
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
            context.Response.Redirect("/editor?lang=de");
            return Task.CompletedTask;
        }

        return next();
    });
});
app.Run();

static string ApplyTranslations(string body)
{
    var replacements = new[] {
        (new Regex("""(?<=")Movement(?=")"""), "Bewegung"),
        (new Regex("""(?<=")Line Navigation(?=")"""), "Linien"),
        (new Regex("""(?<=")Light Effects(?=")"""), "Licht"),
        (new Regex("""(?<=")Sounds(?=")"""), "Töne"),
        (new Regex("""(?<=")Sensors(?=")"""), "Sensoren"),
        (new Regex("""(?<=")Button(?=")"""), "Knopf"),
        (new Regex("""(?<=")Wait(?=")"""), "Warten"),
        (new Regex("""(?<=")Timing(?=")"""), "Zeit"),
        (new Regex("""(?<=")Terminate(?=")"""), "Beenden"),
        (new Regex("""(?<=")Logic(?=")"""), "Logik"),
        (new Regex("""(?<=")Loops(?=")"""), "Schleifen"),
        (new Regex("""(?<=")Math(?=")"""), "Mathematik"),
        (new Regex("""(?<=")Variables(?=")"""), "Variablen"),
        (new Regex("""(?<=")Functions(?=")"""), "Funktionen"),
        (new Regex("""(?<=")Arrays(?=")"""), "Datenfelder"),
        (new Regex("""(?<=")List(?=")"""), "Liste"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_MOVE_TITLE=")move(?=")"""), "Bewege dich"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_MOVE_OPERATOR_FORWARD=")forward(?=")"""), "vorwärts"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_MOVE_OPERATOR_BACKWARD=")backward(?=")"""), "rückwärts"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_DISTANCE_TITLE=")distance(?=")"""), "Strecke"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_DISTANCE_OPERATOR_STEP=")step(?=")"""), "Schritt"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_DISTANCE_OPERATOR_STEPS=")steps(?=")"""), "Schritte"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_DISTANCE_OPERATOR_STEPS_FEW=")steps(?=")"""), "Schritte"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_SPEED_TITLE=")speed(?=")"""), "Geschwindigkeit"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_SPEED_OPERATOR_SLOW=")slow(?=")"""), "langsam"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_SPEED_OPERATOR_MEDIUM=")medium(?=")"""), "mittel"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_SPEED_OPERATOR_FAST=")fast(?=")"""), "schnell"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_SPEED_OPERATOR_VERY_FAST=")very fast(?=")"""), "sehr schnell"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_ROTATE_TITLE=")rotate(?=")"""), "Drehe dich"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_ROTATE_OPERATOR_SLIGHT_LEFT=")slight left(?=")"""), "leicht nach links"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_ROTATE_OPERATOR_LEFT=")left(?=")"""), "nach links"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_ROTATE_OPERATOR_SLIGHT_RIGHT=")slight right(?=")"""), "leicht nach rechts"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_ROTATE_OPERATOR_RIGHT=")right(?=")"""), "nach rechts"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_ROTATE_OPERATOR_U_TURN_LEFT=")u-turn left(?=")"""), "nach links umdrehen"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_ROTATE_OPERATOR_U_TURN_RIGHT=")u-turn right(?=")"""), "nach rechts umdrehen"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_ZIGZAG_TITLE=")zigzag(?=")"""), "Fahre Zick-Zack"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_SKATE_TITLE=")skate(?=")"""), "Skate"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_SPIN_TITLE=")spin(?=")"""), "Kreisle"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_SMALL_CIRCLE_TITLE=")small circle(?=")"""), "Kleiner Kreis"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_BIG_CIRCLE_TITLE=")big circle(?=")"""), "Großer Kreis"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_SECOND_UNIT=")second(?=")"""), "Sekunde"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_SECONDS_UNIT_FEW=")seconds(?=")"""), "Sekunden"),
        (new Regex("""(?<=OZOBOT_MOVEMENT_SECONDS_UNIT=")seconds(?=")"""), "Sekunden"),
        (new Regex("""(?<=OZOBOT_LIGHT_EFFECTS_TURN_OFF_LEDS_TITLE=")turn top light off(?=")"""), "Schalte oberes Licht aus"),
        (new Regex("""(?<=OZOBOT_LIGHT_EFFECTS_SET_LIGHT_COLOR_TITLE=")set top light color(?=")"""), "Oberes Licht ändern auf"),
        (new Regex("""(?<=OZOBOT_LIGHT_EFFECTS_POLICE_CAR_LIGHTS_TITLE=")police car lights(?=")"""), "Polizeiauto"),
        (new Regex("""(?<=OZOBOT_LIGHT_EFFECTS_RAINBOW_TITLE=")rainbow(?=")"""), "Regenbogen"),
        (new Regex("""(?<=OZOBOT_LIGHT_EFFECTS_TRAFFIC_LIGHTS_TITLE=")traffic lights(?=")"""), "Ampel"),
        (new Regex("""(?<=OZOBOT_LIGHT_EFFECTS_DISCO_TITLE=")disco(?=")"""), "Disco"),
        (new Regex("""(?<=OZOBOT_LIGHT_EFFECTS_CHRISTMAS_TREE_TITLE=")christmas tree(?=")"""), "Weihnachtsbaum"),
        (new Regex("""(?<=OZOBOT_LIGHT_EFFECTS_FIREWORK_TITLE=")firework(?=")"""), "Feuerwerk"),
        (new Regex("""(?<=OZOBOT_TIMING_SYSTEM_DELAY_TITLE=")wait(?=")"""), "Warte"),
        (new Regex("""(?<=OZOBOT_TIMING_DELAY_SIMPLE_UNIT_SECONDS=")second\(s\)(?=")"""), "Sekunde(n)"),
        (new Regex("""(?<=OZOBOT_LOOPS_REPEAT_FOREVER_TITLE=")repeat forever(?=")"""), "Wiederhole"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_PLAY_FILE_SFX_BY_SELECTION_TITLE=")play(?=")"""), "Spiele"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_PLAY_FILE_SFX_BY_SELECTION_HAPPY=")happy(?=")"""), "fröhlich"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_PLAY_FILE_SFX_BY_SELECTION_SAD=")sad(?=")"""), "traurig"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_PLAY_FILE_SFX_BY_SELECTION_SURPRISED=")surprised(?=")"""), "überrascht"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_PLAY_FILE_SFX_BY_SELECTION_LAUGH=")laugh(?=")"""), "lachen"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_SAY_COLOR_BY_SELECTION_TITLE=")say color(?=")"""), "Sag Farbe"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_SAY_COLOR_RED=")red(?=")"""), "rot"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_SAY_COLOR_GREEN=")green(?=")"""), "grün"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_SAY_COLOR_BLUE=")blue(?=")"""), "blau"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_SAY_COLOR_BLACK=")black(?=")"""), "schwarz"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_SAY_COLOR_WHITE=")white(?=")"""), "weiß"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_SAY_COLOR_YELLOW=")yellow(?=")"""), "gelb"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_SAY_COLOR_MAGENTA=")magenta(?=")"""), "magenta"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_SAY_COLOR_CYAN=")cyan(?=")"""), "zyan"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_SAY_DIRECTION_BY_SELECTION_TITLE=")say direction(?=")"""), "Sag Richtung"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_SAY_DIRECTION_STRAIGHT=")forward(?=")"""), "vorwärts"),
        (new Regex("""(?<=OZOBOT_LINE_NAVIGATION_CHOOSE_WAY_AT_INTERSECTION_OPERATOR_BACK=")back(?=")"""), "zurück"),
        (new Regex("""(?<=OZOBOT_EVO_SOUND_SAY_NUMBER_BY_SELECTION_TITLE=")say number(?=")"""), "Sag Zahl"),
        (new Regex("""(?<=OZOBOT_EVO_SOUNDS_PLAY_NOTE_SIMPLE_TITLE=")play note(?=")"""), "Spiel Note")
    };

    return replacements.Aggregate(body, (string b, (Regex pattern, string replacement) x) => x.pattern.Replace(b, x.replacement));
}
