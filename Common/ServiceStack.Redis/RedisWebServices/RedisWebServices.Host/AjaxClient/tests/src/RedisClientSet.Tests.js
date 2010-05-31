var setId = "testset";
var setId2 = "testset2";
var setId3 = "testset3";
var emptySet = "testemptyset";

var stringList = [];
var stringList2 = [];
var stringList3 = [];
var lastItem = "four";

var onAfterFlushAllAndStringListAdd = function(onSuccessFn, onErrorFn)
{
    var count = 0;
    redis.flushAll(function() {
        redis.addRangeToSet(setId, A.join(stringList), function() {
            onSuccessFn();
        }, onErrorFn || failFn);
    }, onErrorFn || failFn);
};
var onAfterFlushAllAndAllStringListsAdded = function(onSuccessFn, onErrorFn)
{
    var count = 0;
    redis.flushAll(function() {
        redis.addRangeToSet(setId, A.join(stringList), function() {
            redis.addRangeToSet(setId2, A.join(stringList2), function() {
                redis.addRangeToSet(setId3, A.join(stringList3), function() {
                    onSuccessFn();
                }, onErrorFn || failFn);
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

        stringList = ["one", "two", "three", "four" ];
        stringList2 = ["four", "five", "six", "seven"];
        stringList3 = ["one", "five", "seven", "eleven"];
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
        onAfterFlushAllAndStringListAdd(function() {
            redis.getAllItemsFromSet(setId, function(items) {
                resume(function() {
                    Assert.isTrue(A.areEqual(items, stringList));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetDifferencesFromSet: function() {
        onAfterFlushAllAndAllStringListsAdded(function() {
            redis.getDifferencesFromSet(setId, A.join([setId2, setId3]), function(items) {
                resume(function() {
                    Assert.isTrue(A.areEqual(items, ["two", "three"]));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetIntersectFromSets: function() {
        onAfterFlushAllAndAllStringListsAdded(function() {
            redis.getIntersectFromSets(A.join([setId, setId2]), function(items) {
                resume(function() {
                    Assert.isTrue(A.areEqual(items, ["four"]));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetRandomItemFromSet: function() {
        onAfterFlushAllAndStringListAdd(function() {
            redis.getRandomItemFromSet(setId, function(items) {
                resume(function() {
                    Assert.isTrue(A.contains(stringList, items));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetSetCount: function() {
        onAfterFlushAllAndStringListAdd(function() {
            redis.getSetCount(setId, function(count) {
                resume(function() {
                    Assert.areEqual(count, stringList.length);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetUnionFromSets: function() {
        onAfterFlushAllAndAllStringListsAdded(function() {
            redis.getUnionFromSets(A.join([setId, setId2]), function(items) {
                resume(function() {
                    A.each(stringList2, function(x) {
                        if (!A.contains(stringList, x)) stringList.push(x);
                    });
                    Assert.isTrue(A.areEqual(items, stringList));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testMoveBetweenSets: function() {
        onAfterFlushAllAndStringListAdd(function() {
            redis.moveBetweenSets(setId, emptySet, lastItem, function() {
                redis.getAllItemsFromSet(setId, function(items) {
                    redis.getAllItemsFromSet(emptySet, function(emptySetItems) {
                        resume(function() {
                            A.removeItem(stringList, lastItem);
                            Assert.isTrue(A.areEqual(items, stringList));
                            Assert.isTrue(A.areEqual(emptySetItems, [lastItem]));
                        });
                    }, failFn);
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testPopItemFromSet: function() {
        onAfterFlushAllAndStringListAdd(function() {
            redis.popItemFromSet(setId, function(item) {
                resume(function() {
                    Assert.isTrue(A.contains(stringList, item));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testRemoveItemFromSet: function() {
        onAfterFlushAllAndStringListAdd(function() {
            redis.removeItemFromSet(setId, lastItem, function() {
                redis.getAllItemsFromSet(setId, function(items) {
                    resume(function() {
                        A.removeItem(stringList, lastItem);
                        Assert.isTrue(A.areEqual(items, stringList));
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testSetContainsItem: function() {
        onAfterFlushAllAndStringListAdd(function() {
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
        onAfterFlushAllAndAllStringListsAdded(function() {
            redis.storeDifferencesFromSet("storeset", setId, A.join([setId2, setId3]), function() {
                redis.getAllItemsFromSet("storeset", function(items) {
                    resume(function() {
                        Assert.isTrue(A.areEqual(items, ["two", "three"]));
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testStoreIntersectFromSets: function() {
        onAfterFlushAllAndAllStringListsAdded(function() {
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
        onAfterFlushAllAndAllStringListsAdded(function() {
            redis.storeUnionFromSets("storeset", A.join([setId, setId2]), function() {
                redis.getAllItemsFromSet("storeset", function(items) {
                    resume(function() {
                        A.each(stringList2, function(x) {
                            if (!A.contains(stringList, x)) stringList.push(x);
                        });
                        Assert.isTrue(A.areEqual(items, stringList));
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

