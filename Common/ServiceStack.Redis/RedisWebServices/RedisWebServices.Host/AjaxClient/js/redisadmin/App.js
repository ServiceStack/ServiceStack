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

    this.redis = new RedisClient();
    RedisClient.errorFn = function(e) {
        console.log(e);
        alert(e);
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
            $this.showKey(selectedItem.getHtml());
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
                    var nodeLabel = e.currentTarget.getHtml();
                    var keyGroup = nodeLabel.indexOf("<em>") != -1
                            ? nodeLabel.substring(0, nodeLabel.indexOf("<em>"))
                            : nodeLabel;

                    $this.redis.searchKeysGroup(keyGroup + ":*", function(keysWithChildCounts) {
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

/**
 * Display information about the key in the main area
 * @param {string} the key
 * @param {boolean=} Whether to show the results in a new tab or the main tab.
 */
redisadmin.App.prototype.showKey = function(key, inNewTab)
{
    this.redis.getValue(key, function(value)
    {
       var html = "<h3>" + key + "</h3>"
                + "<textarea class='key-value'>" + value + "</textarea>";
        
        goog.dom.getElement('tab_content').innerHTML = html;
    });
};
