var redisAssertValue = function(key, expected) {
    return function(value) {
        redis.getValue(key, function(value) {
            Assert.areEqual(expected, value);
        }, failFn);
    }
};

var onAfterFlushAllAndSetTestKey = function(onSuccessFn, onErrorFn)
{
    redis.flushAll(function(){
        redis.setEntry(testKey, testValue, onSuccessFn, onErrorFn);
    }, onErrorFn || failFn);
};

var onAfterFlushAllAndAllStringValuesSet = function(onSuccessFn, onErrorFn)
{
    var i = 0;
    redis.flushAll(function() {
        for (var i in stringValues) {
            redis.setEntry(stringValues[i], stringValues[i], function() {
                if (i++ == stringValues.length - 1) onSuccessFn();
            }, onErrorFn || failFn);
        }
    }, onErrorFn || failFn);
};

YAHOO.namespace("ajaxstack");
YAHOO.ajaxstack.RedisClientCommonTests = new YAHOO.tool.TestCase({

    name: "RedisClientCommon Tests",

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

    testAppendToValue: function() {
        redis.setEntry(testKey, testValue, function() {
            redis.appendToValue(testKey, ", World!", function(valueLength) {
                redis.getValue(testKey, function(value) {
                    resume(function() {
                        var expected = "Hello, World!";
                        Assert.areEqual(valueLength, expected.length);
                        Assert.areEqual(value, expected);
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testContainsKey: function() {
        redis.setEntry(testKey, testValue, function() {
            redis.containsKey(testKey, function(result) {
                resume(function() {
                    Assert.isTrue(result);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testContainsKey_doesNotContain: function() {
        redis.containsKey("notExists", function(result) {
            resume(function() {
                Assert.isFalse(result);
            });
        }, failFn);

        wait();
    },

    testDecrementValue: function() {
        redis.setEntry(testKey, "10", function() {
            redis.decrementValue(testKey, 2, function(value) {
                resume(function() {
                    Assert.areEqual(value, 10 - 2);
                });
            }, failFn);
        });

        wait();
    },

    testEcho: function() {
        redis.echo("Hello", function(text) {
            resume(function() {
                Assert.areEqual(text, "Hello");
            });
        }, failFn);

        wait();
    },

    testGetAllKeys: function() {
        onAfterFlushAllAndAllStringValuesSet(function() {
            redis.getAllKeys(function(keys) {
                resume(function() {
                    Assert.isArray(keys);
                    Assert.areEqual(keys.length, stringValues.length);
                });
            }, failFn);
        });

        wait();
    },

    testGetAndSetEntry: function() {
        redis.setEntry(testKey, "A", function() {
            redis.getAndSetEntry(testKey, "B", function(getSetValue) {
                redis.getValue(testKey, function(currentValue) {
                    resume(function() {
                        Assert.areEqual(getSetValue, "A");
                        Assert.areEqual(currentValue, "B");
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testGetEntryType: function() {
        var expectResults = 5;
        var callbacksReceived = 0;

        var keyTypeMap = {};
        var waitForAllEntryTypesFn = function(keyName, keyType) {
            keyTypeMap[keyName] = keyType;
            if (O.keys(keyTypeMap).length == expectResults)
            {
                resume(function() {
                    Assert.areEqual(keyTypeMap[testKey], "String");
                    Assert.areEqual(keyTypeMap["TestList"], "List");
                    Assert.areEqual(keyTypeMap["TestSet"], "Set");
                    Assert.areEqual(keyTypeMap["TestSortedSet"], "SortedSet");
                    Assert.areEqual(keyTypeMap["TestHash"], "Hash");
                });
            }
        };
        var testEntryTypesFn = function() {
            redis.getEntryType(testKey, function(keyType) {
                waitForAllEntryTypesFn(testKey, keyType);
            }, failFn);
            redis.getEntryType("TestList", function(keyType) {
                waitForAllEntryTypesFn("TestList", keyType);
            }, failFn);
            redis.getEntryType("TestSet", function(keyType) {
                waitForAllEntryTypesFn("TestSet", keyType);
            }, failFn);
            redis.getEntryType("TestSortedSet", function(keyType) {
                waitForAllEntryTypesFn("TestSortedSet", keyType);
            }, failFn);
            redis.getEntryType("TestHash", function(keyType) {
                waitForAllEntryTypesFn("TestHash", keyType);
            }, failFn);
        };
        var waitForAllCallbacksFn = function() {
            if (++callbacksReceived == expectResults) testEntryTypesFn();
        };

        //Set keys of different types
        redis.setEntry(testKey, testValue, waitForAllCallbacksFn, failFn);
        redis.addItemToList("TestList", testValue, waitForAllCallbacksFn, failFn);
        redis.addItemToSet("TestSet", testValue, waitForAllCallbacksFn, failFn);
        redis.addItemToSortedSet("TestSortedSet", testValue, 1, waitForAllCallbacksFn, failFn);
        redis.setEntryInHash("TestHash", testKey, testValue, waitForAllCallbacksFn, failFn);

        wait();
    },

    testGetRandomKey: function() {
        onAfterFlushAllAndSetTestKey(function() {
            redis.getRandomKey(function(key) {
                resume(function() {
                    Assert.areEqual(key, testKey);
                });
            }, failFn);
        });

        wait();
    },

    testGetSubstring: function() {
        onAfterFlushAllAndSetTestKey(function() {
            redis.getSubstring(testKey, "0", 5, function(result) {
                resume(function() {
                    Assert.areEqual(result, testValue.substring(0, 5));
                });
            }, failFn);
        });

        wait();
    },

    testGetValue: function() {
        onAfterFlushAllAndSetTestKey(function() {
            redis.getValue(testKey, function(result) {
                resume(function() {
                    Assert.areEqual(result, testValue);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetValues: function() {
        onAfterFlushAllAndAllStringValuesSet(function() {
            redis.getValues(S.toString(stringValues), function(result) {
                resume(function() {
                    Assert.areEqual(S.toString(result), S.toString(stringValues));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testIncrementValue: function() {
        redis.flushAll(function() {
            redis.setEntry(testKey, 10, function() {
                redis.incrementValue(testKey, 2, function(result) {
                    resume(function() {
                        Assert.areEqual(result, 10 + 2);
                    });
                }, failFn);

            }, failFn);
        }, failFn);

        wait();
    },

    testPing: function() {
        redis.ping(function(result) {
            resume(function() {
                Assert.isTrue(result);
            });
        }, failFn);

        wait();
    },

    testRemoveEntry: function() {
        onAfterFlushAllAndSetTestKey(function() {
            redis.getValue(testKey, function(result) {
                resume(function() {
                    Assert.areEqual(result, testValue);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testSearchKeys: function() {
        var expectedCallbacks = 4;
        var callbacksReceived = 0;
        var waitForAllCallbacksFn = function() {
            if (++callbacksReceived == expectedCallbacks)
            {
                redis.searchKeys("A*", function(results) {
                    resume(function() {
                        Assert.areEqual(S.toString(A.sort(results)), S.toString(["A1", "A2"]));
                    });
                }, failFn);
            }
        };

        onAfterFlushAllAndSetTestKey(function() {
            //Set keys of different types
            redis.setEntry("A1", testValue, waitForAllCallbacksFn, failFn);
            redis.setEntry("A2", testValue, waitForAllCallbacksFn, failFn);
            redis.setEntry("B", testValue, waitForAllCallbacksFn, failFn);
            redis.setEntry("C", testValue, waitForAllCallbacksFn, failFn);
        }, failFn);

        wait();
    },

    testSetEntry: function() {
        redis.flushAll(function() {
            redis.setEntry(testKey, testValue, function() {
                redis.getValue(testKey, function(value) {
                    resume(function() {
                        Assert.areEqual(value, testValue);
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testSetEntryIfNotExists: function() {
        redis.flushAll(function() {
            redis.setEntryIfNotExists("Key1", "B", function(result1) {
                redis.setEntryIfNotExists("Key1", "C", function(result2) {
                    resume(function() {
                        Assert.isTrue(result1);
                        Assert.isFalse(result2);
                    });
                }, failFn);
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

