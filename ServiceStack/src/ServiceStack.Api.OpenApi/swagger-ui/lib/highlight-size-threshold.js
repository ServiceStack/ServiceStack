// Keep Swagger UI's response-size limit and allow updated responses to be highlighted.
(function () {
  var configure = hljs.configure;
  var highlightElement = hljs.highlightElement;
  var highlightSizeThreshold = null;

  hljs.configure = function (options) {
    var threshold = options.highlightSizeThreshold;
    highlightSizeThreshold = typeof threshold === 'number' ? threshold : null;
    configure.call(hljs, options);
  };

  hljs.highlightElement = function (element) {
    if (highlightSizeThreshold === null || element.textContent.length < highlightSizeThreshold) {
      delete element.dataset.highlighted;
      return highlightElement.call(hljs, element);
    }
  };
}());
