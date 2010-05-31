var setId = "testzset";
var setId2 = "testzset2";
var setId3 = "testzset3";
var emptySet = "testemptyset";

var stringList = [];
var stringList2 = [];
var stringDoubleMap = {};
var firstItem = "one";
var lastItem = "four";
var lastItemScore = 4;

var startPos = '0';
var endPos = -1;

var onAfterFlushAllAndAllStringListsAdded = function(onSuccessFn, onErrorFn)
{
    var count = 0;
    redis.flushAll(function() {
        redis.addRangeToSortedSet(setId, RedisClient.convertArrayToItemWithScoresDto(stringList), function() {
            redis.addRangeToSortedSet(setId2, RedisClient.convertArrayToItemWithScoresDto(stringList2), function() {
                onSuccessFn();
            }, onErrorFn || failFn);
        }, onErrorFn || failFn);
    }, onErrorFn || failFn);
};
var onAfterFlushAllAndStringDoubleMap = function(onSuccessFn, onErrorFn)
{
    var count = 0;
    redis.flushAll(function() {
        redis.addRangeToSortedSet(setId, RedisClient.convertMapToItemWithScoresDto(stringDoubleMap), function() {
            onSuccessFn();
        }, onErrorFn || failFn);
    }, onErrorFn || failFn);
};

YAHOO.namespace("ajaxstack");
YAHOO.ajaxstack.RedisClientSortedSet = new YAHOO.tool.TestCase({

    name: "RedisClientSortedSet Tests",

    //--------------------------------------------- 
    // Setup and tear down 
    //---------------------------------------------

    setUp: function() {
        resume = this.resume;
        wait = this.wait;

        stringList = ["one", "two", "three", "four" ];
        stringList2 = ["four", "five", "six", "seven"];
        stringDoubleMap = {'one':1, 'two':2, 'three':3, 'four':4};
    },

    tearDown: function() {
    },

    //--------------------------------------------- 
    // Tests 
    //---------------------------------------------

    testAddItemToSortedSet: function() {
        redis.flushAll(function() {
            redis.addItemToSortedSet(setId, testValue, 1, function() {
                redis.popItemWithHighestScoreFromSortedSet(setId, function(item) {
                    resume(function() {
                        Assert.areEqual(testValue, item);
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testGetAllItemsFromSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.getAllItemsFromSortedSet(setId, function(items) {
                resume(function() {
                    Assert.isTrue(A.areEqual(O.keys(stringDoubleMap), items));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetAllItemsFromSortedSetDesc: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.getAllItemsFromSortedSetDesc(setId, function(items) {
                resume(function() {
                    Assert.isTrue(A.areEqual(O.keys(stringDoubleMap), items));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetItemIndexInSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.getItemIndexInSortedSet(setId, lastItem, function(index) {
                resume(function() {
                    Assert.areEqual(O.count(stringDoubleMap) - 1, index);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetItemScoreInSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.getItemScoreInSortedSet(setId, lastItem, function(score) {
                resume(function() {
                    Assert.areEqual(lastItemScore, score);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetRangeFromSortedSetByLowestScore: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.getRangeFromSortedSetByLowestScore(setId, 2, 3, startPos, endPos, function(items) {
                resume(function() {
                    var expectedItems = O.where(stringDoubleMap, function(k, score) {
                        return score >= 2 && score <= 3;
                    });
                    Assert.isTrue(A.areEqual(O.keys(expectedItems), items));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetRangeWithScoresFromSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.getRangeWithScoresFromSortedSet(setId, startPos, 2, function(itws) {
                resume(function() {
                    var itemsMap = RedisClient.convertItemWithScoresToMap(itws);
                    var expectedItems = O.take(stringDoubleMap, 3);
                    Assert.isTrue(O.areEqual(expectedItems, itemsMap));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetRangeWithScoresFromSortedSetByLowestScore: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.getRangeWithScoresFromSortedSetByLowestScore(setId, 2, 3, startPos, endPos, function(itws) {
                resume(function() {
                    var itemsMap = RedisClient.convertItemWithScoresToMap(itws);
                    var expectedItems = O.where(stringDoubleMap, function(k, score) {
                        return score >= 2 && score <= 3;
                    });
                    Assert.isTrue(O.areEqual(expectedItems, itemsMap));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetSortedSetCount: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.getSortedSetCount(setId, function(count) {
                resume(function() {
                    Assert.areEqual(O.count(stringDoubleMap), count);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testIncrementItemInSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.incrementItemInSortedSet(setId, lastItem, 2, function(score) {
                resume(function() {
                    Assert.areEqual(lastItemScore + 2, score);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testPopItemWithHighestScoreFromSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.popItemWithHighestScoreFromSortedSet(setId, function(item) {
                resume(function() {
                    Assert.areEqual(lastItem, item);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testPopItemWithLowestScoreFromSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.popItemWithLowestScoreFromSortedSet(setId, function(item) {
                resume(function() {
                    Assert.areEqual(firstItem, item);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testRemoveItemFromSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.removeItemFromSortedSet(setId, lastItem, function(result) {
                redis.getAllItemsFromSortedSet(setId, function(items) {
                    resume(function() {
                        Assert.isTrue(result);
                        var expectedItems = A.removeItem(O.keys(stringDoubleMap), lastItem);
                        Assert.isTrue(A.areEqual(expectedItems, items));
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testRemoveRangeFromSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.removeRangeFromSortedSet(setId, startPos, 2, function(itemsRemovedCount) {
                redis.getAllItemsFromSortedSet(setId, function(items) {
                    resume(function() {
                        Assert.areEqual(3, itemsRemovedCount);
                        var expectedItems = O.keys(O.skip(stringDoubleMap, 3));
                        Assert.isTrue(A.areEqual(expectedItems, items));
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testRemoveRangeFromSortedSetByScore: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.removeRangeFromSortedSetByScore(setId, 1, 2, function(itemsRemovedCount) {
                redis.getAllItemsFromSortedSet(setId, function(items) {
                    resume(function() {
                        Assert.areEqual(2, itemsRemovedCount);
                        var expectedItems = O.keys(O.skip(stringDoubleMap, 2));
                        Assert.isTrue(A.areEqual(expectedItems, items));
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testSortedSetContainsItem: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.sortedSetContainsItem(setId, lastItem, function(result) {
                resume(function() {
                    Assert.isTrue(result);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testStoreIntersectFromSortedSets: function() {
        onAfterFlushAllAndAllStringListsAdded(function() {
            redis.storeIntersectFromSortedSets(setId3, A.join([setId, setId2]), function() {
                redis.getAllItemsFromSortedSet(setId3, function(items) {
                    resume(function() {
                        Assert.isTrue(A.areEqual(["four"], items));
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testStoreUnionFromSortedSets: function() {
        onAfterFlushAllAndAllStringListsAdded(function() {
            redis.storeUnionFromSortedSets(setId3, A.join([setId, setId2]), function() {
                redis.getAllItemsFromSortedSet(setId3, function(items) {
                    resume(function() {
                        A.each(stringList2, function(x) {
                            if (!A.contains(stringList, x)) stringList.push(x);
                        });
                        Assert.isTrue(A.areEqual(stringList, items));
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

