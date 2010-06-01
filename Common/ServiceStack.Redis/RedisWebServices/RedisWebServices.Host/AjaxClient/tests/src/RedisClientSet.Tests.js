var setId = "testset";
var setId2 = "testset2";
var setId3 = "testset3";
var emptySet = "testemptyset";

var stringSet = [];
var stringSet2 = [];
var stringSet3 = [];
var lastItem = "four";

var onAfterFlushAllAndStringSetAdd = function(onSuccessFn, onErrorFn)
{
    var count = 0;
    redis.flushAll(function() {
        redis.addRangeToSet(setId, A.join(stringSet), function() {
            onSuccessFn();
        }, onErrorFn || failFn);
    }, onErrorFn || failFn);
};
var onAfterFlushAllAndAllStringSetsAdded = function(onSuccessFn, onErrorFn)
{
    var count = 0;
    redis.flushAll(function() {
        redis.addRangeToSet(setId, A.join(stringSet), function() {
            redis.addRangeToSet(setId2, A.join(stringSet2), function() {
                onSuccessFn();
            }, onErrorFn || failFn);
        }, onErrorFn || failFn);
    }, onErrorFn || failFn);
};

YAHOO.namespace("ajaxstack");
YAHOO.ajaxstack.RedisClientSetTests = new YAHOO.tool.TestCase({

    name: "RedisClientSet Tests",

    //--------------------------------------------- 
    // Setup and tear down 
    //---------------------------------------------

    setUp: function() {
        resume = this.resume;
        wait = this.wait;

        stringSet = ["one", "two", "three", "four" ];
        stringSet2 = ["four", "five", "six", "seven"];
        stringSet3 = ["one", "five", "seven", "eleven"];
    },

    tearDown: function() {
    },

    //--------------------------------------------- 
    // Tests 
    //---------------------------------------------

    testAddItemToSet: function() {
        redis.flushAll(function() {
            redis.addItemToSet(setId, testValue, function() {
                redis.popItemFromSet(setId, function(item) {
                    resume(function() {
                        Assert.areEqual(item, testValue);
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testGetAllItemsFromSet: function() {
        onAfterFlushAllAndStringSetAdd(function() {
            redis.getAllItemsFromSet(setId, function(items) {
                resume(function() {
                    Assert.isTrue(A.areEqual(items, stringSet));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetDifferencesFromSet: function() {
        onAfterFlushAllAndAllStringSetsAdded(function() {
            redis.addRangeToSet(setId3, A.join(stringSet3), function() {
                redis.getDifferencesFromSet(setId, A.join([setId2, setId3]), function(items) {
                    resume(function() {
                        Assert.isTrue(A.areEqual(items, ["two", "three"]));
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testGetIntersectFromSets: function() {
        onAfterFlushAllAndAllStringSetsAdded(function() {
            redis.getIntersectFromSets(A.join([setId, setId2]), function(items) {
                resume(function() {
                    Assert.isTrue(A.areEqual(items, ["four"]));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetRandomItemFromSet: function() {
        onAfterFlushAllAndStringSetAdd(function() {
            redis.getRandomItemFromSet(setId, function(items) {
                resume(function() {
                    Assert.isTrue(A.contains(stringSet, items));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetSetCount: function() {
        onAfterFlushAllAndStringSetAdd(function() {
            redis.getSetCount(setId, function(count) {
                resume(function() {
                    Assert.areEqual(count, stringSet.length);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetUnionFromSets: function() {
        onAfterFlushAllAndAllStringSetsAdded(function() {
            redis.getUnionFromSets(A.join([setId, setId2]), function(items) {
                resume(function() {
                    A.each(stringSet2, function(x) {
                        if (!A.contains(stringSet, x)) stringSet.push(x);
                    });
                    Assert.isTrue(A.areEqual(items, stringSet));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testMoveBetweenSets: function() {
        onAfterFlushAllAndStringSetAdd(function() {
            redis.moveBetweenSets(setId, emptySet, lastItem, function() {
                redis.getAllItemsFromSet(setId, function(items) {
                    redis.getAllItemsFromSet(emptySet, function(emptySetItems) {
                        resume(function() {
                            A.removeItem(stringSet, lastItem);
                            Assert.isTrue(A.areEqual(items, stringSet));
                            Assert.isTrue(A.areEqual(emptySetItems, [lastItem]));
                        });
                    }, failFn);
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testPopItemFromSet: function() {
        onAfterFlushAllAndStringSetAdd(function() {
            redis.popItemFromSet(setId, function(item) {
                resume(function() {
                    Assert.isTrue(A.contains(stringSet, item));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testRemoveItemFromSet: function() {
        onAfterFlushAllAndStringSetAdd(function() {
            redis.removeItemFromSet(setId, lastItem, function() {
                redis.getAllItemsFromSet(setId, function(items) {
                    resume(function() {
                        A.removeItem(stringSet, lastItem);
                        Assert.isTrue(A.areEqual(items, stringSet));
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testSetContainsItem: function() {
        onAfterFlushAllAndStringSetAdd(function() {
            redis.setContainsItem(setId, lastItem, function(result) {
                redis.setContainsItem(setId, "notexists", function(result2) {
                    resume(function() {
                        Assert.isTrue(result);
                        Assert.isFalse(result2);
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testStoreDifferencesFromSet: function() {
        onAfterFlushAllAndAllStringSetsAdded(function() {
            redis.addRangeToSet(setId3, A.join(stringSet3), function() {
                redis.storeDifferencesFromSet("storeset", setId, A.join([setId2, setId3]), function() {
                    redis.getAllItemsFromSet("storeset", function(items) {
                        resume(function() {
                            Assert.isTrue(A.areEqual(items, ["two", "three"]));
                        });
                    }, failFn);
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testStoreIntersectFromSets: function() {
        onAfterFlushAllAndAllStringSetsAdded(function() {
            redis.storeIntersectFromSets("storeset", A.join([setId, setId2]), function() {
                redis.getAllItemsFromSet("storeset", function(items) {
                    resume(function() {
                        Assert.isTrue(A.areEqual(items, ["four"]));
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testStoreUnionFromSets: function() {
        onAfterFlushAllAndAllStringSetsAdded(function() {
            redis.storeUnionFromSets("storeset", A.join([setId, setId2]), function() {
                redis.getAllItemsFromSet("storeset", function(items) {
                    resume(function() {
                        A.each(stringSet2, function(x) {
                            if (!A.contains(stringSet, x)) stringSet.push(x);
                        });
                        Assert.isTrue(A.areEqual(items, stringSet));
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

