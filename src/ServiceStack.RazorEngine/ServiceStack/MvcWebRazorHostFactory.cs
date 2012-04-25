//// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

//using System.Web.Mvc.Razor;
//using System.Web.WebPages.Razor;
//using ServiceStack.RazorEngine.ServiceStack;

//namespace System.Web.Mvc
//{
//    public class MvcWebRazorHostFactory : WebRazorHostFactory
//    {
//        public override WebPageRazorHost CreateHost(string virtualPath, string physicalPath)
//        {
//            WebPageRazorHost host = base.CreateHost(virtualPath, physicalPath);

//            if (!host.IsSpecialPage)
//            {
//                return new MvcWebPageRazorHost(virtualPath, physicalPath);
//            }

//            return host;
//        }
//    }
//}
