{{ true | assignTo: debug }}

{{ (debug ? '' : '[hash].min') | assignTo: min }}

{{ [`/css/bundle${min}.css`,`/js/lib.bundle${min}.js`,`/js/bundle${min}.js`] 
   | map => it.replace('[hash]','.*').filesFind()
   | flatten
   | map => it.VirtualPath.fileDelete() | end }}

{{ end | return }}

{{ ['/assets/css/'] | bundleCss({ minify:!debug, cache:!debug, disk:!debug, out:`/css/bundle${min}.css` }) }}

{{ ['content:/src/source.js',
    'content:/src/components/',
    '/assets/js/jquery.min.js',
    '/assets/js/',
    '/js/ss-utils.js',
    '/lib/@servicestack/client/index.js',
    '/dtos.js',
    ] | bundleJs({ minify:!debug, cache:!debug, disk:!debug, out:`/js/bundle${min}.js` }) }}
