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
        $this.log.severe("RedisClient.errorFn: ");
        $this.log.severe(JSV.serialize(e));
    };

    redisadmin.App.initTabs();
    redisadmin.App.initSplitter();
}
goog.inherits(redisadmin.App, goog.events.EventTarget);
goog.addSingletonGetter(redisadmin.App);

redisadmin.App.prototype.loadPath = function(path)
{
    this.log.info('Loading: ' + path);
};

redisadmin.App.prototype.setNavPath_ = function(path)
{
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

        var fetchKeyTypes = [];
        tree.forEachChild(function(child, i){
           if (!$this.isKeyGroup_(child))
           {
               var key = $this.getNodeLabel_(child);
               if (!$this.keyTypes[key]) fetchKeyTypes.push(key);
           }
        });
        $this.fetchKeyTypes(fetchKeyTypes, function(){
           $this.updateNodeTypes_(tree); 
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
                        $this.fetchTypesForValidKeys(keysWithChildCounts, function(){
                            $this.updateNodeTypes_(parentNode);
                        });

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

redisadmin.App.prototype.updateNodeTypes_ = function(parentNode, keyTypes)
{
    var $this = this;
    keyTypes = keyTypes || this.keyTypes;
    parentNode.forEachChild(function(child, i){
       var label = $this.getNodeLabel_(child);
       var keyType = keyTypes[label];
       //$this.log.info($this.getNodeLabel_(parentNode) + "> navTo: " + label + " (" + keyType + ")");
       if (keyType)
       {
           child.setIconClass("goog-tree-icon goog-tree-file-icon tnode-" + keyType);
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
    var sb = [];
    sb.push("<h2 class='key'>" + key + "</h2>");


    sb.push("<div id='keydetails-body'>");

    sb.push("<div id='toolbarViewKey' class='key-options goog-toolbar'>");
    sb.push("<div id='lnk-editkey' class='goog-toolbar-button nav-link'><span id='icon-edit' class='goog-inline-block'></span>edit</div>");
    sb.push("<div class='goog-toolbar-separator nav-separator'></div>");
    sb.push("</div>");

    sb.push("<div id='toolbarEditKey' class='goog-toolbar' style='display:none'>");
    sb.push("<div id='lnk-back' class='goog-toolbar-button nav-link'>« back</div>");
    sb.push("<hr/>");
    sb.push("<div class='goog-toolbar-separator nav-separator'></div>");
    sb.push("<div id='btnDeleteKey' class='goog-toolbar-button'><span id='icon-delete' class='goog-inline-block'></span>Delete</div>");
    sb.push("<hr/>");
    sb.push("<div id='btnSaveKey' class='goog-toolbar-button'><span id='icon-save' class='goog-inline-block'></span>Save</div>");
    sb.push("</div>");

    try
    {
        var obj = JSV.parse(textValue);

        sb.push("<div id='key-view'>");
        sb.push("<dl>");
        for (var k in obj)
        {
            sb.push("<dt>" + k + "</dt>"
                  + "<dd>" + obj[k] + "</dd>");
        }
        sb.push("</dl>");
        sb.push("</div>");
    }
    catch (e) {
        $this.log.severe("Error parsing key: " + key + ", Error: " + e);
    }

    sb.push("<div id='key-edit' style='display:none'>");
    sb.push("<textarea id='txtEntryValue'>" + textValue + "</textarea>");
    sb.push("</div>");

    sb.push("<div>");

    goog.dom.getElement('tab_content').innerHTML = sb.join('');

    var toolbarViewKey = new goog.ui.Toolbar();
    toolbarViewKey.decorate(goog.dom.getElement('toolbarViewKey'));

    var toolbarEditKey = new goog.ui.Toolbar();
    toolbarEditKey.decorate(goog.dom.getElement('toolbarEditKey'));

    var lnkEdit = goog.dom.getElement('lnk-editkey'),
        lnkBack = goog.dom.getElement('lnk-back'),
        btnDeleteKey = goog.dom.getElement('btnDeleteKey'),
        btnSaveKey = goog.dom.getElement('btnSaveKey'),
        txtEntryValue = goog.dom.getElement('txtEntryValue');

    var editMode = false;

    var toggleEditModeFn = function(e) {
        editMode = !editMode;
        goog.style.showElement(goog.dom.getElement('key-view'), !editMode);
        goog.style.showElement(goog.dom.getElement('key-edit'), editMode);
        goog.style.showElement(goog.dom.getElement('toolbarViewKey'), !editMode);
        goog.style.showElement(goog.dom.getElement('toolbarEditKey'), editMode);
    };
    goog.events.listen(lnkEdit, goog.events.EventType.CLICK, toggleEditModeFn);
    goog.events.listen(lnkBack, goog.events.EventType.CLICK, toggleEditModeFn);

    goog.events.listen(btnDeleteKey, goog.events.EventType.CLICK,
    function(e) {
        alert('delete');
    });

    goog.events.listen(btnSaveKey, goog.events.EventType.CLICK,
    function(e) {
        alert('updating: ' + goog.dom.getTextContent(txtEntryValue));
    });
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

        var html = "<div class='key-group'>"
                + "<input id='txtKeyGroupFilter' type='text' value='" + parentLabel + "' />"
                + '<div id="btnKeyGroupFilter" class="goog-custom-button goog-inline-block">Filter</div>'
                + "</div>";

        html += "<div id='keys-table'>" + jLinq.from(rows).toTable() + "</div>";

        goog.dom.getElement('tab_content').innerHTML = html;

        var txtKeyGroupFilter = goog.dom.getElement('txtKeyGroupFilter');
        var autoComplete = new goog.ui.AutoComplete.Basic(childKeys, txtKeyGroupFilter, false);
        autoComplete.setAllowFreeSelect(true);
        autoComplete.setAutoHilite(false);

        var filterFn = function(e)
        {
            var filteredRows = [], filterText = txtKeyGroupFilter.value;
            for (var i=0; i<rows.length; i++)
            {
                var row = rows[i];
                if (row.Key.indexOf(filterText) != -1)
                {
                    filteredRows.push(row);
                }
            }
            goog.dom.getElement('keys-table').innerHTML = jLinq.from(filteredRows).toTable();
        };

        goog.events.listen(autoComplete, goog.ui.AutoComplete.EventType.UPDATE, filterFn);

        var btnKeyGroupFilter = goog.dom.getElement("btnKeyGroupFilter");
        var button = goog.ui.decorate(btnKeyGroupFilter);
        button.setDispatchTransitionEvents(goog.ui.Component.State.ALL, true);
        goog.events.listen(btnKeyGroupFilter, goog.events.EventType.CLICK, filterFn);
    });
};
