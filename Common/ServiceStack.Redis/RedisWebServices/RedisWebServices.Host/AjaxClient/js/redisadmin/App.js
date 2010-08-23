/**
 * Created by IntelliJ IDEA.
 * User: mythz
 * Date: 14-Jun-2010
 * Time: 01:14:14
 * To change this template use File | Settings | File Templates.
 */

goog.provide("redisadmin.App");

goog.require('goog.json');

/**
 * @constructor 
 */
redisadmin.App = function()
{
    goog.events.EventTarget.call(this);

    var $this = this;
    this.log = goog.debug.Logger.getLogger('redisadmin.App');
    this.log.setLevel(goog.debug.Logger.Level.FINE);
    this.log.info('initializing App()');
    this.tree = null;
    this.history = null;
    this.currentPath = null;

    this.editor = new redisadmin.EditorViewController("tab_content", this);
    this.admin = new redisadmin.AdminViewController("tab_content", this);
    this.controllers = [this.editor, this.admin];
    this.deferredCalls = [];

    this.redis = new RedisClient();
    this.keyTypes = {};

    RedisClient.errorFn = function(e) {
        $this.log.severe("RedisClient.errorFn: ");
        $this.log.severe(goog.json.serialize(e));
    };

    redisadmin.App.initTabs();
    redisadmin.App.initSplitter();
}
goog.inherits(redisadmin.App, goog.events.EventTarget);
goog.addSingletonGetter(redisadmin.App);

redisadmin.App.NodeType = {
  UNLOADED: 'loading',
  KEY_GROUP: 'keyGroup',
  KEY: 'key'
};


redisadmin.App.prototype.setHistory = function(history, initPath)
{
    this.log.info('setHistory: ' + history + ": " + initPath);
    this.history = history;

    if (initPath)
    {
        app.loadPath(initPath);
    }
};

redisadmin.App.prototype.loadPath = function(path)
{
    if (path == this.currentPath)
    {
        //this.log.fine('loadPath: ignore current path: ' + path);
        return;
    }

    this.log.info('loadPath: ' + path + ", from: " + this.currentPath);

    var $this = this;
    var navRoot = Path.getFirstArg(path);

    if (navRoot == "key")
    {
        var keyName = Path.getFirstValue(path);

        $this.addDeferredCall_(
            function() {
                return $this.tree != null && $this.tree.getChildren().length > 0;
            },
            function(){
                $this.openKeyNode_(keyName, $this.tree);
                $this.setNavPath(Path.combine("key", keyName));
            });
    }
};

redisadmin.App.prototype.setNavPath = function(path)
{
    var $this = this;
    if (this.currentPath == path) return;
    
    this.log.info('setNavPath: from: ' + this.currentPath + " to" + path);
    this.currentPath = path;
    //Would ideally like to set without triggering but nothing lets me do this
    $this.history.setToken(path);
};

redisadmin.App.prototype.addDeferredCall_ = function(waitUntilFn, onSuccessFn)
{
    var $this = this;

    if (waitUntilFn())
    {
        onSuccessFn();
        return;
    }

    var deferredCall = new goog.async.ConditionalDelay(waitUntilFn);
    deferredCall.onSuccess = onSuccessFn;
    deferredCall.onFailure = function() {
        $this.log.severe('Failure: Time limit exceeded.');
    }
    deferredCall.start(100, 5000);

    for (var i=0, len = this.deferredCalls.length; i<len; i++)
    {
        var previousCall = this.deferredCalls[i];
        if (previousCall.isDone())
        {
            previousCall.dispose();
            this.deferredCalls[i] = deferredCall;
        }
    }
}

redisadmin.App.prototype.openKeyNode_ = function(keyName, parentNode)
{
    var $this = this;

    if (!keyName || !parentNode) return;

    //Navigate backwards because sub keys navigate down the wrong path, i.e. employee & employeeterritory
    for (var children = parentNode.getChildren(), i=children.length-1; i>=0; i--)
    {
        var childNode = children[i];
        var nodeLabel = this.getNodeLabel_(childNode);

        if (goog.string.startsWith(keyName, nodeLabel))
        {
            if (this.isKeyGroup_(childNode))
            {
                if (nodeLabel == keyName)
                {
                    this.showKeyGroup(childNode);
                }
                else
                {
                    childNode.expand();

                    this.addDeferredCall_(
                        function()
                        {
                            return childNode.nodeType != redisadmin.App.NodeType.UNLOADED;
                        },
                        function()
                        {
                            $this.openKeyNode_(keyName, childNode);
                        });
                }
            }
            else
            {
                childNode.select();
                this.showKey(childNode.getHtml());
            }
            break;
        }
    }
};

redisadmin.App.prototype.showKeyGroup = function(parentNode)
{
    var $this = this;

    var nodeLabel = $this.getNodeLabel_(parentNode);

    var onSuccess = function() {        
        $this.editor.showKeyGroup(nodeLabel, $this.getChildKeys_(parentNode));
        parentNode.select();
    };

    if (parentNode.nodeType == redisadmin.App.NodeType.UNLOADED)
    {
        this.loadChildren_(parentNode, onSuccess);
    }
    else
    {
        onSuccess();
    }
};

redisadmin.App.prototype.init = function(path)
{
};
redisadmin.App.initTabs = function()
{
    var tabBar = new goog.ui.TabBar();
    tabBar.decorate(goog.dom.getElement('tab'));

    // Handle SELECT events dispatched by tabs.
    goog.events.listen(tabBar, goog.ui.Component.EventType.SELECT,
        function(e) {
            var tabSelected = e.target;
            var contentElement = goog.dom.getElement(tabBar.getId() + '_content');
            goog.dom.setTextContent(contentElement,
                'You selected the "' + tabSelected.getCaption() + '" tab.');
        });
};

redisadmin.App.initSplitter = function()
{
    var lhs = new goog.ui.Component();
    var rhs = new goog.ui.Component();
    // Set up splitpane with already existing DOM.
    var splitpane = new goog.ui.SplitPane(lhs, rhs, goog.ui.SplitPane.Orientation.HORIZONTAL);

    splitpane.setInitialSize(300);
    splitpane.setHandleSize(6);
    splitpane.decorate(document.getElementById('container'));

    var headerSize = goog.style.getContentBoxSize(goog.dom.getElement("header"));
    var footerSize = goog.style.getContentBoxSize(goog.dom.getElement("footer"));

    // Takes a goog.math.Size object containing the viewport size, and updates
    // the UI accordingly.
    function updateUi(size)
    {
        var height = size.height - headerSize.height - footerSize.height - 42;
        var width = size.width;
        var resizeTo = new goog.math.Size(width, height);
        splitpane.setSize(resizeTo);
    }

    // Initialize the layout.
    updateUi(goog.dom.getViewportSize());

    // Start listening for viewport size changes.
    var vsm = new goog.dom.ViewportSizeMonitor();
    goog.events.listen(vsm, goog.events.EventType.RESIZE, function(e) {
        updateUi(vsm.getSize());
    });
};

/**
 * @return {object}
 */
redisadmin.App.prototype.getRedisClient = function()
{
    return this.redis;
}

/**
 * @param {string}
 */
redisadmin.App.prototype.searchInMainNav = function(query)
{
    var $this = this;

    this.redis.searchKeysGroup(query, function(keysWithChildCounts) {
        $this.fetchTypesForValidKeys(keysWithChildCounts);

        if ($this.tree)
        {
            $this.tree.dispose();
            $this.tree = null;
        }
        $this.tree = new goog.ui.tree.TreeControl('root');
        var childCount = O.keys(keysWithChildCounts).length;
        $this.tree.setHtml(query + "<em>(" + O.keys(keysWithChildCounts).length + ")</em>");
        $this.tree.setShowExpandIcons(true);
        $this.addChildNodes_($this.tree, keysWithChildCounts);

        goog.dom.getElement("mainNav").innerHTML = "<div id='tree'></div>";
        $this.tree.render(goog.dom.getElement("tree"));
        
        goog.events.listen($this.tree, goog.events.EventType.CHANGE, function(e)
        {
            var selectedItem = e.currentTarget.getTree().getSelectedItem();
            var nodeLabel = $this.getNodeLabel_(selectedItem);
            $this.loadPath(Path.combine("key", nodeLabel));
        });

        var fetchKeyTypes = [];
        $this.tree.forEachChild(function(child, i){
           if (!$this.isKeyGroup_(child))
           {
               var key = $this.getNodeLabel_(child);
               if (!$this.keyTypes[key]) fetchKeyTypes.push(key);
           }
        });
        $this.fetchKeyTypes(fetchKeyTypes, function(){
           $this.updateNodeTypes_($this.tree);
        });

    });
};

redisadmin.App.prototype.getChildKeys_ = function(parentNode)
{
    var childKeys = [];
    for (var i=0, children = parentNode.getChildren(); i<children.length; i++)
    {
        var childNode = children[i];
        if (!this.isKeyGroup_(childNode))
        {
            var key = this.getNodeLabel_(childNode);
            childKeys.push(key);
        }
    }
    return childKeys;
}

redisadmin.App.prototype.addChildNodes_ = function(parentNode, keysWithChildCounts)
{
    var $this = this;

    for (var key in keysWithChildCounts)
    {
        var childCount = keysWithChildCounts[key];
        var childNode = parentNode.getTree().createNode();

        var htmlCount = childCount > 1 ? "<em>(" + childCount + ")</em>" : "";
        childNode.setHtml(key + htmlCount);

        var hasChildrenToLoad = childCount > 1;
        childNode.nodeType = hasChildrenToLoad
                ? redisadmin.App.NodeType.UNLOADED
                : redisadmin.App.NodeType.KEY_GROUP;

        if (hasChildrenToLoad) {
            var loadingNode = parentNode.getTree().createNode();
            loadingNode.setHtml("<span class='loading'>loading...</span>");

            goog.events.listenOnce(childNode,
                goog.ui.tree.BaseNode.EventType.BEFORE_EXPAND,
                function (e) {
                    $this.loadChildren_(e.currentTarget);
                }
            );

            childNode.add(loadingNode);
        }

        parentNode.add(childNode);
    }
}

redisadmin.App.prototype.loadChildren_ = function(parentNode, callbackFn)
{
    var $this = this;

    if (parentNode.nodeType != redisadmin.App.NodeType.UNLOADED)
    {
        return;
    }

    var keyGroup = $this.getNodeLabel_(parentNode);
    $this.redis.searchKeysGroup(keyGroup + ":*", function(keysWithChildCounts) {
        $this.fetchTypesForValidKeys(keysWithChildCounts, function() {
            $this.updateNodeTypes_(parentNode);
        });

        $this.addChildNodes_(parentNode, keysWithChildCounts);
        parentNode.removeChildAt(0); //remove loading node
        parentNode.nodeType = redisadmin.App.NodeType.KEY_GROUP;

        if (callbackFn) callbackFn();
    });
}

redisadmin.App.prototype.updateNodeTypes_ = function(parentNode, keyTypes)
{
    var $this = this;
    keyTypes = keyTypes || this.keyTypes;
    parentNode.forEachChild(function(child, i)
    {
        var label = $this.getNodeLabel_(child);
        var keyType = keyTypes[label];

        //$this.log.info($this.getNodeLabel_(parentNode) + "> navTo: " + label + " (" + keyType + ")");
        if (keyType)
        {
            child.setIconClass("goog-tree-icon goog-tree-file-icon tnode-" + keyType);
            child.nodeType = redisadmin.App.NodeType.KEY;
        }
    });
};

redisadmin.App.prototype.isKeyGroup_ = function(node)
{
    return node.getHtml().indexOf("<em>") != -1;
};

redisadmin.App.prototype.getNodeLabel_ = function(node)
{
    var nodeLabel = node.getHtml();
    if (nodeLabel.indexOf("<em>") != -1)
    {
        nodeLabel = nodeLabel.substring(0, nodeLabel.indexOf("<em>"));
    }
    return nodeLabel;
};

redisadmin.App.prototype.fetchKeyTypes = function(keys, callbackFn)
{
    var $this = this;
    this.redis.getEntryTypes(keys, function(keysWithTypes)
    {
        for (var key in keysWithTypes)
        {
            $this.keyTypes[key] = keysWithTypes[key];
        }
        if (callbackFn) callbackFn();
    });
}

redisadmin.App.prototype.fetchTypesForValidKeys = function(keysWithChildCounts, callbackFn)
{
    var $this = this;

    var keysToFetch = [];
    for (var key in keysWithChildCounts)
    {
        var isValidKey = keysWithChildCounts[key] == 1;
        if (isValidKey && !this.keyTypes[key])
        {
            keysToFetch.push(key);
        }
    }
    if (keysToFetch.length > 0)
    {
        var ignoreLargeKeySpace = keysToFetch > 10000;
        if (ignoreLargeKeySpace) return;

        $this.log.fine("Fetching types for '" + keysToFetch.length + "' keys");

        this.fetchKeyTypes(keysToFetch, callbackFn);
    }
};

redisadmin.App.prototype.getKeyType = function(key, callbackFn)
{
    var $this = this;

    if (this.keyTypes[key])
    {
        callbackFn(this.keyTypes[key]);
    }
    else
    {
        $this.log.warning("key for '" + key + "' not found, fetching...");

        this.redis.getEntryType(key, function(keyType){
            $this.keyTypes[key] = keyType;
            callbackFn(keyType);
        });
    }
}

/**
 * Display information about the key in the main area
 * @param {string} the key
 * @param {boolean=} Whether to show the results in a new tab or the main tab.
 */
redisadmin.App.prototype.showKey = function(key, inNewTab)
{
    var $this = this;

    this.getKeyType(key, function(keyType)
    {
        if (keyType == "String")
        {
            $this.redis.getValue(key, function(value)
            {
                $this.editor.showKeyDetails(key, value);
            });
        }
        else if (keyType == "List")
        {
            $this.redis.getAllItemsFromList(key, function(items)
            {
                $this.editor.showKeyDetails(key, S.toString(items));
            });
        }
        else if (keyType == "Set")
        {
            $this.redis.getAllItemsFromSet(key, function(items)
            {
                $this.editor.showKeyDetails(key, S.toString(items));
            });
        }
        else if (keyType == "SortedSet")
        {
            $this.redis.getAllItemsFromSortedSet(key, function(itemsWithScores)
            {
                $this.editor.showKeyDetails(key, S.toString(itemsWithScores));
            });
        }
        else if (keyType == "Hash")
        {
            $this.redis.getAllEntriesFromHash(key, function(kvps)
            {
                $this.editor.showKeyDetails(key, S.toString(kvps));
            });
        }
        else
        {
            $this.log.severe("Unknown KeyType: " + keyType);
        }
    });
};
