var redisClient = new RedisClient("http://localhost/RedisWebServices.Host/Public/");

var failFn = function(error) {
    alert("error: " + S.toString(error));
    Assert.fail("onErrorFn called" + error);
}

YAHOO.namespace("ajaxstack");
YAHOO.ajaxstack.RedisClientTests = new YAHOO.tool.TestCase({

    name: "RedisClient Tests",

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

    testEcho: function() {
        this.wait(function() {
            redisClient.echo("Hello", function(text) {
                Assert.areEqual(text, "Hello");
            }, failFn);

        }, 200);
    },

    testPing: function() {
        this.wait(function() {
            redisClient.ping(function(result) {
                Assert.isTrue(result);
            }, failFn);

        }, 200);
    },
    
    testEcho: function() {
        this.wait(function() {
            redisClient.echo("Hello", function(text) {
                Assert.areEqual(text, "Hello");
            }, failFn);

        }, 200);
    }


});

