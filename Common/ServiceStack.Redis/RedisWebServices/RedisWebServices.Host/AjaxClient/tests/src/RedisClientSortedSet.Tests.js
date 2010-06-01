var zsetId = "testzset";
var zsetId2 = "testzset2";
var zsetId3 = "testzset3";
var emptyzset = "testemptyzset";

var stringZSet = [];
var stringZSet2 = [];
var stringDoubleMap = {};
var firstItem = "one";
var lastItem = "four";
var lastItemScore = 4;

var startPos = '0';
var endPos = -1;

var onAfterFlushAllAndAllstringZSetsAdded = function(onSuccessFn, onErrorFn)
{
    var count = 0;
    redis.flushAll(function() {
        redis.addRangeToSortedSet(zsetId, RedisClient.convertArrayToItemWithScoresDto(stringZSet), function() {
            redis.addRangeToSortedSet(zsetId2, RedisClient.convertArrayToItemWithScoresDto(stringZSet2), function() {
                onSuccessFn();
            }, onErrorFn || failFn);
        }, onErrorFn || failFn);
    }, onErrorFn || failFn);
};
var onAfterFlushAllAndStringDoubleMap = function(onSuccessFn, onErrorFn)
{
    var count = 0;
    redis.flushAll(function() {
        redis.addRangeToSortedSet(zsetId, RedisClient.convertMapToItemWithScoresDto(stringDoubleMap), function() {
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

        stringZSet = ["one", "two", "three", "four" ];
        stringZSet2 = ["four", "five", "six", "seven"];
        stringDoubleMap = {'one':1, 'two':2, 'three':3, 'four':4};
    },

    tearDown: function() {
    },

    //--------------------------------------------- 
    // Tests 
    //---------------------------------------------

    testAddItemToSortedSet: function() {
        redis.flushAll(function() {
            redis.addItemToSortedSet(zsetId, testValue, 1, function() {
                redis.popItemWithHighestScoreFromSortedSet(zsetId, function(item) {
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
            redis.getAllItemsFromSortedSet(zsetId, function(items) {
                resume(function() {
                    Assert.isTrue(A.areEqual(O.keys(stringDoubleMap), items));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetAllItemsFromSortedSetDesc: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.getAllItemsFromSortedSetDesc(zsetId, function(items) {
                resume(function() {
                    Assert.isTrue(A.areEqual(O.keys(stringDoubleMap), items));
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetItemIndexInSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.getItemIndexInSortedSet(zsetId, lastItem, function(index) {
                resume(function() {
                    Assert.areEqual(O.count(stringDoubleMap) - 1, index);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetItemScoreInSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.getItemScoreInSortedSet(zsetId, lastItem, function(score) {
                resume(function() {
                    Assert.areEqual(lastItemScore, score);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testGetRangeFromSortedSetByLowestScore: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.getRangeFromSortedSetByLowestScore(zsetId, 2, 3, startPos, endPos, function(items) {
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
            redis.getRangeWithScoresFromSortedSet(zsetId, startPos, 2, function(itws) {
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
            redis.getRangeWithScoresFromSortedSetByLowestScore(zsetId, 2, 3, startPos, endPos, function(itws) {
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
            redis.getSortedSetCount(zsetId, function(count) {
                resume(function() {
                    Assert.areEqual(O.count(stringDoubleMap), count);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testIncrementItemInSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.incrementItemInSortedSet(zsetId, lastItem, 2, function(score) {
                resume(function() {
                    Assert.areEqual(lastItemScore + 2, score);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testPopItemWithHighestScoreFromSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.popItemWithHighestScoreFromSortedSet(zsetId, function(item) {
                resume(function() {
                    Assert.areEqual(lastItem, item);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testPopItemWithLowestScoreFromSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.popItemWithLowestScoreFromSortedSet(zsetId, function(item) {
                resume(function() {
                    Assert.areEqual(firstItem, item);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testRemoveItemFromSortedSet: function() {
        onAfterFlushAllAndStringDoubleMap(function() {
            redis.removeItemFromSortedSet(zsetId, lastItem, function(result) {
                redis.getAllItemsFromSortedSet(zsetId, function(items) {
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
            redis.removeRangeFromSortedSet(zsetId, startPos, 2, function(itemsRemovedCount) {
                redis.getAllItemsFromSortedSet(zsetId, function(items) {
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
            redis.removeRangeFromSortedSetByScore(zsetId, 1, 2, function(itemsRemovedCount) {
                redis.getAllItemsFromSortedSet(zsetId, function(items) {
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
            redis.sortedSetContainsItem(zsetId, lastItem, function(result) {
                resume(function() {
                    Assert.isTrue(result);
                });
            }, failFn);
        }, failFn);

        wait();
    },

    testStoreIntersectFromSortedSets: function() {
        onAfterFlushAllAndAllstringZSetsAdded(function() {
            redis.storeIntersectFromSortedSets(zsetId3, A.join([zsetId, zsetId2]), function() {
                redis.getAllItemsFromSortedSet(zsetId3, function(items) {
                    resume(function() {
                        Assert.isTrue(A.areEqual(["four"], items));
                    });
                }, failFn);
            }, failFn);
        }, failFn);

        wait();
    },

    testStoreUnionFromSortedSets: function() {
        onAfterFlushAllAndAllstringZSetsAdded(function() {
            redis.storeUnionFromSortedSets(zsetId3, A.join([zsetId, zsetId2]), function() {
                redis.getAllItemsFromSortedSet(zsetId3, function(items) {
                    resume(function() {
                        A.each(stringZSet2, function(x) {
                            if (!A.contains(stringZSet, x)) stringZSet.push(x);
                        });
                        Assert.isTrue(A.areEqual(stringZSet, items));
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

