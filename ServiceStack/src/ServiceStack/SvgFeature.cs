#if NETCORE        
using ServiceStack.Host;
#else
using System.Web;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Script;
using ServiceStack.Templates;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class SvgFeature : IPlugin, IPostInitPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.Svg;
        /// <summary>
        /// RequestLogs service Route, default is /metadata/svg
        /// </summary>
        public string RoutePath { get; set; } = "/metadata/svg";

        /// <summary>
        /// Custom Validation for SVG Metadata Page, return false to deny access, e.g. only allow in DebugMode with:
        /// ValidateFn = req => HostContext.DebugMode
        /// </summary>
        public Func<IRequest, bool> ValidateFn { get; set; }

        public void Register(IAppHost appHost)
        {
            appHost.RawHttpHandlers.Add(req => req.PathInfo == RoutePath
                ? (string.IsNullOrEmpty(req.QueryString["id"]) || string.IsNullOrEmpty(req.QueryString["format"]) 
                    ? new SharpPageHandler(HtmlTemplates.GetSvgTemplatePath()) {
                        ValidateFn = ValidateFn,
                        Context = SharpPageHandler.NewContext(appHost),
                    }
                    : (IHttpHandler) new SvgFormatHandler {
                        Id = req.QueryString["id"], 
                        Format = req.QueryString["format"], 
                        Fill = req.QueryString["fill"],
                    })
                : req.PathInfo.StartsWith(RoutePath)
                  ? new SvgFormatHandler(req.PathInfo.Substring(RoutePath.Length+1)) {
                      Fill = req.QueryString["fill"]
                  }
                  : null);

            var btnSvgCssFile = appHost.VirtualFileSources.GetFile("/css/buttons-svg.css");
            if (btnSvgCssFile != null)
            {
                var btnSvgCss = btnSvgCssFile.ReadAllText();
                foreach (var name in new[]{ "svg-auth", "svg-icons" })
                {
                    if (Svg.CssFiles.ContainsKey(name) && !Svg.AppendToCssFiles.ContainsKey(name))
                    {
                        Svg.AppendToCssFiles[name] = btnSvgCss;
                    }
                }
            }

            appHost.ConfigurePlugin<MetadataFeature>(
                feature => feature.AddDebugLink(RoutePath, "SVG Images"));
        }

        static void AppendEntry(StringBuilder sb, string name, string dataUri)
        {
            sb.Append(".svg-").Append(name).Append(", .fa-").Append(name).AppendLine(" {");
            sb.Append("background-image: url(\"").Append(dataUri.Replace("\"","'")).AppendLine("\");");
            sb.AppendLine("}");
        }

        public static void WriteDataUris(StringBuilder sb, List<string> dataUris)
        {
            foreach (var name in dataUris)
            {
                AppendEntry(sb, name, Svg.GetDataUri(name));
            }
        }

        public static void WriteAdjacentCss(StringBuilder sb, List<string> dataUris, Dictionary<string, string> adjacentCssRules)
        {
            foreach (var entry in adjacentCssRules)
            {
                var i = 0;
                foreach (var name in dataUris)
                {
                    if (i++ > 0)
                        sb.Append(", ");
                    sb.Append(entry.Key).Append(name);
                }
                sb.Append(" { ").Append(entry.Value).AppendLine(" }");
            }
        }

        public static void WriteSvgCssFile(IVirtualFiles vfs, string name, 
            List<string> dataUris, 
            Dictionary<string, string> adjacentCssRules = null, 
            Dictionary<string, string> appendToCssFiles = null)
        {
            var sb = StringBuilderCache.Allocate();
            WriteDataUris(sb, dataUris);

            if (adjacentCssRules != null)
            {
                WriteAdjacentCss(sb, dataUris, adjacentCssRules);
            }

            if (appendToCssFiles != null)
            {
                if (appendToCssFiles.TryGetValue(name, out var suffix))
                {
                    sb.AppendLine(suffix);
                }
            }

            var css = StringBuilderCache.ReturnAndFree(sb);
            
            if (Svg.CssFillColor.TryGetValue(name, out var fillColor))
            {
                css = Svg.Fill(css, fillColor);
            }
            
            vfs.WriteFile($"/css/{name}.css", css);
        }

        public void AfterPluginsLoaded(IAppHost appHost)
        {
            var memFs = appHost.VirtualFileSources.GetMemoryVirtualFiles();
            if (memFs == null)
                return;

            appHost.AfterInitCallbacks.Add(host => 
            {
                foreach (var cssFile in Svg.CssFiles)
                {
                    WriteSvgCssFile(memFs, cssFile.Key, cssFile.Value, Svg.AdjacentCssRules, Svg.AppendToCssFiles);
                }
            });
        }
    }

    public class SvgFormatHandler : HttpAsyncTaskHandler
    {
        public string Id { get; set; }
        public string Format { get; set; }
        
        public string Fill { get; set; }

        public SvgFormatHandler() {}

        public SvgFormatHandler(string fileName) //name.svg, name.datauri, name.css
        {
            Id = fileName.LeftPart('.');
            Format = fileName.RightPart('.');
        }

        public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                return;

            var svg = Svg.GetImage(Id, Fill);
            if (svg == null)
            {
                httpRes.StatusCode = 404;
                httpRes.StatusDescription = "SVG Image was not found";
            }
            else if (Format == "svg")
            {
                httpRes.ContentType = MimeTypes.ImageSvg;
                await httpRes.WriteAsync(svg);
            }
            else if (Format == "css")
            {
                httpRes.ContentType = "text/css";
                var css = $".svg-{Id} {{\n  {Svg.InBackgroundImageCss(Svg.GetImage(Id, Fill))}\n}}\n";
                await httpRes.WriteAsync(css);
            }
            else if (Format == "datauri")
            {
                var dataUri = Svg.GetDataUri(Id, Fill);
                httpRes.ContentType = MimeTypes.PlainText;
                await httpRes.WriteAsync(dataUri);
            }
            else
            {
                httpRes.StatusCode = 400;
                httpRes.StatusDescription = "Unknown format, valid formats: svg, css, datauri";
            }
            await httpRes.EndRequestAsync();
        }
    }

    public static class Svg
    {
        public static string LightColor { get; set; } = "#dddddd";
        public static string[] FillColors { get; set; } = { "#ffffff", "#556080" };

        public static Dictionary<string, List<string>> CssFiles { get; set; } = new Dictionary<string, List<string>> {
            ["svg-auth"]  = new() { "servicestack", "apple", "twitter", "github", "google", "facebook", "microsoft", "linkedin", },
            ["svg-icons"] = new() { "male", "female", "male-business", "female-business", "male-color", "female-color", "users", },
        };

        public static Dictionary<string, string> CssFillColor { get; set; } = new();
        
        public static Dictionary<string, string> AppendToCssFiles { get; set; } = new();

        public static Dictionary<string,string> AdjacentCssRules { get; set; } = new() {
            //[".btn-md .fa-"] = "css", // Generates: .btn-md .fa-svg1, .btn-md .fa-svg2 { css } 
        };

        const string LogoPrefix = "<svg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'> <style> .path{} </style> ";
        const string IconPrefix = "<svg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'> <style> .path{} </style> ";

        public static Dictionary<string,string> Images { get; set; } = new() {
            [Logos.ServiceStack]   = LogoPrefix + "<g id='servicestack-svg'><path fill='#ffffff' class='path' stroke='null' d='m16.564516,43.33871c16.307057,2.035887 54.629638,20.41875 60.67742,46.306452l-78.241936,0c19.859879,-1.616734 36.825605,-27.344758 17.564516,-46.306452zm6.387097,-30.33871c6.446976,7.105645 9.520766,16.74617 9.26129,26.666129c16.546573,6.726411 41.376412,24.690121 46.625807,49.979033l19.161291,0c-8.123589,-43.132863 -54.529839,-73.551412 -75.048388,-76.645162z' /></g></svg>",
            [Logos.Apple]          = LogoPrefix + "<g id='apple-svg'><path fill='#ffffff' class='path' stroke='null' d='M46.122,25.028C50.231,25.028 55.381,22.165 58.449,18.348C61.226,14.888 63.252,10.057 63.252,5.225C63.252,4.569 63.194,3.913 63.078,3.376C58.506,3.555 53.009,6.537 49.71,10.534C47.106,13.576 44.733,18.348 44.733,23.239C44.733,23.955 44.849,24.671 44.907,24.909C45.196,24.969 45.659,25.028 46.122,25.028ZM31.655,97.203C37.268,97.203 39.756,93.326 46.759,93.326C53.877,93.326 55.439,97.084 61.689,97.084C67.824,97.084 71.932,91.238 75.81,85.512C80.15,78.951 81.944,72.509 82.06,72.21C81.655,72.091 69.907,67.14 69.907,53.242C69.907,41.193 79.166,35.765 79.687,35.348C73.553,26.281 64.236,26.043 61.689,26.043C54.803,26.043 49.189,30.337 45.659,30.337C41.84,30.337 36.805,26.281 30.844,26.281C19.502,26.281 7.986,35.944 7.986,54.197C7.986,65.53 12.268,77.519 17.534,85.273C22.048,91.835 25.983,97.203 31.655,97.203Z' /></g></svg>",
            [Logos.Twitter]        = LogoPrefix + "<g id='twitter-svg'><path fill='#ffffff' class='path' stroke='null' d='m32.167025,90.818083c37.320006,0 57.741133,-30.948298 57.741133,-57.741133c0,-0.870668 0,-1.741336 -0.039576,-2.612005c3.957583,-2.84946 7.40068,-6.45086 10.131412,-10.52717c-3.640976,1.622609 -7.558983,2.691156 -11.674869,3.205642c4.195038,-2.493277 7.40068,-6.490436 8.944137,-11.239535c-3.918007,2.334974 -8.271348,3.997159 -12.90172,4.907403c-3.720128,-3.957583 -8.983713,-6.411284 -14.80136,-6.411284c-11.199959,0 -20.3024,9.10244 -20.3024,20.3024c0,1.583033 0.197879,3.12649 0.514486,4.630372c-16.859303,-0.831092 -31.818966,-8.944137 -41.83165,-21.212644c-1.741336,3.007763 -2.730732,6.490436 -2.730732,10.210564c0,7.044497 3.6014,13.257902 9.023289,16.898879c-3.32437,-0.118727 -6.45086,-1.028972 -9.181592,-2.532853c0,0.079152 0,0.158303 0,0.277031c0,9.814805 7.004922,18.046578 16.265665,19.906642c-1.701761,0.47491 -3.482673,0.712365 -5.342737,0.712365c-1.306002,0 -2.572429,-0.118727 -3.79928,-0.356182c2.572429,8.073469 10.091836,13.930692 18.956822,14.088995c-6.965346,5.461464 -15.711604,8.706682 -25.209803,8.706682c-1.622609,0 -3.245218,-0.079152 -4.828251,-0.277031c8.944137,5.698919 19.629611,9.062865 31.067025,9.062865' /></g></svg>",
            [Logos.GitHub]         = LogoPrefix + "<g id='github-svg'><path fill='#ffffff' class='path' stroke='null' d='m49.974605,1.297c-27.058469,0 -48.974605,21.928379 -48.974605,48.974605c0,21.642694 14.031224,39.995927 33.486386,46.464656c2.44873,0.461178 3.346598,-1.052954 3.346598,-2.354862c0,-1.163147 -0.040812,-4.244466 -0.061218,-8.325683c-13.623103,2.954801 -16.496279,-6.570759 -16.496279,-6.570759c-2.228345,-5.652486 -5.448425,-7.162536 -5.448425,-7.162536c-4.436283,-3.036425 0.342822,-2.975207 0.342822,-2.975207c4.917867,0.342822 7.501277,5.044384 7.501277,5.044384c4.366902,7.489033 11.464139,5.325988 14.263854,4.073055c0.440771,-3.167024 1.701868,-5.325988 3.101725,-6.550353c-10.876443,-1.224365 -22.307932,-5.436181 -22.307932,-24.201617c0,-5.346394 1.897766,-9.713297 5.040303,-13.141519c-0.550964,-1.236609 -2.203857,-6.215694 0.428528,-12.961945c0,0 4.101623,-1.314152 13.468016,5.019897c3.917968,-1.089685 8.08081,-1.628406 12.243651,-1.652893c4.162841,0.024487 8.325683,0.563208 12.243651,1.652893c9.305175,-6.334049 13.406798,-5.019897 13.406798,-5.019897c2.632385,6.746252 0.979492,11.725337 0.489746,12.961945c3.122131,3.428222 5.019897,7.795125 5.019897,13.141519c0,18.814411 -11.447814,22.956846 -22.344663,24.160805c1.714111,1.469238 3.305786,4.473014 3.305786,9.060302c0,6.554435 -0.061218,11.819205 -0.061218,13.410879c0,1.285583 0.857056,2.81604 3.367004,2.326294c19.593923,-6.423836 33.612904,-24.789312 33.612904,-46.399357c0,-27.046225 -21.928379,-48.974605 -48.974605,-48.974605' /></g></svg>",
            [Logos.Google]         = LogoPrefix + "<g id='google-svg'><path fill='#ffffff' class='path' stroke='null' d='m50.4849,43.206983l0,16.886897l27.930066,0c-1.128529,7.243104 -8.437293,21.232759 -27.930066,21.232759c-16.804822,0 -30.527734,-13.90758 -30.527734,-31.081739s13.727016,-31.081739 30.527734,-31.081739c9.561718,0 15.967659,4.0586 19.636404,7.587818l13.353575,-12.877541c-8.57682,-8.0064 -19.69796,-12.873438 -32.989979,-12.873438c-27.228326,0 -49.2449,22.016574 -49.2449,49.2449s22.016574,49.2449 49.2449,49.2449c28.422515,0 47.275104,-19.981118 47.275104,-48.120475c0,-3.233748 -0.348818,-5.704201 -0.775607,-8.162342l-46.499497,0z'/></g></svg>",
            [Logos.Facebook]       = LogoPrefix + "<g id='facebook-svg'><path fill='#ffffff' class='path' stroke='null' d='m93.593662,1l-87.187329,0c-2.984917,0 -5.406333,2.421417 -5.406333,5.406333l0,87.187329c0,2.989 2.421417,5.406333 5.406333,5.406333l46.933831,0l0,-37.950498l-12.776749,0l0,-14.785749l12.776749,0l0,-10.922916c0,-12.654249 7.733833,-19.538749 19.024249,-19.538749c5.410416,0 10.061333,0.396083 11.416999,0.57575l0,13.229999l-7.844083,0c-6.125,0 -7.317333,2.944083 -7.317333,7.231583l0,9.436583l14.634666,0l-1.89875,14.822499l-12.735916,0l0,37.901498l24.969582,0c2.993083,0 5.410416,-2.417333 5.410416,-5.406333l0,-87.187329c0,-2.984917 -2.417333,-5.406333 -5.406333,-5.406333'/></g></svg>",
            [Logos.Microsoft]      = LogoPrefix + "<g id='microsoft-svg'><path fill='#ffffff' class='path' stroke='null' d='m47.550002,99.000004l-46.550002,0l0,-46.550002l46.550002,0l0,46.550002zm51.450002,0l-46.550002,0l0,-46.550002l46.550002,0l0,46.550002zm-51.450002,-51.450002l-46.550002,0l0,-46.550002l46.550002,0l0,46.550002zm51.450002,0l-46.550002,0l0,-46.550002l46.550002,0l0,46.550002z'/></g></svg>",
            [Logos.LinkedIn]       = LogoPrefix + "<g id='linkedin-svg'><path fill='#ffffff' class='path' stroke='null' d='m84.491916,84.512333l-14.512167,0l0,-22.740083c0,-5.422667 -0.11025,-12.401083 -7.562333,-12.401083c-7.566417,0 -8.722,5.900417 -8.722,12.000917l0,23.14025l-14.512167,0l0,-46.762333l13.9405,0l0,6.374083l0.187833,0c1.94775,-3.675 6.684417,-7.554167 13.760833,-7.554167c14.704083,0 17.423583,9.6775 17.423583,22.274583l0,25.667833l-0.004083,0zm-61.699166,-53.160916c-4.671333,0 -8.423917,-3.781167 -8.423917,-8.432083c0,-4.646833 3.756667,-8.423917 8.423917,-8.423917c4.655,0 8.428,3.777083 8.428,8.423917c0,4.650917 -3.777083,8.432083 -8.428,8.432083zm7.2765,53.160916l-14.553,0l0,-46.762333l14.553,0l0,46.762333zm61.682833,-83.512332l-83.520499,0c-3.997583,0 -7.231583,3.1605 -7.231583,7.060083l0,83.879832c0,3.903667 3.234,7.060083 7.231583,7.060083l83.508249,0c3.9935,0 7.260167,-3.156417 7.260167,-7.060083l0,-83.879832c0,-3.899583 -3.266667,-7.060083 -7.260167,-7.060083l0.01225,0z'/></g></svg>",

            [Icons.Male]           = IconPrefix + "<g id='male-svg'><path fill='#556080' d='M1 92.84V84.14C1 84.14 2.38 78.81 8.81 77.16C8.81 77.16 19.16 73.37 27.26 69.85C31.46 68.02 32.36 66.93 36.59 65.06C36.59 65.06 37.03 62.9 36.87 61.6H40.18C40.18 61.6 40.93 62.05 40.18 56.94C40.18 56.94 35.63 55.78 35.45 47.66C35.45 47.66 32.41 48.68 32.22 43.76C32.1 40.42 29.52 37.52 33.23 35.12L31.35 30.02C31.35 30.02 28.08 9.51 38.95 12.54C34.36 7.06 64.93 1.59 66.91 18.96C66.91 18.96 68.33 28.35 66.91 34.77C66.91 34.77 71.38 34.25 68.39 42.84C68.39 42.84 66.75 49.01 64.23 47.62C64.23 47.62 64.65 55.43 60.68 56.76C60.68 56.76 60.96 60.92 60.96 61.2L64.74 61.76C64.74 61.76 64.17 65.16 64.84 65.54C64.84 65.54 69.32 68.61 74.66 69.98C84.96 72.62 97.96 77.16 97.96 81.13C97.96 81.13 99 86.42 99 92.85L1 92.84Z'/></g></svg>",
            [Icons.Female]         = IconPrefix + "<g id='female-svg'><path fill='#556080' class='path' stroke='null' d='m85.60156,58.800256c-1.802974,-4.00837 -11.201255,-7.569957 -14.439638,-8.694836c-5.028682,-11.014303 -2.309962,-24.921604 -2.309962,-24.921604c-0.890397,3.951334 -2.031119,5.465959 -2.031119,5.465959c-0.884059,7.148524 -6.714416,13.035916 -6.714416,13.035916c-0.098229,1.920215 0.14259,3.552081 0.59571,4.943128c-8.999028,14.547372 -16.363021,6.936223 -19.997488,1.0013c0.693939,-1.600179 1.115372,-3.542575 0.988626,-5.944428c0,0 -5.830356,-5.887392 -6.720753,-13.035916c0,0 -1.131216,-1.514625 -2.018444,-5.465959c0,0 2.706046,13.859771 -2.294118,24.867737c-3.872117,1.660384 -7.769583,2.83913 -7.769583,2.83913c-4.207996,1.619191 -5.991958,4.043225 -5.991958,4.043225c-6.220103,9.227172 -6.948897,29.779178 -6.948897,29.779178c0.082385,4.695972 2.097661,5.17761 2.097661,5.17761c14.331903,6.388042 36.778774,7.525596 36.778774,7.525596c23.061594,0.487975 39.84288,-6.552813 39.84288,-6.552813c2.439877,-1.539975 2.512757,-2.747238 2.512757,-2.747238c1.698408,-14.734324 -5.580031,-31.315984 -5.580031,-31.315984z'/><path fill='#556080' class='path' stroke='null' d='m60.844727,16.200632c0.247156,0.922084 1.736432,5.649742 6.717584,7.779089c0,0 0.183783,-1.936058 0.725626,-2.119841c0,0 0.611554,0.304192 0.792168,1.029818c0,0 1.299155,-12.421194 -4.356924,-17.056961l0.031687,-0.066542c-11.806471,-10.415424 -25.165592,-2.490576 -25.165592,-2.490576c-9.791196,6.29932 -7.053464,19.911934 -7.053464,19.911934c18.413152,1.739601 28.030071,-6.730259 28.308914,-6.986921z'/></g></svg>",
            [Icons.MaleBusiness]   = IconPrefix + "<g id='male-business-svg'><path fill='#556080' class='path' stroke='null' d='m31.412893,31.445302c0.343588,1.580507 0.870424,2.603637 1.485066,3.241185c1.46216,10.177854 10.597796,18.851555 17.935319,18.488878c9.334154,-0.458118 15.950141,-10.517625 17.435207,-18.488878c0.614642,-0.63373 1.206377,-1.988996 1.561419,-3.580956c0.397036,-1.828654 0.824612,-4.584997 -0.274871,-6.04334c-0.0649,-0.076353 -0.484842,-0.461936 -0.557377,-0.526836c1.049854,-3.787109 3.355714,-10.544349 -2.386031,-16.61823c-3.111385,-3.290814 -7.432964,-4.943857 -11.33842,-6.249493c-11.529303,-3.852009 -19.668532,1.549966 -23.425099,10.532896c-0.271053,0.637548 -2.023354,4.672803 0.099259,12.334827c-0.206153,0.137435 -0.393218,0.313047 -0.549742,0.526836c-1.103301,1.454525 -0.385583,4.554456 0.015271,6.383111z'/><path fill='#556080' class='path' stroke='null' d='m91.040766,87.419684c-0.320683,-7.005387 -0.717718,-18.107113 -6.837411,-27.189302c0,0 -1.744666,-2.378396 -5.882998,-3.958903c0,0 -8.986748,-2.737255 -13.12508,-5.695934l-1.889737,1.294183l0.209971,12.285197l-11.346055,30.289234c-0.248147,0.664271 -0.881877,1.103301 -1.588142,1.103301s-1.339995,-0.43903 -1.588142,-1.103301l-11.342238,-30.289234c0,0 0.209971,-12.247021 0.206153,-12.285197c0.026724,0.103077 -1.893554,-1.294183 -1.893554,-1.294183c-4.130697,2.958679 -13.121263,5.695934 -13.121263,5.695934c-4.138332,1.580507 -5.882998,3.958903 -5.882998,3.958903c-6.115875,9.082189 -6.520546,20.183915 -6.841229,27.189302c-0.221424,4.84078 0.794071,6.646528 2.069166,7.161911c15.827976,6.352569 60.94878,6.352569 76.780574,0c1.28273,-0.511565 2.29059,-2.321131 2.072984,-7.161911z'/><path fill='#556080' class='path' stroke='null' d='m50.985984,57.088456l-0.511565,0.011453c-1.649225,0 -3.31372,-0.320683 -4.947674,-0.885695l4.497192,6.883223l-4.035256,3.894003l4.153603,25.223213c0.034359,0.217606 0.221424,0.37413 0.442847,0.37413c0.217606,0 0.404671,-0.156524 0.442847,-0.37413l4.153603,-25.223213l-4.039074,-3.894003l4.432291,-6.780146c-1.446889,0.423759 -2.977767,0.706265 -4.588815,0.771165z'/></g></svg>",
            [Icons.FemaleBusiness] = IconPrefix + "<g id='female-business-svg'><path fill='#556080' class='path' stroke='null' d='m61.460569,63.399148l0,0l0,-4.153337c0,0 13.088078,0.405204 19.307953,-5.936233c0,0 -9.036041,-2.552783 -7.415227,-19.024311c1.620815,-16.451268 -2.026018,-30.896778 -15.255917,-29.681167c0,0 -5.733632,-6.908722 -17.140114,-2.552783c-3.910215,1.499253 -14.42525,5.247387 -13.898485,28.060352c0.526765,22.792705 -8.104073,22.934526 -8.104073,22.934526s4.45724,6.483258 19.571336,6.341437l0,4.254638l11.487523,33.550861l11.467263,-33.490081l-0.02026,-0.303903z'/><path fill='#556080' class='path' stroke='null' d='m92.316826,97.679376l-2.309661,-12.763915c-0.749627,-4.133077 -3.423971,-7.678609 -7.192365,-9.542546l-14.46577,-7.131584c-0.830667,-0.405204 -1.641075,-0.871188 -2.451482,-1.316912l4.558541,13.452761l-6.381957,-0.486244l-14.060566,17.302195l-14.060566,-17.302195l-6.381957,0.486244l4.659842,-13.452761l-2.897206,1.478993l-14.121347,6.969503c-3.768394,1.863937 -6.442738,5.409469 -7.192365,9.542546l-2.309661,12.763915c-0.162081,0.931968 0.547025,1.803156 1.499253,1.803156l40.256982,0l1.053529,0l40.256982,0c0.992749,0 1.701855,-0.871188 1.539774,-1.803156z'/></g></svg>",
            [Icons.MaleColor]      = IconPrefix + "<g id='male-color-svg'><path fill='#556080' class='path' stroke='null' d='m50.653881,0.00747c-27.500423,-0.469836 -50.176573,21.443301 -50.646409,48.943724c-0.266867,15.592907 6.682943,29.607166 17.748513,38.926827c0.723547,-0.631459 1.499716,-1.210297 2.362334,-1.680132l14.859963,-8.105605c1.948878,-1.063708 3.162934,-3.106554 3.162934,-5.327937l0,-6.089071c0,0 -4.361955,-5.217056 -6.025173,-12.465681c-1.379438,-0.892688 -2.304074,-2.435628 -2.304074,-4.192814l0,-6.66415c0,-1.465887 0.652132,-2.775789 1.665098,-3.692909l0,-9.633511c0,0 -1.978948,-14.991517 18.323592,-14.991517s18.323592,14.991517 18.323592,14.991517l0,9.633511c1.014845,0.917119 1.665098,2.227021 1.665098,3.692909l0,6.66415c0,2.240177 -1.503474,4.125157 -3.544441,4.754737c-1.137002,3.535044 -2.777669,6.904705 -4.94643,9.968034c-0.546889,0.77241 -1.05807,1.426421 -1.505354,1.935723l0,6.243177c0,2.298436 1.298626,4.401421 3.354627,5.427542l15.912395,7.955258c0.954706,0.477353 1.817324,1.080622 2.612286,1.751547c10.731047,-8.94943 17.663943,-22.330351 17.921413,-37.398922c0.473594,-27.500423 -21.437663,-50.176573 -48.939966,-50.646409z'/><path stroke='null' fill='#E7ECED' d='m34.898055,77.913534l-15.017499,8.056835c-0.88126,0.472613 -1.673254,1.053572 -2.410169,1.686836c8.755618,7.261052 20.058152,11.641593 32.407182,11.641593c12.257865,0 23.488227,-4.315161 32.219154,-11.477206c-0.805289,-0.668756 -1.678951,-1.270264 -2.647577,-1.744745l-16.081088,-7.907392c-2.077797,-1.021815 -3.39019,-3.110278 -3.39019,-5.394885l0,-6.205612c0.452025,-0.506237 0.968626,-1.156314 1.521312,-1.924076c2.191753,-3.044897 3.849813,-6.394283 4.998869,-9.908057c2.062603,-0.625792 3.582016,-2.499431 3.582016,-4.726128l0,-6.624052c0,-1.457067 -0.659045,-2.759088 -1.68275,-3.670689l0,-9.575547c0,0 1.999927,-14.901314 -18.517847,-14.901314s-18.517847,14.901314 -18.517847,14.901314l0,9.575547c-1.025604,0.911601 -1.68275,2.213621 -1.68275,3.670689l0,6.624052c0,1.744745 0.93254,3.280269 2.328501,4.167586c1.68275,7.205011 6.089048,12.390675 6.089048,12.390675l0,6.052433c-0.001899,2.204281 -1.228825,4.234835 -3.198365,5.292143z'/></g></svg>",
            [Icons.FemaleColor]    = IconPrefix + "<g id='female-color-svg'><path fill='#556080' class='path' stroke='null' d='m49.881625,0.193237c-27.496353,0 -49.785007,22.290532 -49.785007,49.785007c0,15.070767 6.706886,28.567201 17.287609,37.697583c1.12345,-1.489793 2.562519,-2.750387 4.294661,-3.616458l17.120406,-7.34188c0.928068,-0.464034 1.683297,-1.96322 2.214963,-3.032189c0.479063,-0.965641 -0.236714,-2.102242 -1.315076,-2.102242l-12.108089,0c0,0 -4.377323,-0.428339 -7.75331,-1.87868c-1.97825,-0.849163 -2.658332,-1.970735 -1.42216,-3.731058c3.616458,-5.147582 11.091724,-17.268822 11.334074,-29.786464c0,0 0.415188,-19.269616 19.425546,-19.425546c11.089845,0.092055 16.327604,6.618588 18.768008,12.042336c1.360164,3.026553 1.944433,6.325514 2.16236,9.635747c0.789045,11.931494 6.864695,22.844743 9.695865,27.507625c0.960005,1.581848 0.495971,3.687848 -1.091513,4.63846c-2.605728,1.557425 -5.793848,0.997579 -5.793848,0.997579l-12.188873,0c-1.191083,0 -1.717113,1.66451 -0.781531,2.402831c0.762744,0.601177 1.489793,1.144116 1.882437,1.358285l14.240391,8.617503c2.040246,1.112178 3.667182,2.75978 4.781239,4.706092c0.048846,0.046967 0.103327,0.090177 0.150294,0.140901c11.371647,-9.122868 18.66656,-23.117151 18.66656,-38.830427c0,-27.494475 -22.288654,-49.785007 -49.785007,-49.785007z'/><path stroke='null' fill='#E7ECED' d='m21.758902,83.967938l17.119332,-7.341419c0.928009,-0.464005 1.683191,-1.963097 2.214824,-3.031998c0.479033,-0.965581 -0.236699,-2.10211 -1.314993,-2.10211l-12.107329,0c0,0 -4.377048,-0.428312 -7.752824,-1.878562c-1.978125,-0.84911 -2.658165,-1.970611 -1.422071,-3.730823c3.616231,-5.147259 11.091027,-17.267738 11.333362,-29.784594c0,0 0.415162,-19.268406 19.424327,-19.424327c11.089149,0.09205 16.326579,6.618172 18.76683,12.04158c1.360079,3.026363 1.944311,6.325117 2.162224,9.635142c0.788996,11.930744 6.864264,22.843309 9.695256,27.505898c0.959945,1.581749 0.49594,3.687616 -1.091444,4.638168c-2.603686,1.557328 -5.791605,0.997516 -5.791605,0.997516l-12.188107,0c-1.191008,0 -1.717005,1.664406 -0.781482,2.40268c0.762696,0.60114 1.489699,1.144044 1.882319,1.3582l14.239497,8.616962c2.055146,1.121501 3.695131,2.785907 4.810996,4.750882c-8.545577,6.907471 -19.416812,11.049699 -31.261143,11.049699c-12.349664,0 -23.641697,-4.506669 -32.341316,-11.953287c1.13653,-1.549813 2.618715,-2.857292 4.403348,-3.749609z'/></g></svg>",
            [Icons.Users]          = IconPrefix + "<g id='users-svg'><path fill='#556080' class='path' stroke='null' d='m68.234601,75.454014l-15.811134,-7.90474c-1.491242,-0.746448 -2.418096,-2.245966 -2.418096,-3.914303l0,-5.59588c0.379017,-0.463427 0.777895,-0.991403 1.190014,-1.573997c2.050664,-2.896418 3.694175,-6.120546 4.889154,-9.597903c2.335341,-1.070847 3.851409,-3.379707 3.851409,-5.986483l0,-6.620385c0,-1.593858 -0.595835,-3.138062 -1.655096,-4.344628l0,-8.803457c0.092685,-0.910303 0.456807,-6.329088 -3.462461,-10.799503c-3.399568,-3.877891 -8.916003,-5.844145 -16.398694,-5.844145s-12.999126,1.966254 -16.398694,5.84249c-3.919268,4.470415 -3.555147,9.890855 -3.462461,10.799503l0,8.803457c-1.059262,1.206565 -1.655096,2.75077 -1.655096,4.344628l0,6.620385c0,2.014252 0.915268,3.892786 2.477679,5.145694c1.516068,6.003034 4.688888,10.526412 5.797802,11.977932l0,5.476713c0,1.602133 -0.873891,3.071859 -2.279068,3.839823l-14.765114,8.053698c-4.801434,2.621672 -7.782263,7.643234 -7.782263,13.111672l0,7.014298l76.134428,0l0,-6.691554c0,-5.691876 -3.162889,-10.807778 -8.25231,-13.353317l-0.000001,0z'/><path fill='#556080' class='path' stroke='null' d='m92.155708,77.335859l-16.092501,-6.967955c-0.380672,-0.190336 -0.802722,-0.655418 -1.165188,-1.276079l10.799503,-0.008275c0,0 0.623971,0.061239 1.592203,0.061239c1.775918,0 4.366144,-0.201922 6.620385,-1.170153c1.352214,-0.582594 2.358512,-1.732886 2.762356,-3.156269c0.407154,-1.436624 0.148959,-2.957657 -0.705071,-4.175808c-3.086754,-4.392625 -10.291388,-15.870718 -10.516482,-27.512665c-0.004965,-0.200267 -0.657073,-19.998528 -20.208725,-20.159072c-1.964599,0.016551 -3.821617,0.258195 -5.580985,0.683555c1.310836,3.465772 1.190014,6.567422 1.100639,7.57372l0,7.833571c1.072502,1.525999 1.655096,3.338329 1.655096,5.198657l0,6.620385c0,3.156269 -1.661717,6.077513 -4.314836,7.716059c-1.238012,3.346605 -2.876557,6.473081 -4.880879,9.303296c-0.248264,0.352536 -0.493219,0.68521 -0.733208,0.999678l0,4.733575c0,0.731553 0.390603,1.365454 1.044366,1.691508l15.811134,7.90474c5.93683,2.969243 9.624385,8.935865 9.624385,15.574456l0,6.694864l20.690358,0l0,-6.118891c0,-5.117558 -2.843455,-9.715415 -7.502551,-12.044135z'/></g></svg>",
        };
        
        public static Dictionary<string,string> DataUris { get; set; } = new();
        
        public static string GetImage(string name) => Images.TryGetValue(name, out var svg) ? svg : null;
        public static string GetImage(string name, string fillColor) => Fill(GetImage(name), fillColor);

        public static StaticContent GetStaticContent(string name) => new(GetImage(name).ToUtf8Bytes(), MimeTypes.ImageSvg);

        public static string GetDataUri(string name)
        {
            if (DataUris.TryGetValue(name, out var dataUri))
                return dataUri;
            
            if (!Images.TryGetValue(name, out var svg))
                return null;

            DataUris[name] = dataUri = ToDataUri(svg);
            return dataUri;
        }
        public static string GetDataUri(string name, string fillColor) => Fill(GetDataUri(name), fillColor);

        public static string Create(string body, string fill="none", string viewBox="0 0 24 24", string stroke="currentColor") =>
            $"<svg xmlns='http://www.w3.org/2000/svg' fill='{fill}' viewBox='{viewBox}' stroke='{stroke}' aria-hidden='true'>{body}</svg>";

        public static ImageInfo CreateImage(string body, string fill="none", string viewBox="0 0 24 24", string stroke="currentColor") =>
            ImageSvg(Create(body:body, fill:fill, viewBox:viewBox, stroke:stroke));

        public static ImageInfo ImageSvg(string svg) => new() { Svg = svg };
        public static ImageInfo ImageUri(string uri) => new() { Uri = uri };
        
        public static class Body
        {
            public static string Home  = "<path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6' />";
            public static string Key = "<path fill='currentColor' d='M12.65 10A5.99 5.99 0 0 0 7 6c-3.31 0-6 2.69-6 6s2.69 6 6 6a5.99 5.99 0 0 0 5.65-4H17v4h4v-4h2v-4H12.65zM7 14c-1.1 0-2-.9-2-2s.9-2 2-2s2 .9 2 2s-.9 2-2 2z'/>";
            public static string User = "<path fill='currentColor' d='M20 22H4v-2a5 5 0 0 1 5-5h6a5 5 0 0 1 5 5v2zm-8-9a6 6 0 1 1 0-12a6 6 0 0 1 0 12z'/>";
            public static string UserDetails = "<path fill='currentColor' d='M15 11h7v2h-7zm1 4h6v2h-6zm-2-8h8v2h-8zM4 19h10v-1c0-2.757-2.243-5-5-5H7c-2.757 0-5 2.243-5 5v1h2zm4-7c1.995 0 3.5-1.505 3.5-3.5S9.995 5 8 5S4.5 6.505 4.5 8.5S6.005 12 8 12z'/>";
            public static string UserShield = "<path fill='currentColor' d='M12 1L3 5v6c0 5.55 3.84 10.74 9 12c5.16-1.26 9-6.45 9-12V5l-9-4m0 4a3 3 0 0 1 3 3a3 3 0 0 1-3 3a3 3 0 0 1-3-3a3 3 0 0 1 3-3m5.13 12A9.69 9.69 0 0 1 12 20.92A9.69 9.69 0 0 1 6.87 17c-.34-.5-.63-1-.87-1.53c0-1.65 2.71-3 6-3s6 1.32 6 3c-.24.53-.53 1.03-.87 1.53Z'/>";
            public static string Users = "<path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z' />";
            public static string Table = "<path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M3 8V6a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v2M3 8v6m0-6h6m12 0v6m0-6H9m12 6v4a2 2 0 0 1-2 2H9m12-6H9m-6 0v4a2 2 0 0 0 2 2h4m-6-6h6m0-6v6m0 0v6m6-12v12'/>";
            public static string History = "<g fill='none' stroke='currentColor' stroke-linecap='round' stroke-linejoin='round' stroke-width='2'><path d='M3 3v5h5'/><path d='M3.05 13A9 9 0 1 0 6 5.3L3 8'/><path d='M12 7v5l4 2'/></g>";
            public static string Lock = "<path fill='currentColor' fill-opacity='.886' d='M16 8a3 3 0 0 1 3 3v8a3 3 0 0 1-3 3H7a3 3 0 0 1-3-3v-8a3 3 0 0 1 3-3V6.5a4.5 4.5 0 1 1 9 0V8ZM7 9a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h9a2 2 0 0 0 2-2v-8a2 2 0 0 0-2-2H7Zm8-1V6.5a3.5 3.5 0 0 0-7 0V8h7Zm-3.5 6a1.5 1.5 0 1 0 0 3a1.5 1.5 0 0 0 0-3Zm0-1a2.5 2.5 0 1 1 0 5a2.5 2.5 0 0 1 0-5Z'/>";
            public static string Logs = "<path fill='none' stroke='currentColor' stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M5 13V5a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v13c0 1-.6 3-3 3m0 0H6c-1 0-3-.6-3-3v-2h12v2c0 2.4 2 3 3 3zM9 7h8m-8 4h4'/>";
            public static string Profiling = "<g fill='none' stroke='currentColor' stroke-linecap='round' stroke-linejoin='round' stroke-width='2'><path d='M10 2h4m-2 12l3-3'/><circle cx='12' cy='14' r='8'/></g>";
        }

        public static class Logos
        {
            public const string ServiceStack = "servicestack";
            public const string Apple = "apple";
            public const string Twitter = "twitter";
            public const string GitHub = "github";
            public const string Google = "google";
            public const string Facebook = "facebook";
            public const string Microsoft = "microsoft";
            public const string LinkedIn = "linkedin";
        }

        public static class Icons
        {
            public const string DefaultProfile = Male;
            public const string Male = "male";
            public const string Female = "female";
            public const string MaleBusiness = "male-business";
            public const string FemaleBusiness = "female-business";
            public const string MaleColor = "male-color";
            public const string FemaleColor = "female-color";
            public const string Users = "users";
        }

        public static string Fill(string svg, string fillColor)
        {
            if (string.IsNullOrEmpty(svg) || string.IsNullOrEmpty(fillColor)) 
                return svg;

            foreach (var color in FillColors)
            {
                svg = svg.Replace(color, fillColor);
                if (color[0] == '#' || fillColor[0] == '#')
                {
                    svg = svg.Replace(
                        color[0] == '#' ? "%23" + color.Substring(1) : color, 
                        fillColor[0] == '#' ? "%23" + fillColor.Substring(1) : fillColor);
                }
            }
            return svg;
        }

        public static string Encode(string svg)
        {
            if (string.IsNullOrEmpty(svg))
                return null;

            //['%','#','<','>','?','[','\\',']','^','`','{','|','}'].map(x => `.Replace("${x}","` + encodeURIComponent(x) + `")`).join('')
            return svg.Replace("\r", " ").Replace("\n", "")                
                .Replace("\"","'")
                .Replace("%","%25")
                .Replace("#","%23")
                .Replace("<","%3C")
                .Replace(">","%3E")
                .Replace("?","%3F")
                .Replace("[","%5B")
                .Replace("\\","%5C")
                .Replace("]","%5D")
                .Replace("^","%5E")
                .Replace("`","%60")
                .Replace("{","%7B")
                .Replace("|","%7C")
                .Replace("}","%7D");
        }

        public static string ToDataUri(string svg) => "data:image/svg+xml," + Encode(svg);
        public static string GetBackgroundImageCss(string name) => InBackgroundImageCss(GetImage(name));
        public static string GetBackgroundImageCss(string name, string fillColor) => InBackgroundImageCss(GetImage(name, fillColor));

        public static string InBackgroundImageCss(string svg) => "background-image: url(\"" + ToDataUri(svg) + "\");";

        internal static long ImagesAdded = 0;

        public static void AddImage(string svg, string name, string cssFile=null)
        {
            Interlocked.Increment(ref ImagesAdded);
            
            svg = svg.Trim();
            if (svg.IndexOf("http://www.w3.org/2000/svg", StringComparison.OrdinalIgnoreCase) < 0)
            {
                svg = svg.LeftPart(' ') + " xmlns='http://www.w3.org/2000/svg' " + svg.RightPart(' ');
            }
            
            Images[name] = svg;

            if (cssFile != null)
            {
                if (CssFiles.TryGetValue(cssFile, out var cssFileSvgs))
                {
                    cssFileSvgs.Add(name);
                }
                else
                {
                    CssFiles[cssFile] = new List<string> { name };
                }
            }
        }

        public static void Load(IVirtualDirectory svgDir)
        {
            if (svgDir == null)
                throw new ArgumentNullException(nameof(svgDir));
            
            var context = new ScriptContext {
                ScriptBlocks = {
                    new SvgScriptBlock()
                }
            }.Init();

            var log = LogManager.GetLogger(typeof(Svg));
            foreach (var svgGroupDir in svgDir.GetDirectories())
            {
                foreach (var file in svgGroupDir.GetFiles())
                {
                    if (file.Extension == "svg")
                    {
                        var svg = file.ReadAllText();
                        AddImage(svg, file.Name.WithoutExtension(), svgGroupDir.Name);
                    }
                    else if (file.Extension == "html")
                    {
                        var script = file.ReadAllText();
                        var svg = context.EvaluateScript(script);
                        if (svg.StartsWith("<svg"))
                        {
                            AddImage(svg, file.Name.WithoutExtension(), svgGroupDir.Name);
                        }
                        else
                        {
                            log.Warn($"Unable to load #Script SVG '{file.Name}'");
                        }
                    }
                }
            }

            // Also load any .html #Script files which can register svg using {{#svg name group}} #Script block
            foreach (var svgScript in svgDir.GetAllMatchingFiles("*.html"))
            {
                var script = svgScript.ReadAllText();
                var output = context.EvaluateScript(script);
            }
        }
    }

}