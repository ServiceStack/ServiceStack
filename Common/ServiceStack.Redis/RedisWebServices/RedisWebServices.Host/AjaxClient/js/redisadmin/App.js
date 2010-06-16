/**
 * Created by IntelliJ IDEA.
 * User: mythz
 * Date: 14-Jun-2010
 * Time: 01:14:14
 * To change this template use File | Settings | File Templates.
 */

goog.provide("redisadmin.App");

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

    this.redis = new RedisClient();
    this.keyTypes = {};

    RedisClient.errorFn = function(e) {
        $this.log.error("Unknown KeyType: " + keyType);
    };

    redisadmin.App.initTabs();
    redisadmin.App.initSplitter();
}
goog.inherits(redisadmin.App, goog.events.EventTarget);
goog.addSingletonGetter(redisadmin.App);

redisadmin.App.initTabs = function()
{
    var tabBar = new goog.ui.TabBar();
    tabBar.decorate(goog.dom.getElement('tab'));

    // Handle SELECT events dispatched by tabs.
    goog.events.listen(tabBar, goog.ui.Component.EventType.SELECT,
        function(e) {
            var tabSelected = e.target;
            var contentElement = goog.dom.getElement(tabBar.getId() +
                    '_content');
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

        /*
        goog.dom.setTextContent(goog.dom.getElement('tab_content'),
                size.toString()
                        + ", header: " + headerSize.toString()
                        + ", footer: " + footerSize.toString()
                );
        */
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

        var tree = new goog.ui.tree.TreeControl('root');
        var childCount = O.keys(keysWithChildCounts).length;
        tree.setHtml(query + "<em>(" + O.keys(keysWithChildCounts).length + ")</em>");
        tree.setShowExpandIcons(true);
        $this.addChildNodes_(tree, keysWithChildCounts);

        goog.dom.getElement("mainNav").innerHTML = "<div id='tree'></div>";
        tree.render(goog.dom.getElement("tree"));
        
        goog.events.listen(tree, goog.events.EventType.CHANGE, function(e)
        {
            var selectedItem = e.currentTarget.getTree().getSelectedItem();
            if ($this.isKeyGroup_(selectedItem))
            {
                $this.showKeyGroup(selectedItem);
            }
            else
            {
                $this.showKey(selectedItem.getHtml());
            }
        });
    });
};

redisadmin.App.prototype.addChildNodes_ = function(parentNode, keysWithChildCounts)
{
    var $this = this;
    var tree = parentNode.getTree();

    for (var key in keysWithChildCounts)
    {
        var childCount = keysWithChildCounts[key];
        var childNode = tree.createNode();

        var htmlCount = childCount > 1 ? "<em>(" + childCount + ")</em>" : "";
        childNode.setHtml(key + htmlCount);

        if (childCount > 1) {
            var loadingNode = parentNode.getTree().createNode();
            loadingNode.setHtml("<span class='loading'>loading...</span>");

            goog.events.listenOnce(childNode,
                goog.ui.tree.BaseNode.EventType.BEFORE_EXPAND,
                function (e) {
                    var parentNode = e.currentTarget;

                    var keyGroup = $this.getNodeLabel_(e.currentTarget);

                    $this.redis.searchKeysGroup(keyGroup + ":*", function(keysWithChildCounts) {
                        $this.fetchTypesForValidKeys(keysWithChildCounts);

                        parentNode.removeChildAt(0);
                        $this.addChildNodes_(parentNode, keysWithChildCounts);
                    });
                }
            );

            childNode.add(loadingNode);
        }

        parentNode.add(childNode);
    }
}

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

redisadmin.App.prototype.fetchTypesForValidKeys = function(keysWithChildCounts)
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

        this.redis.getEntryTypes(keysToFetch, function(keysWithTypes)
        {
            for (var key in keysWithTypes)
            {
                $this.keyTypes[key] = keysWithTypes[key];
            }
        });
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
                $this.showKeyDetails(key, value);
            });
        }
        else if (keyType == "List")
        {
            $this.redis.getAllItemsFromList(key, function(items)
            {
                $this.showKeyDetails(key, S.toString(items));
            });
        }
        else if (keyType == "Set")
        {
            $this.redis.getAllItemsFromSet(key, function(items)
            {
                $this.showKeyDetails(key, S.toString(items));
            });
        }
        else if (keyType == "SortedSet")
        {
            $this.redis.getAllItemsFromSortedSet(key, function(itemsWithScores)
            {
                $this.showKeyDetails(key, S.toString(itemsWithScores));
            });
        }
        else if (keyType == "Hash")
        {
            $this.redis.getAllEntriesFromHash(key, function(kvps)
            {
                $this.showKeyDetails(key, S.toString(kvps));
            });
        }
        else
        {
            $this.log.severe("Unknown KeyType: " + keyType);
        }
    });
};

redisadmin.App.prototype.showKeyDetails = function(key, textValue)
{
    var html = "<h3>" + key + "</h3>"
             + "<textarea class='key-value'>" + textValue + "</textarea>";

    try
    {
        var obj = JSV.parse(textValue);
        html += jLinq.from(obj).toTable();
    }
    catch (e) {
        $this.log.severe("Error parsing key: " + key + ", Error: " + e);
    }

    goog.dom.getElement('tab_content').innerHTML = html;
}

redisadmin.App.prototype.showKeyGroup = function(parentNode, inNewTab)
{
    var $this = this;

    var childKeys = [], parentLabel = this.getNodeLabel_(parentNode);
    for (var i=0, children = parentNode.getChildren(); i<children.length; i++)
    {
        var childNode = children[i];
        if (!this.isKeyGroup_(childNode))
        {
            var key = this.getNodeLabel_(childNode);
            childKeys.push(key);
        }
    }
    if (childKeys.length == 0)
    {
        this.log.warning("no keys in keyGroup: " + parentLabel);
        return;
    }
    this.redis.getValues(childKeys, function(values){
        var rows = [];
        var sbJsv = [];
        for (var i=0; i<childKeys.length; i++)
        {
            var jsvText = values[i];
            sbJsv.push(sbJsv.length == 0 ? '[' : ',');
            sbJsv.push(jsvText);

            var row = {Key:childKeys[i]};
            var obj = JSV.parse(jsvText);
            for (var k in obj)
            {
                row[k] = obj[k];
            }
            rows.push(row);
        }
        sbJsv.push(']');

        //$this.benchmarkTextFormats(sbJsv.join(''), goog.json.serialize(rows));

        var html = "<h3>" + parentLabel + "</h3>"
                 + "<textarea class='key-value'>" + goog.json.serialize(rows) + "</textarea>";

        html += jLinq.from(rows).toTable();

        goog.dom.getElement('tab_content').innerHTML = html;

        //console.log(map);
    });
};

redisadmin.App.prototype.benchmarkTextFormats = function(jsv, json)
{
    var tracer = goog.debug.Trace.startTracer('deserializer speed tests');
    goog.debug.Trace.addComment('BEGIN json vs jsv');

    var jsonObj = goog.json.parse(json);
    goog.debug.Trace.addComment('dserialize json');

    var jsvObj = JSV.parse(jsv);
    goog.debug.Trace.addComment('dserialize jsv');

    goog.debug.Trace.addComment('END json vs jsv');

    goog.debug.Trace.stopTracer(tracer);

    var results = goog.debug.Trace.getFormattedTrace();

    goog.dom.getElement('tab_content').innerHTML
            = "<pre>" + goog.string.htmlEscape(results) + "</pre>";

//        console.log("JSON");
//        console.log(jsonObj);
//        console.log("JSV");
//        console.log(jsvObj);
};