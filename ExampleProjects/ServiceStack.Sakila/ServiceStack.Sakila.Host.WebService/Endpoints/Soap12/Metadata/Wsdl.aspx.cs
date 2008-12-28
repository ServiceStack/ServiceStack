using System;
using Sakila.ServiceModel.Version100.Operations.SakilaService;
using ServiceStack.WebHost.Endpoints.Endpoints;

namespace ServiceStack.Sakila.Host.WebService.Endpoints.Soap12.Metadata
{
    public partial class Wsdl : BaseWsdlPage
    {
        protected string Xsd { get; set; }
        protected string ReplyEndpointUri { get; set; }
        protected string OneWayEndpointUri { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var serviceOperationType = typeof(GetCustomers);
            var operations = GetServiceOperations(serviceOperationType);

            this.repReplyPortTypes.DataSource = operations.ReplyOperations.Names;
            this.repReplyMessages.DataSource = operations.ReplyOperations.Names;
            this.repReplyOperations.DataSource = operations.ReplyOperations.Names;

            this.repOneWayPortTypes.DataSource = operations.OneWayOperations.Names;
            this.repOneWayMessages.DataSource = operations.OneWayOperations.Names;
            this.repOneWayOperations.DataSource = operations.OneWayOperations.Names;

            var baseUri = GetBaseUri(Request);
            ReplyEndpointUri = baseUri + "AsyncOneWay.svc";
            OneWayEndpointUri = baseUri + "SyncReply.svc";

            Xsd = new XsdGenerator
            {
                OperationTypes = operations.AllOperations.Types,
                OptimizeForFlash = Request.QueryString["flash"] != null,
                IncludeAllTypesInAssembly = Request.QueryString["includeAllTypes"] != null,
            }.ToString();

            DataBind(repReplyPortTypes, repReplyMessages, repReplyOperations,
                repOneWayPortTypes, repOneWayMessages, repOneWayOperations);
        }
    }

}
