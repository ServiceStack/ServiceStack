//Timed / Waiting or Blocking calls should be kept in this class so their run last and do not effect other suites
YAHOO.namespace("ajaxstack");
YAHOO.ajaxstack.RedisClientTimedOrBlockingTests = new YAHOO.tool.TestCase({

    name: "RedisClientTimedOrBlocking Tests",

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

    testExpireEntry: function() {
        redis.setEntry(testKey, testValue, function() {
            redis.expireEntryIn(testKey, "00:00:01", function() {
                setTimeout(function() {
                    redis.getValue(testKey, function(value) {
                        resume(function() {
                            Assert.isNull(value);
                        });
                    }, failFn);
                }, 2100);
            }, failFn);
        }, failFn);

        wait();
    },

    testGetTimeToLive: function() {
        //Supply in C# TimeSpan SCORM 1.2 format - http://www.ostyn.com/standards/scorm/samples/ISOTimeForSCORM.htm
        var expireIn = "00:00:02";

        redis.setEntryWithExpiry(testKey, testValue, expireIn, function() {
            redis.getTimeToLive(testKey, function(result) {
                resume(function() {
                    //Deserialize .NET JSON TimeSpan from ISO 8601 Duration format
                    Assert.isTrue(SCORM12DurationToCs(expireIn) >= ISODurationToCentisec(result));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testSetEntryWithExpiry: function() {
        //Supply in C# TimeSpan SCORM 1.2 format - http://www.ostyn.com/standards/scorm/samples/ISOTimeForSCORM.htm
        var expireIn = "00:00:01";

        redis.setEntryWithExpiry(testKey, testValue, expireIn, function() {
            redis.getValue(testKey, function(value) {
                //Value should exist when calling back straight away
                Assert.areEqual(value, testValue);

                //After 2 seconds key should have expired
                setTimeout(function() {
                    redis.getValue(testKey, function(value) {
                        resume(function() {
                            Assert.isNull(value);
                        });
                    }, failFn);
                }, 2100);
                
            }, failFn);
        }, failFn);

        wait();
    },

    testEcho: function() {
        redis.echo("Hello", function(text) {
            resume(function() {
                Assert.areEqual(text, "Hello");
            });
        }, failFn);

        wait();
    }

});

