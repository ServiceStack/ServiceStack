<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Test.aspx.cs" Inherits="ServiceStack.WebHost.IntegrationTests.Subpages.Test" %>
<%@ Import Namespace="ServiceStack.Html" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <%=ServiceStack.MiniProfiler.Profiler.RenderIncludes(null, null, null, null, false, null).AsRaw() %>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        Test Page
    </div>
    </form>
</body>
</html>
