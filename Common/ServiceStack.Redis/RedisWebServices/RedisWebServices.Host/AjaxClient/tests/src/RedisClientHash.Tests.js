var hashId = "testhash";

var stringMap = {one:"a", two:"b", three:"c", four:"d"};
var stringMapCount = AjaxStack.ObjectExt.keys(stringMap).length;
var firstHashKey = "one";
var stringIntMap = {one:1, two:2, three:3, four:4};

var onAfterFlushAllAndStringMapSet = function(onSuccessFn, onErrorFn)
{
    var i = 0;
    redis.flushAll(function() {
        for (var key in stringMap) {
            redis.setEntryInHash(hashId, key, stringMap[key], function() {
                if (i++ == stringMapCount - 1) onSuccessFn();
            }, onErrorFn || failFn);
        }
    }, onErrorFn || failFn);
};

var isKvpsEqualToMap = function(kvps, map)
{
    var toMap = RedisClient.convertKeyValuePairsToMap(kvps);
    return O.areEqual(toMap, map);
}
/*
YAHOO.namespace("ajaxstack");
YAHOO.ajaxstack.RedisClientHashTests = new YAHOO.tool.TestCase({

    name: "RedisClientHash Tests",

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

    testGetAllEntriesFromHash: function() {
        onAfterFlushAllAndStringMapSet(function() {
            redis.getAllEntriesFromHash(hashId, function(kvps) {
                resume(function() {
                    Assert.isTrue(isKvpsEqualToMap(kvps, stringMap));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetHashCount: function() {
        onAfterFlushAllAndStringMapSet(function() {
            redis.getHashCount(hashId, function(count) {
                resume(function() {
                    Assert.areEqual(count, stringMapCount);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetHashKeys: function() {
        onAfterFlushAllAndStringMapSet(function() {
            redis.getHashKeys(hashId, function(keys) {
                resume(function() {
                    Assert.isTrue(A.areEqual(keys, O.keys(stringMap)));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetValueFromHash: function() {
        onAfterFlushAllAndStringMapSet(function() {
            redis.getValueFromHash(hashId, firstHashKey, function(value) {
                resume(function() {
                    Assert.areEqual(value, stringMap[firstHashKey]);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetHashValues: function() {
        onAfterFlushAllAndStringMapSet(function() {
            redis.getHashValues(hashId, function(values) {
                resume(function() {
                    Assert.isTrue(A.areEqual(values, O.values(stringMap)));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testHashContainsEntry: function() {
        onAfterFlushAllAndStringMapSet(function() {
            redis.hashContainsEntry(hashId, firstHashKey, function(result) {
                resume(function() {
                    Assert.isTrue(result);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testIncrementValueInHash: function() {
        redis.flushAll(function() {
            redis.setEntryInHash(hashId, firstHashKey, 10, function() {
                redis.incrementValueInHash(hashId, firstHashKey, 2, function(result) {
                    resume(function() {
                        Assert.areEqual(result, 10 + 2);
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testRemoveEntryFromHash: function() {
        redis.flushAll(function() {
            redis.setEntryInHash(hashId, firstHashKey, 10, function() {
                redis.removeEntryFromHash(hashId, firstHashKey, function(itemWasRemoved) {
                    redis.getValueFromHash(hashId, firstHashKey, function(value) {
                        resume(function() {
                            Assert.isTrue(itemWasRemoved);
                            Assert.isNull(value);
                        });
                    }, failFn);
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testSetEntryInHash: function() {
        redis.flushAll(function() {
            redis.setEntryInHash(hashId, testKey, testValue, function() {
                redis.getValueFromHash(hashId, testKey, function(value) {
                    resume(function() {
                        Assert.areEqual(value, testValue);
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testSetEntryInHashIfNotExists: function() {
        redis.flushAll(function() {
            redis.setEntryInHashIfNotExists(hashId, testKey, testValue, function(wasSet) {
                redis.getValueFromHash(hashId, testKey, function(value) {
                    redis.setEntryInHashIfNotExists(hashId, testKey, testValue, function(wasSet2) {
                        resume(function() {
                            Assert.areEqual(value, testValue);
                            Assert.isTrue(wasSet);
                            Assert.isFalse(wasSet2);
                        });
                    }, failFn);
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testSetRangeInHash: function() {
        onAfterFlushAllAndStringMapSet(function() {
            redis.setRangeInHash(hashId, RedisClient.convertMapToKeyValuePairsDto(stringMap), function() {
                redis.getAllEntriesFromHash(hashId, function(kvps) {
                    resume(function() {
                        Assert.isTrue(isKvpsEqualToMap(kvps, stringMap));
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testPause_for_a_sec: function() {
        wait(1000);
    }


});

*/