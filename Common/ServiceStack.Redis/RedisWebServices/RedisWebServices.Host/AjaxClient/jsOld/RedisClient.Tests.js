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

    testEcho: function() 
    {
        redisClient.echo("Hello", function(text) 
        {
            this.resume(function() 
            {
                Assert.areEqual(text, "Hello");
            });
        }, failFn);

        this.wait();
    },

    testPing: function() 
    {
        redisClient.ping(function(result) 
        {
            this.resume(function() 
            {
                Assert.isTrue(result);
            });
                
        }, failFn);

        this.wait();
    },

    testEcho: function() 
    {
        redisClient.echo("Hello", function(text) 
        {
            this.resume(function() 
            {
                Assert.areEqual(text, "Hello");
            });

        }, failFn);
        
        this.wait();
    }


});

