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

        this.resume(function() {
        
            client.getFromService("Echo", { Text: "Hola" }, function(r) {
                Assert.areEqual(r.getResult().Text, "Hola");
            }, errorHandler);
            
        });

        this.wait();
    }

});

