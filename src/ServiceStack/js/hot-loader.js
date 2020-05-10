var __lastEtag = "";
(function (qs) {
    qs = qs.split('+').join(' ')
    var tokens, re = /[?&]?([^=]+)=([^&]*)/g
    while (tokens = re.exec(qs)) {
        if (tokens[1] === "scroll") {
            setTimeout(function(){ window.scrollTo(0, tokens[2]) }, 1);
            return
        }
    }
})(document.location.search);
(function shouldReload(){
    function _replace(uri, key, value, strip) {
        var re = new RegExp("([?&])" + key + "=.*?(&|$)", "i")
        return uri.match(re) ?
            uri.replace(re, strip ? '' : '$1' + key + "=" + value + '$2') :
            strip ? uri : uri + (uri.indexOf('?') !== -1 ? "&" : "?") + key + "=" + value
    }
    fetch("/hotreload/page.json?path=" + encodeURIComponent(location.pathname) + "&eTag=" + __lastEtag)
        .then(function(res){
            if (res.status !== 200) {
                console.log("hotreload failed: " + res.status)
                if (res.status !== 404 && res.status !== 405) setTimeout(shouldReload, 1000)
            } else {
                res.json().then(function(r){
                    if (r.eTag) {
                        __lastEtag = r.eTag
                    } 
                    if (r.reload) {
                        var scroll = (document.documentElement.scrollTop || document.body.scrollTop);
                        if (location.href.indexOf('#') >= 0)
                            location.reload();
                        else
                            location.href = _replace(location.href,'scroll', scroll, scroll === 0)
                    }
                    setTimeout(shouldReload, 1)
                })
            }
        })
        .catch(function(err){ console.log('hotreload error: ', err) })
})();