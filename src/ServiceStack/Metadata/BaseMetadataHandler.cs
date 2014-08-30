using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;
using ServiceStack.Host;
using ServiceStack.Support.WebHost;
using ServiceStack.Web;
using System.Text;

namespace ServiceStack.Metadata
{
    public abstract class BaseMetadataHandler : HttpHandlerBase
    {
        public abstract Format Format { get; }

        public string ContentType { get; set; }
        public string ContentFormat { get; set; }

        public override void Execute(HttpContextBase context)
        {
            var writer = new HtmlTextWriter(context.Response.Output);
            context.Response.ContentType = "text/html";

            var request = context.ToRequest();
            ProcessOperations(writer, request, request.Response);
        }

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            using (var sw = new StreamWriter(httpRes.OutputStream))
            {
                var writer = new HtmlTextWriter(sw);
                httpRes.ContentType = "text/html";
                ProcessOperations(writer, httpReq, httpRes);
            }
        }

        public virtual string CreateResponse(Type type)
        {
            if (type == typeof(string))
                return "(string)";
            if (type == typeof(byte[]))
                return "(byte[])";
            if (type == typeof(Stream))
                return "(Stream)";
            if (type == typeof(HttpWebResponse))
                return "(HttpWebResponse)";
            if (type.IsGenericType)
                type = type.GetGenericArguments()[0]; //e.g. Task<T> => T

            return CreateMessage(type);
        }

        protected virtual void ProcessOperations(HtmlTextWriter writer, IRequest httpReq, IResponse httpRes)
        {
            var operationName = httpReq.QueryString["op"];

            if (!AssertAccess(httpReq, httpRes, operationName)) return;

            ContentFormat = ServiceStack.ContentFormat.GetContentFormat(Format);
            var metadata = HostContext.Metadata;
            if (operationName != null)
            {
                var allTypes = metadata.GetAllTypes();
                //var operationType = allTypes.Single(x => x.Name == operationName);
                var operationType = allTypes.Single(x => x.GetOperationName() == operationName);
                var op = metadata.GetOperation(operationType);
                var requestMessage = CreateResponse(operationType);
                string responseMessage = null;

                var responseType = metadata.GetResponseTypeByRequest(operationType);
                if (responseType != null)
                {
                    responseMessage = CreateResponse(responseType);
                }

                var isSoap = Format == Format.Soap11 || Format == Format.Soap12;
                var sb = new StringBuilder();
                var description = operationType.GetDescription();
                if (!description.IsNullOrEmpty())
                {
                    sb.AppendFormat("<h3 id='desc'>{0}</div>", ConvertToHtml(description));
                }

                if (op.Routes.Count > 0)
                {
                    sb.Append("<table>");
                    if (!isSoap)
                    {
                        sb.Append("<caption>The following routes are available for this service:</caption>");
                    }
                    sb.Append("<tbody>");

                    foreach (var route in op.Routes)
                    {
                        if (isSoap && !(route.AllowsAllVerbs || route.AllowedVerbs.Contains(HttpMethods.Post)))
                            continue;

                        sb.Append("<tr>");
                        var verbs = route.AllowsAllVerbs ? "All Verbs" : route.AllowedVerbs;

                        if (!isSoap)
                        {
                            var path = "/" + PathUtils.CombinePaths(HostContext.Config.HandlerFactoryPath, route.Path);

                            sb.AppendFormat("<th>{0}</th>", verbs);
                            sb.AppendFormat("<th>{0}</th>", path);
                        }
                        sb.AppendFormat("<td>{0}</td>", route.Summary);
                        sb.AppendFormat("<td><i>{0}</i></td>", route.Notes);
                        sb.Append("</tr>");
                    }

                    sb.Append("<tbody>");
                    sb.Append("</tbody>");
                    sb.Append("</table>");
                }
                
				this.AppendParameterDescription( operationType, sb, new HashSet< Type >() );

                sb.Append(@"<div class=""call-info"">");
                var overrideExtCopy = HostContext.Config.AllowRouteContentTypeExtensions
                   ? " the <b>.{0}</b> suffix or ".Fmt(ContentFormat)
                   : "";
                sb.AppendFormat(@"<p>To override the Content-type in your clients, use the HTTP <b>Accept</b> Header, append {1} <b>?format={0}</b></p>", ContentFormat, overrideExtCopy);
                if (ContentFormat == "json")
                {
                    sb.Append("<p>To embed the response in a <b>jsonp</b> callback, append <b>?callback=myCallback</b></p>");
                }
                sb.Append("</div>");

                RenderOperation(writer, httpReq, operationName, requestMessage, responseMessage, sb.ToString());
                return;
            }

            RenderOperations(writer, httpReq, metadata);
        }

	    protected void RenderOperations(HtmlTextWriter writer, IRequest httpReq, ServiceMetadata metadata)
        {
            var defaultPage = new IndexOperationsControl
            {
                HttpRequest = httpReq,
                MetadataConfig = HostContext.MetadataPagesConfig,
                Title = HostContext.ServiceName,
                Xsds = XsdTypes.Xsds,
                XsdServiceTypesIndex = 1,
                OperationNames = metadata.GetOperationNamesForMetadata(httpReq),
            };

            var metadataFeature = HostContext.GetPlugin<MetadataFeature>();
            if (metadataFeature != null && metadataFeature.IndexPageFilter != null)
            {
                metadataFeature.IndexPageFilter(defaultPage);
            }

            defaultPage.RenderControl(writer);
        }

        private string ConvertToHtml(string text)
        {
            return text.Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\n", "<br />\n");
        }

        protected bool AssertAccess(IRequest httpReq, IResponse httpRes, string operationName)
        {
            var appHost = HostContext.AppHost;
            if (!appHost.HasAccessToMetadata(httpReq, httpRes)) return false;

            if (operationName == null) return true; //For non-operation pages we don't need to check further permissions
            if (!appHost.Config.EnableAccessRestrictions) return true;
            if (!appHost.MetadataPagesConfig.IsVisible(httpReq, Format, operationName))
            {
                appHost.HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Service Not Available");
                return false;
            }

            return true;
        }

        protected abstract string CreateMessage(Type dtoType);

        protected virtual void RenderOperation(HtmlTextWriter writer, IRequest httpReq, string operationName,
            string requestMessage, string responseMessage, string metadataHtml)
        {
            var operationControl = new OperationControl
            {
                HttpRequest = httpReq,
                MetadataConfig = HostContext.Config.ServiceEndpointsMetadataConfig,
                Title = HostContext.ServiceName,
                Format = this.Format,
                OperationName = operationName,
                HostName = httpReq.GetUrlHostName(),
                RequestMessage = requestMessage,
                ResponseMessage = responseMessage,
                MetadataHtml = metadataHtml,
            };
            if (!this.ContentType.IsNullOrEmpty())
            {
                operationControl.ContentType = this.ContentType;
            }
            if (!this.ContentFormat.IsNullOrEmpty())
            {
                operationControl.ContentFormat = this.ContentFormat;
            }

            var metadataFeature = HostContext.GetPlugin<MetadataFeature>();
            if (metadataFeature != null && metadataFeature.DetailPageFilter != null)
            {
                metadataFeature.DetailPageFilter(operationControl);
            }

            operationControl.Render(writer);
        }

	    private void AppendParameterDescription( Type modelType, StringBuilder sb, HashSet< Type > processedTypes )
	    {
		    if( IsScalarType( modelType ) || processedTypes.Contains( modelType ) || !IsValidToDocumentType( modelType ) )
			    return;

		    if( IsListType( modelType ) )
			    modelType = GetListElementType( modelType );

		    var apiMembers = modelType.GetApiMembers();
		    if( apiMembers.Count > 0 )
		    {
			    sb.AppendFormat( "<table class='membersTable'><caption>Parameters for <strong><em>{0}</em></strong>:</caption>", modelType.Name );
			    sb.Append( "<thead><tr>" );
			    sb.Append( "<th>Name</th>" );
			    sb.Append( "<th>Parameter</th>" );
			    sb.Append( "<th>Data Type</th>" );
			    sb.Append( "<th>Required</th>" );
			    sb.Append( "<th>Description</th>" );
			    sb.Append( "</tr></thead>" );

			    sb.Append( "<tbody>" );
			    foreach( var apiMember in apiMembers )
			    {
				    sb.Append( "<tr valign='top'>" );
				    sb.AppendFormat( "<td>{0}</td>", this.ConvertToHtml( apiMember.Name ) );
				    sb.AppendFormat( "<td>{0}</td>", apiMember.ParameterType );
				    sb.AppendFormat( "<td>{0}</td>", apiMember.DataType );
				    sb.AppendFormat( "<td>{0}</td>", apiMember.IsRequired ? "Yes" : "No" );
				    sb.Append( "<td>" );
				    AppendFullDescription( apiMember, sb );
				    sb.Append( "</td>" );
				    sb.Append( "</tr>" );
			    }
			    sb.Append( "</tbody>" );
			    sb.Append( "</table>" );
		    }

		    processedTypes.Add( modelType );

		    foreach( var propertyType in modelType.GetProperties().Select( p => p.PropertyType ) )
		    {
			    this.AppendParameterDescription( propertyType, sb, processedTypes );

			    foreach( var genericTypeArgument in propertyType.GenericTypeArguments() )
			    {
				    this.AppendParameterDescription( genericTypeArgument, sb, processedTypes );
			    }
		    }
	    }
		
		private static bool IsValidToDocumentType( Type modelType )
		{
			return modelType.Namespace != null && !modelType.Namespace.StartsWith( "System" ) && !modelType.Namespace.StartsWith( "Microsoft" );
		}

		private static void AppendFullDescription( ModelInfo apiMember, StringBuilder sb )
		{
			sb.Append( apiMember.Description );
			if( apiMember.Min.HasValue || apiMember.Max.HasValue )
			{
				sb.Append( "<br /" );
				sb.AppendFormat( "Min/Max: [{0}-{1}]", ConvertToString( apiMember.Min ), ConvertToString( apiMember.Max ) );
			}

			if( apiMember.AllowedValues != null )
			{
				sb.AppendFormat( "<div class='valuesHeader'>Possible values</div>" );
				sb.Append( "<ul>" );
				foreach( var value in apiMember.AllowedValues )
				{
					sb.AppendFormat( "<li>{0}</li>", value );
				}
				sb.Append( "</ul>" );
			}
		}

		private static string ConvertToString( int? value )
		{
			return value.HasValue ? value.ToString() : string.Empty;
		}

		private static readonly Dictionary< Type, string > _clrTypesToSwaggerScalarTypes = new Dictionary< Type, string >
		{
			{ typeof( byte ), SwaggerType.Byte },
			{ typeof( sbyte ), SwaggerType.Byte },
			{ typeof( bool ), SwaggerType.Boolean },
			{ typeof( short ), SwaggerType.Int },
			{ typeof( ushort ), SwaggerType.Int },
			{ typeof( int ), SwaggerType.Int },
			{ typeof( uint ), SwaggerType.Int },
			{ typeof( long ), SwaggerType.Long },
			{ typeof( ulong ), SwaggerType.Long },
			{ typeof( float ), SwaggerType.Float },
			{ typeof( double ), SwaggerType.Double },
			{ typeof( decimal ), SwaggerType.Double },
			{ typeof( string ), SwaggerType.String },
			{ typeof( DateTime ), SwaggerType.Date }
		};

		private static bool IsScalarType( Type type )
		{
			return _clrTypesToSwaggerScalarTypes.ContainsKey( type ) || ( Nullable.GetUnderlyingType( type ) ?? type ).IsEnum;
		}

		private static Type GetListElementType( Type type )
		{
			if( type.IsArray )
				return type.GetElementType();

			if( !type.IsGenericType )
				return null;
			var genericType = type.GetGenericTypeDefinition();
			if( genericType == typeof( List< > ) || genericType == typeof( IList< > ) || genericType == typeof( IEnumerable< > ) )
				return type.GetGenericArguments()[ 0 ];
			return null;
		}

		private static bool IsListType( Type type )
		{
			return GetListElementType( type ) != null;
		}
    }
}