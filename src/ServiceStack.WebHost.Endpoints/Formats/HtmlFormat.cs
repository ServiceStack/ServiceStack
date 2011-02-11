using System;
using System.IO;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Formats
{
	public class HtmlFormat
	{
		private const string Template = @"
<!doctype html>
<html lang=""en-us"" dir=""ltr"">
<head>
<title>{1}</title>
<style type=""text/css"">
BODY, H1, H2, H3, H4, H5, H6, DL, DT, DD {{
  margin: 0;
  padding: 0;
  color: #444;
}}
H1 {{
  text-align: center;
  font: 18px Helvetica;
  padding: 0 0 30px 0;
}}
H1 B {{
  font-weight: normal;
  color: #069;
}}
BODY {{
  padding: 20px;
  font: 13px/15px Arial, Verdana, Helvetica;
}}
#body {{
  text-align: left;
}}
TABLE {{
  border-collapse:collapse;
  border: solid 1px #ccc;
}}
TH {{
  text-align: left;
  padding: 4px 8px;
  background: #f1f1f1;
  white-space:nowrap;
}}
TD {{
  padding: 4px 8px;
  vertical-align: top;
}}
DT {{
  
  margin: 20px 0 5px 0;
  font: 24px Helvetica;
}}
#sslnk {{
  position: absolute;
  bottom: 10px;
  right: 10px;
}}
</style>
<script>
  var dto = ""{0}"";
</script>
</head>
<body>
<div id=""body"">

  <h1>{1} generated on: <b>{2}</b></h1>
  
  <div id=""content""></div>

  <a id='sslnk' href='http://www.servicestack.net'>by servicestack.net</a>
</div>
<script>
var $ = function(id) {{ return document.getElementById(id); }},
  $$ = function(sel) {{ return document.querySelectorAll(sel); }}

var splitCase = function(t) {{ return typeof t != 'string' ? t : t.replace(/([A-Z]|[0-9]+)/g, ' $1'); }}

function val(m) {{
  if (!m) return '';
  if (typeof m == 'string') return str(m);
  if (typeof m == 'number') return num(m);
  return m.length ? arr(m) : obj(m);
}}
function num(m) {{ return m; }}
function str(m) {{
  return (m.substr(0,6) == '/Date(')
    ? new Date(parseFloat(/Date\(([^)]+)\)/.exec(m)[1])).toDateString()
      : m;
}}
function obj(m) {{
  var sb = '<dl>';
  for (var k in m)
  {{
    var v = m[k];
    sb += '<dt>' + splitCase(k) + '</dt><dd>' + val(v) + '</dd>';
  }}
  sb += '</dl>';
  return sb;
}}
function arr(m) {{
  if (typeof m[0] == 'string' || typeof m[0] == 'number') return m.join(', ');
  var h = {{}};
  for (var i=0,len=m.length; i<len; i++)
  {{
    var row = m[i];
    for (var k in row) 
    {{
      h[k] = k;
    }}
  }}
  var sb = '<table><caption></caption>';
  sb += '<thead>';

  for (var k in h)
  {{
    sb += '<th>' + splitCase(k) + '</th>';
  }}

  sb += '</thead>';
  sb += '<tbody>';

  for (var i=0,len=m.length; i<len; i++)
  {{
    sb += '<tr>';
    var row = m[i];
    for (var k in h) 
    {{
      var f = row[k];
      sb += '<td>' + val(f) + '</td>';
    }}
    sb += '</tr>';
  }}
  
  sb += '</tbody>';
  sb += '</table>';
  return sb;
}}

var model = JSON.parse(dto);
$(""content"").innerHTML = val(model);  

</script>
</body>
</html>";

		public static void Register(IAppHost appHost)
		{
			appHost.ContentTypeFilters.Register(
			  ContentType.Html,
			  SerializeToStream,
			  DeserializeFromStream);
		}

		public static void SerializeToStream(object obj, Stream stream)
		{
			var title = obj.GetType().Name;
			var id = obj.GetId();
			var hasId = (id is int ? (int)id : 0) != obj.GetHashCode();
			if (hasId)
			{
				title += ": " + id;
			}

			var json = JsonSerializer.SerializeToString(obj);
			var jsonVar = json != null ? json.Replace("\"", "\\\"") : null;

			var html = string.Format(Template,
			  jsonVar,
			  title,
			  DateTime.UtcNow);

			using (var sw = new StreamWriter(stream))
			{
				sw.Write(html);
			}
		}

		public static object DeserializeFromStream(Type type, Stream stream)
		{
			return null;
		}
	}
}