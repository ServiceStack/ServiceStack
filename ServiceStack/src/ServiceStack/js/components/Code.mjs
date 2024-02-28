// Requires CopyLine, CopyIcon global components & highlightjs directive
import { ref, onMounted, computed, watch } from "vue"
import { ApiResult, lastLeftPart, queryString, trimEnd } from "@servicestack/client"
import { useClient, useMetadata, useUtils } from "@servicestack/vue"
const BaseUrl = globalThis.BaseUrl || globalThis.Server?.app.baseUrl || lastLeftPart(trimEnd(document.baseURI,'/'),'/') 
const Usages = {
    csharp: `using ServiceStack;
using ServiceStack.Text;

var client = new JsonApiClient("${BaseUrl}");

var api = await client.ApiAsync(new Hello {
    //...
});

// Quickly inspect response
api.Response.PrintDump();`,
    typescript: `import { JsonServiceClient, Inspect } from '@servicestack/client'
import { Hello } from './dtos'

const client = new JsonServiceClient('${BaseUrl}')

const api = await client.api(new Hello({
    //...
}))

Inspect.printDump(api.response)`,
    mjs: `import { JsonServiceClient, Inspect } from '@servicestack/client'
import { Hello } from './dtos.mjs'

const client = new JsonServiceClient('${BaseUrl}')

const api = await client.api(new Hello({
    //...
}))

Inspect.printDump(api.response)`,
    dart: `import 'dart:io';
import 'dart:typed_data';
import 'package:servicestack/web_client.dart'
  if (dart.library.io) 'package:servicestack/client.dart';
import 'package:servicestack/inspect.dart';

var client = ClientFactory.api('${BaseUrl}');

var response = await client.send(Hello(
    //...
));

Inspect.printDump(response);`,
    java: `import net.servicestack.client.*;
import java.util.Collections;

var client = new JsonServiceClient("${BaseUrl}");

var response = client.send(new Hello()
    //...
);

Inspect.printDump(response);`,
    kotlin: `package myapp
import net.servicestack.client.*

val client = JsonServiceClient("${BaseUrl}")

val response = client.send(Hello().apply {
    //...
});

Inspect.printDump(response)`,
    python: `from servicestack import JsonServiceClient
from my_app.dtos import *

client = JsonServiceClient('${BaseUrl}')

response = client.send(Hello(
    #...
))

printdump(response)`,
    php: `use ServiceStack\\JsonServiceClient;
use ServiceStack\\Inspect;
use dtos\\Hello;

$client = new JsonServiceClient('${BaseUrl}');

/** @var {HelloResponse} $response */
$response = $client->send(new Hello(
    //...
));

Inspect::printDump(response);`,
    swift: `import Foundation
import ServiceStack

let client = JsonServiceClient(baseUrl:"${BaseUrl}")

let request = Hello()
//...
let response = try client.send(request)

Inspect.printDump(response)`,
    vbnet: `Imports ServiceStack
Imports ServiceStack.Text

Dim client = New JsonApiClient("${BaseUrl}")

Dim api = Await client.ApiAsync(New Hello() With {
    '...
})

' Quickly inspect response

api.Response.PrintDump()`,
    fsharp: `open ServiceStack
open ServiceStack.Text

let client = new JsonApiClient("${BaseUrl}")

let api = client.Api(new Hello(
    //...
))

// Quickly inspect response
api.Response.PrintDump()`
}
const InstallTool = {
    template:`<h2 class="text-lg p-4">
        To easily update DTOs for all APIs install the 
        <em><b>x</b></em> <a class="text-blue-600 underline" href="https://docs.servicestack.net/dotnet-tool">dotnet tool</a>
    </h2>
    
    <CopyLine prefix="$ " text="dotnet tool install --global x" />
    
    <h2 class="text-lg p-4">To generate all DTOs for <b>{{BaseUrl}}</b> run:</h2>
    <CopyLine prefix="$ " :text="create" />
    <h2 class="text-lg p-4">Once generated, the DTOs can be updated with:</h2>
    <CopyLine prefix="$ " :text="update" />`,
    props:['lang'],
    setup(props) {
        const create = `x ${props.lang} ${BaseUrl}`
        const update = `x ${props.lang}`
        return { BaseUrl, create, update }
    }
}
const components = { InstallTool }
const CSharp = {
    components,
    template:`<div>
        <h2 class="text-2xl pl-4 pb-3 border-b pt-4 pb-3 w-full bg-white">Call this API from C#</h2>
        <p class="text-lg p-4">
            <b class="mr-2">1.</b> Include <b>ServiceStack.Client</b> package in your projects<em>.csproj</em>
        </p>
        <CopyLine :text="pkg" />
        <div class="text-lg p-4 flex">
            <div><b class="mr-2">2.</b> Copy the DTOs source code for this API</div>
            <CopyIcon class="ml-3" :text="src" title="Copy code" />
        </div>
        <div class="text-lg p-4">
            <b class="mr-2">3.</b> Use the API DTOs with the <em>JsonServiceClient</em> or <em>JsonApiClient</em><span class="text-gray-400">(net6+)</span>
        </div>
        <div class="bg-gray-50 border-y border-gray-200 p-4 overflow-auto">
          <div class="relative">
            <CopyIcon class="absolute right-0" :text="usage" title="Copy code" />
          </div>
          <pre><code class="language-csharp" v-highlightjs="usage"></code></pre>
        </div>
        <InstallTool lang="csharp" />
    </div>`,
    props:['src','usage'],
    setup() {
        return { pkg: `<PackageReference Include="ServiceStack.Client" Version="8.*" />` }
    }
}
const TypeScript = {
    components,
    template:`<div>
      <h2 class="text-2xl pl-4 pb-3 border-b pt-4 pb-3 w-full bg-white">Call this API from TypeScript</h2>
      <p class="text-lg p-4">
        <b class="mr-2">1.</b> Install <b>@servicestack/client</b> into your projects<em>package.json</em>
      </p>
      <CopyLine text="npm install @servicestack/client" />
      <div class="text-lg p-4 flex">
        <div><b class="mr-2">2.</b> Copy the DTOs source code for this API</div>
        <CopyIcon class="ml-3" :text="src" title="Copy code" />
      </div>
      <div class="text-lg p-4">
        <b class="mr-2">3.</b> Use the API DTOs with the typed <em>JsonServiceClient</em>
      </div>
      <div class="bg-gray-50 border-y border-gray-200 p-4 overflow-auto">
        <div class="relative">
          <CopyIcon class="absolute right-0" :text="usage" title="Copy code" />
        </div>
        <pre><code class="language-typescript" v-highlightjs="usage"></code></pre>
      </div>
      <InstallTool lang="typescript" />
  </div>`,
    props:['src','usage']
}
const Mjs = {
    components,
    template:`<div>
    <h2 class="text-2xl pl-4 pb-3 border-b pt-4 pb-3 w-full bg-white">Call this API from a JavaScript Module</h2>
    <p class="text-lg p-4">
      <b class="mr-2">1.</b> Save <a class="text-blue-600 underline" href="https://unpkg.com/@servicestack/client@2/dist/servicestack-client.mjs">servicestack-client.mjs</a>
      to your project
    </p>
    <div class="text-lg p-4 flex">
      <div><b class="mr-2">2.</b> Define an <a class="text-blue-600 underline" href="https://docs.servicestack.net/javascript-add-servicestack-reference">Import Map</a> referencing its saved location</div>
    </div>
    <div class="p-4 bg-gray-50 border-y overflow-auto">
      <div class="relative">
        <CopyIcon class="absolute right-0" :text="mjsImportMapDecoded" title="Copy code" />
      </div>
      <pre><code class="language-html" v-highlightjs="mjsImportMapDecoded"></code></pre>
    </div>
    
    <div class="text-lg p-4 flex">
      <div><b class="mr-2">2.</b> Copy the DTOs source code for this API</div>
      <CopyIcon class="ml-3" :text="src" title="Copy code" />
    </div>
    <div class="text-lg p-4">
      <b class="mr-2">3.</b> Use the API DTOs with the typed <em>JsonServiceClient</em>
    </div>
    <div class="bg-gray-50 border-y border-gray-200 p-4 overflow-auto">
      <div class="relative">
        <CopyIcon class="absolute right-0" :text="usage" title="Copy code" />
      </div>
      <pre><code class="language-javascript" v-highlightjs="usage"></code></pre>
    </div>
    <InstallTool lang="mjs" />
    </div>`,
    props:['src','usage'],
    setup() {
        let mjsImportMap = `&lt;script type="importmap"&gt;
{
    "imports": {
        "@servicestack/client": "/js/servicestack-client.mjs"
    }
}
&lt;/script&gt;`
        return { mjsImportMapDecoded: mjsImportMap.replaceAll('&lt;','<').replaceAll('&gt;','>') }
    }
}
const Dart = {
    components,
    template:`<div>
    <h2 class="text-2xl pl-4 pb-3 border-b pt-4 pb-3 w-full bg-white">Call this API from Dart</h2>
    <p class="text-lg p-4">
      <b class="mr-2">1.</b> Include <b>servicestack</b> package in your projects<em>pubspec.yaml</em>
    </p>
    <CopyLine text="servicestack: ^3.0.0" />
    <div class="text-lg p-4 flex">
      <div><b class="mr-2">2.</b> Copy the DTOs source code for this API</div>
      <CopyIcon class="ml-3" :text="src" title="Copy code" />
    </div>
    <div class="text-lg p-4">
      <b class="mr-2">3.</b> Use the API DTOs with the typed <em>JsonServiceClient</em>
    </div>
    <div class="bg-gray-50 border-y border-gray-200 p-4 overflow-auto">
      <div class="relative">
        <CopyIcon class="absolute right-0" :text="usage" title="Copy code" />
      </div>
      <pre><code class="language-dart" v-highlightjs="usage"></code></pre>
    </div>
    <InstallTool lang="dart" />
    </div>`,
    props:['src','usage']
}
const Java = {
    components,
    template:`<div>
    <h2 class="text-2xl pl-4 pb-3 border-b pt-4 pb-3 w-full bg-white">Call this API from Java</h2>
    <p class="text-lg p-4">
      <b class="mr-2">1.</b> Include <b>net.servicestack:client</b> package in your projects<em>build.gradle</em>
    </p>
    <CopyLine text="implementation 'net.servicestack:client:1.1.0'" />
    <div class="text-lg p-4 flex">
      <div><b class="mr-2">2.</b> Copy the DTOs source code for this API</div>
      <CopyIcon class="ml-3" :text="src" title="Copy code" />
    </div>
    <div class="text-lg p-4">
      <b class="mr-2">3.</b> Use the API DTOs with the typed <em>JsonServiceClient</em>
    </div>
    <div class="bg-gray-50 border-y border-gray-200 p-4 overflow-auto">
      <div class="relative">
        <CopyIcon class="absolute right-0" :text="usage" title="Copy code" />
      </div>
      <pre><code class="language-java" v-highlightjs="usage"></code></pre>
    </div>
    <InstallTool lang="java" />
    </div>`,
    props:['src','usage']
}
const Kotlin = {
    components,
    template:`<div>
    <h2 class="text-2xl pl-4 pb-3 border-b pt-4 pb-3 w-full bg-white">Call this API from Kotlin</h2>
    <p class="text-lg p-4">
      <b class="mr-2">1.</b> Include <b>net.servicestack:client</b> package in your projects<em>build.gradle</em>
    </p>
    <CopyLine text="implementation 'net.servicestack:client:1.1.0'" />
    <div class="text-lg p-4 flex">
      <div><b class="mr-2">2.</b> Copy the DTOs source code for this API</div>
      <CopyIcon class="ml-3" :text="src" title="Copy code" />
    </div>
    <div class="text-lg p-4">
      <b class="mr-2">3.</b> Use the API DTOs with the typed <em>JsonServiceClient</em>
    </div>
    <div class="bg-gray-50 border-y border-gray-200 p-4 overflow-auto">
      <div class="relative">
        <CopyIcon class="absolute right-0" :text="usage" title="Copy code" />
      </div>
      <pre><code class="language-kotlin" v-highlightjs="usage"></code></pre>
    </div>
    <InstallTool lang="kotlin" />
    </div>`,
    props:['src','usage']
}
const Python = {
    components,
    template:`<div>
    <h2 class="text-2xl pl-4 pb-3 border-b pt-4 pb-3 w-full bg-white">Call this API from Python</h2>
    <p class="text-lg p-4">
        <b class="mr-2">1.</b> Include <b>servicestack</b> package in your projects<em>requirements.txt</em>
    </p>
    <CopyLine text="servicestack>=0.1.3" />
    <div class="text-lg p-4 flex">
      <div><b class="mr-2">2.</b> Copy the DTOs source code for this API</div>
      <CopyIcon class="ml-3" :text="src" title="Copy code" />
    </div>
    <div class="text-lg p-4">
      <b class="mr-2">3.</b> Use the API DTOs with the typed <em>JsonServiceClient</em>
    </div>
    <div class="bg-gray-50 border-y border-gray-200 p-4 overflow-auto">
      <div class="relative">
        <CopyIcon class="absolute right-0" :text="usage" title="Copy code" />
      </div>
      <pre><code class="language-python" v-highlightjs="usage"></code></pre>
    </div>
    <InstallTool lang="python" />
    </div>`,
    props:['src','usage']
}
const Php = {
    components,
    template:`<div>
      <h2 class="text-2xl pl-4 pb-3 border-b pt-4 pb-3 w-full bg-white">Call this API from PHP</h2>
      <p class="text-lg p-4">
        <b class="mr-2">1.</b> Include <b>servicestack/client</b> package in your projects<em>composer.json</em>
      </p>
      <CopyLine text="&quot;servicestack/client&quot;: &quot;^1.0&quot;" />
      <div class="text-lg p-4 flex">
        <div><b class="mr-2">2.</b> Copy the DTOs source code for this API</div>
        <CopyIcon class="ml-3" :text="src" title="Copy code" />
      </div>
      <div class="text-lg p-4">
        <b class="mr-2">3.</b> Use the API DTOs with the typed <em>JsonServiceClient</em>
      </div>
      <div class="bg-gray-50 border-y border-gray-200 p-4 overflow-auto">
        <div class="relative">
          <CopyIcon class="absolute right-0" :text="usage" title="Copy code" />
        </div>
        <pre><code class="language-php" v-highlightjs="usage"></code></pre>
      </div>
      <InstallTool lang="php" />
    </div>`,
    props:['src','usage']
}
const Swift = {
    components,
    template:`<div>
    <h2 class="text-2xl pl-4 pb-3 border-b pt-4 pb-3 w-full bg-white">Call this API from Swift</h2>
    <p class="text-lg p-4">
        <b class="mr-2">1.</b> Include <b>ServiceStack</b> package in your projects <em>Package.swift</em>
    </p>
    <div class="p-4 bg-gray-50 border-y overflow-auto">
      <div class="relative">
        <CopyIcon class="absolute right-0" :text="pkg" title="Copy code" />
      </div>
      <pre v-html="pkg"></pre>
    </div>
    <div class="text-lg p-4 flex">
      <div><b class="mr-2">2.</b> Copy the DTOs source code for this API</div>
      <CopyIcon class="ml-3" :text="src" title="Copy code" />
    </div>
    <div class="text-lg p-4">
      <b class="mr-2">3.</b> Use the API DTOs with the typed <em>JsonServiceClient</em>
    </div>
    <div class="bg-gray-50 border-y border-gray-200 p-4 overflow-auto">
      <div class="relative">
        <CopyIcon class="absolute right-0" :text="usage" title="Copy code" />
      </div>
      <pre><code class="language-swift" v-highlightjs="usage"></code></pre>
    </div>
    <InstallTool lang="swift" />
    </div>`,
    props:['src','usage'],
    setup() {
        let pkg = `dependencies: [
    .package(name: "ServiceStack", 
        url: "https://github.com/ServiceStack/ServiceStack.Swift.git", 
        Version(5,0,0)..&lt;Version(6,0,0)),
]`
        return { pkg }
    }
}
const VbNet = {
    components,
    template:`<div>
    <h2 class="text-2xl pl-4 pb-3 border-b pt-4 pb-3 w-full bg-white">Call this API from VB.NET</h2>
    <p class="text-lg p-4">
      <b class="mr-2">1.</b> Include <b>ServiceStack.Client</b> package in your projects<em>.csproj</em>
    </p>
    <CopyLine :text="pkg" />
    <div class="text-lg p-4 flex">
      <div><b class="mr-2">2.</b> Copy the DTOs source code for this API</div>
      <CopyIcon class="ml-3" :text="src" title="Copy code" />
    </div>
    <div class="text-lg p-4">
      <b class="mr-2">3.</b> Use the API DTOs with the <em>JsonServiceClient</em> or <em>JsonApiClient</em><span class="text-gray-400">(net6+)</span>
    </div>
    <div class="bg-gray-50 border-y border-gray-200 p-4 overflow-auto">
      <div class="relative">
        <CopyIcon class="absolute right-0" :text="usage" title="Copy code" />
      </div>
      <pre><code class="language-vbnet" v-highlightjs="usage"></code></pre>
    </div>
    <InstallTool lang="vbnet" />
    </div>`,
    props:['src','usage'],
    setup() {
        return { pkg: `<PackageReference Include="ServiceStack.Client" Version="8.*" />` }
    }
}
const FSharp = {
    components,
    template:`<div>
    <h2 class="text-2xl pl-4 pb-3 border-b pt-4 pb-3 w-full bg-white">Call this API from F#</h2>
    <p class="text-lg p-4">
      <b class="mr-2">1.</b> Include <b>ServiceStack.Client</b> package in your projects<em>.csproj</em>
    </p>
    <CopyLine :text="pkg" />
    <div class="text-lg p-4 flex">
      <div><b class="mr-2">2.</b> Copy the DTOs source code for this API</div>
      <CopyIcon class="ml-3" :text="src" title="Copy code" />
    </div>
    <div class="text-lg p-4">
      <b class="mr-2">3.</b> Use the API DTOs with the <em>JsonServiceClient</em> or <em>JsonApiClient</em><span class="text-gray-400">(net6+)</span>
    </div>
    <div class="bg-gray-50 border-y border-gray-200 p-4 overflow-auto">
      <div class="relative">
        <CopyIcon class="absolute right-0" :text="usage" title="Copy code" />
      </div>
      <pre><code class="language-fsharp" v-highlightjs="usage"></code></pre>
    </div>
    <InstallTool lang="fsharp" />
    </div>`,
    props:['src','usage'],
    setup() {
        return { pkg: `<PackageReference Include="ServiceStack.Client" Version="8.*" />` }
    }
}
export const LanguageComponents = { CSharp, TypeScript, Mjs, Dart, Java, Kotlin, Python, Php, Swift, VbNet, FSharp }
export const LangTypes = {
    CSharp:     ['csharp',     'C#'],
    TypeScript: ['typescript', 'TypeScript'],
    Mjs:        ['mjs',        'JS'],
    Dart:       ['dart',       'Dart'],
    Java:       ['java',       'Java'],
    Kotlin:     ['kotlin',     'Kotlin'],
    Python:     ['python',     'Python'],
    Php:        ['php',        'PHP'],
    Swift:      ['swift',      'Swift'],
    VbNet:      ['vbnet',      'VB'],
    FSharp:     ['fsharp',     'F#'],
}
export const Languages = Object.keys(LangTypes).reduce((acc,type) => {
    const lang = LangTypes[type][0]
    acc[lang] = { lang, type, name: LangTypes[type][1], component: LanguageComponents[type] };
    return acc
}, {})
export const Code = {
    components: LanguageComponents,
    template:`
      <div v-if="!requestType" class="w-full"><Alert>Could not find metadata for '{{op}}'</Alert></div>
      <div v-else class="w-full h-full">
      <nav class="w-full flex space-x-4 pl-2 py-2 border-b bg-white overflow-x-auto" aria-label="Tabs">
        <a v-for="(x,lang) in Languages" @click="select(lang)"
           :class="['cursor-pointer select-none', lang === selected ? 'bg-gray-100 text-gray-700' : 'text-gray-500 hover:text-gray-700', 'px-3 py-1 font-medium text-sm rounded-md']">
          {{x.name}}
        </a>
      </nav>
      <div class="flex" style="height:calc(100% - 45px)">
        <div class="flex-1 w-full lg:w-1/2 relative p-2 h-full overflow-x-auto">
          <div class="absolute right-4 flex flex-col">
            <div v-if="!showHelp" @click="showHelp=true" class="mb-1">
              <div class="cursor-pointer p-1 rounded-md border block border-gray-200 bg-white text-gray-500 hover:bg-gray-50">
                <svg class="w-6 h-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                  <path d="M11 18h2v-2h-2v2m1-16A10 10 0 0 0 2 12a10 10 0 0 0 10 10a10 10 0 0 0 10-10A10 10 0 0 0 12 2m0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8s8 3.59 8 8s-3.59 8-8 8m0-14a4 4 0 0 0-4 4h2a2 2 0 0 1 2-2a2 2 0 0 1 2 2c0 2-3 1.75-3 5h2c0-2.25 3-2.5 3-5a4 4 0 0 0-4-4z" fill="currentColor" />
                </svg>
              </div>
            </div>
            <CopyIcon v-if="activeLangSrc" :text="activeLangSrc" title="Copy code" />
          </div>
          <pre :key="selected" v-if="activeLangSrc" class=""><code :class="'language-' + selected" :lang="selected" v-highlightjs="activeLangSrc"></code></pre>
          <Loading v-else />
        </div>
        <div v-if="showHelp" class="flex-1 w-full lg:w-1/2 overflow-auto shadow-lg relative" style="min-width:585px;max-width:700px">
          <CloseButton @close="showHelp=false" />
          <component v-if="Languages[selected]?.component" :is="Languages[selected]?.component" :src="activeLangSrc" :usage="usage" class="" />
        </div>
      </div>
  </div>
    `,
    props: ['op'],
    setup(props) {
        const { pushState } = useUtils()
        const { typeOf, makeDto } = useMetadata()
        const client = useClient()
        const requestType = computed(() => typeOf(props.op))
        const showHelp = ref(true)
        const selected = ref('')
        const activeLangSrc = ref('')
        const api = ref(new ApiResult())
        let cleanSrc = src => src.trim()
        
        const usage = computed(() => (Usages[selected.value] || '').replace(/Hello/g,props.op))
        async function select(lang) {
            selected.value = lang
            pushState({ lang: lang === 'csharp' ? undefined : lang })
            const cacheKey = `${props.op}:${lang}`
            activeLangSrc.value = Cache[cacheKey] || ''
            if (!activeLangSrc.value) {
                const typesRequest = `Types${Languages[selected.value].type}`
                const requestDto = Object.assign(makeDto(typesRequest, {
                    IncludeTypes: `${props.op}.*`,
                    WithoutOptions: true,
                    MakeVirtual: false,
                    MakePartial: false,
                    AddServiceStackTypes: true,
                }, {
                    method: 'GET',
                    createResponse() { return String() },
                }))
                api.value = await client.api(requestDto)
                activeLangSrc.value = ''
                if (api.value.succeeded && api.value.response) {
                    activeLangSrc.value = Cache[cacheKey] = cleanSrc(api.value.response)
                }
            }
        }
        async function update() {
            const qs = queryString(location.search)
            await select(qs.lang || 'csharp')
        }
        
        onMounted(update)
        watch(() => props.op, update)
        
        return { requestType, Languages, usage, selected, select, activeLangSrc, showHelp, api }
    }
}
export default Code
