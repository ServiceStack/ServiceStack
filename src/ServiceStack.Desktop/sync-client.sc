* Usage: x run sync-client.sc *

{{ const libs = { 
    '@servicestack/client/servicestack-client.min.js': 'https://unpkg.com/@servicestack/client/dist/servicestack-client.min.js',
} 
}}

#each libs
    it.Value |> urlContentsWithCache() |> to => js
    let path = `lib/js/${it.Key}`
    `writing file ${path}...`
    path.writeFile(js) |> end
/each
