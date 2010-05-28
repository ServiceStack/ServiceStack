YAHOO.namespace("ajaxstack");
YAHOO.ajaxstack.RedisClientSetTests = new YAHOO.tool.TestCase({

    name: "RedisClientSet Tests",

    //--------------------------------------------- 
    // Setup and tear down 
    //---------------------------------------------

    setUp: function() {
        resume = this.resume;
        wait = this.wait;
    },

    tearDown: function() {
    },

    //--------------------------------------------- 
    // Tests 
    //---------------------------------------------

    testEcho: function() {
        redis.echo("Hello", function(text) {
            resume(function() {
                Assert.areEqual(text, "Hello");
            });
        }, failFn);

        wait();
    }


});

