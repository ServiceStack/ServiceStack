var JsonServiceClient = AjaxStack.JsonServiceClient;
var errorHandler = function(e) { alert("service error: " + e); }

YAHOO.namespace("ajaxstack");
YAHOO.ajaxstack.JsonServiceClientTests = new YAHOO.tool.TestCase({

    name: "JsonServiceClient Tests",

    //--------------------------------------------- 
    // Setup and tear down 
    //---------------------------------------------

    setUp: function() {
    },

    tearDown: function() {
    },

    //--------------------------------------------- 
    // Tests 
    //---------------------------------------------

    testCallSearchIndex: function() {

        var client = new JsonServiceClient("http://localhost/RedisWebServices.Host/Public/");

        this.wait(function() {

            client.getFromJsonService("Echo", { Text: "Hola" }, function(r) {
                Assert.areEqual(r.getResult().Text, "Hola");

            }, errorHandler);
        }, 1000);

        //Assert.areEqual('1979-05-09T00:00:00Z', Dto.toUtcDate(date));
    }

});

