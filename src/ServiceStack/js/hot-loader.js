var _lastEtag = "";
(function shouldReload(){
    fetch("/reload/page.json?path" + encodeURIComponent(location.pathname) + "&eTag=" + _lastEtag)
    .then(function(res){ 
        if (res.status !== 200) {
            console.log("/reload/page failed: " + res.status)
            if (res.status != 404 && res.status != 405) setTimeout(shouldReload, 1000)
        } else {
            res.json().then(function(r){
                if (!_lastEtag) {
                    _lastEtag = r.eTag
                } else if (r.reload) {
                    console.log("page modified, reloading...")
                    location.href = location.href
                }
                setTimeout(shouldReload, 1000)
            })
        }
    })
    .catch(function(err){ console.log('fetch() error: ', err) })
})();
