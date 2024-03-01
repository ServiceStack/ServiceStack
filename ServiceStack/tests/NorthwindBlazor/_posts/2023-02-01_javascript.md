---
title: Simple, Modern JavaScript
summary: Learn about JS Modules, Vue 3 and available rich UI Components
tags: [js, dev]
image: https://images.unsplash.com/photo-1497515114629-f71d768fd07c?crop=entropy&fit=crop&h=1000&w=2000
author: Brandon Foley
---

<svg class="sm:float-left mr-8 w-24 h-24" style="margin-top:0" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 630 630">
<rect width="630" height="630" fill="#f7df1e"/>
<path d="m423.2 492.19c12.69 20.72 29.2 35.95 58.4 35.95 24.53 0 40.2-12.26 40.2-29.2 0-20.3-16.1-27.49-43.1-39.3l-14.8-6.35c-42.72-18.2-71.1-41-71.1-89.2 0-44.4 33.83-78.2 86.7-78.2 37.64 0 64.7 13.1 84.2 47.4l-46.1 29.6c-10.15-18.2-21.1-25.37-38.1-25.37-17.34 0-28.33 11-28.33 25.37 0 17.76 11 24.95 36.4 35.95l14.8 6.34c50.3 21.57 78.7 43.56 78.7 93 0 53.3-41.87 82.5-98.1 82.5-54.98 0-90.5-26.2-107.88-60.54zm-209.13 5.13c9.3 16.5 17.76 30.45 38.1 30.45 19.45 0 31.72-7.61 31.72-37.2v-201.3h59.2v202.1c0 61.3-35.94 89.2-88.4 89.2-47.4 0-74.85-24.53-88.81-54.075z"/>
</svg>

JavaScript has progressed significantly in recent times where many of the tooling & language enhancements
that we used to rely on external tools for is now available in modern browsers alleviating the need for
complex tooling and npm dependencies that have historically plagued modern web development.

The good news is that the complex npm tooling that was previously considered mandatory in modern JavaScript App
development can be considered optional as we can now utilize modern browser features like
[async/await](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Statements/async_function),
[JavaScript Modules](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Modules),
[dynamic imports](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/import),
[import maps](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/script/type/importmap)
and [modern language features](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide) for a
sophisticated development workflow without the need for any npm build tools.

### Bringing Simplicity Back

The [razor](https://github.com/NetCoreTemplates/razor) template focuses on simplicity and eschews many aspects that has
complicated modern JavaScript development,
specifically:

- No npm node_modules or build tools
- No client side routing
- No heavy client state

Effectively abandoning the traditional SPA approach in lieu of a simpler [MPA](https://docs.astro.build/en/concepts/mpa-vs-spa/)
development model using Razor Pages for Server Rendered content with any interactive UIs progressively enhanced with JavaScript.

#### Freedom to use any JS library

Avoiding the SPA route ends up affording more flexibility on which JS libraries each page can use as without heavy bundled JS
blobs of all JS used in the entire App, it's free to only load the required JS each page needs to best implement its
required functionality, which can be any JS library, preferably utilizing ESM builds that can be referenced from a
[JavaScript Module](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Modules), taking advantage of the module system
native to modern browsers able to efficiently download the declarative matrix of dependencies each script needs.

### Best libraries for progressive Multi Page Apps

It includes a collection of libraries we believe offers the best modern development experience in Progressive
MPA Web Apps, specifically:

#### [Tailwind CLI](https://tailwindcss.com/docs/installation)
Tailwind enables a responsive, utility-first CSS framework for creating maintainable CSS at scale without the need for any CSS
preprocessors like Sass, which is configured to run from an npx script to avoid needing any node_module dependencies.

#### [Vue 3](https://vuejs.org/guide/introduction.html)
Vue is a popular Progressive JavaScript Framework that makes it easy to create interactive Reactive Components whose
[Composition API](https://vuejs.org/api/composition-api-setup.html) offers a nice development model without requiring any
pre-processors like JSX.

Where creating a component is as simple as:

```js
const Hello = {
    template: `<b>Hello, {{name}}!</b>`,
    props: { name:String }
}
```
<div class="text-center text-2xl py-2">
    <hello name="Vue 3"></hello>
</div>

Or a simple reactive example:

```js
import { ref } from "vue"

const Counter = {
    template: `<b @click="count++">Counter {{count}}</b>`,
    setup() {
        let count = ref(1)
        return { count }
    }
}
```

<div class="text-center text-2xl py-2 cursor-pointer select-none">
    <counter></counter>
</div>

### Vue Components in Markdown

Inside `.md` Markdown pages Vue Components can be embedded using Vue's progressive
[HTML Template Syntax](https://vuejs.org/guide/essentials/template-syntax.html):

```html
<hello name="Vue 3"></hello>
<counter></counter>
```

### Vue Components in Razor Pages

Inside `.cshtml` Razor Pages these components can be mounted using the standard [Vue 3 mount](https://vuejs.org/api/application.html#app-mount) API, but to
make it easier we've added additional APIs for declaratively mounting components to pages using `data-component` and `data-props`
attributes:

```html
<div data-component="Hello" data-props="{ name: 'Vue 3' }"></div>
```

Alternatively they can be programatically added using the custom `mount` method in `api.mjs`:

```js
import { mount } from "/mjs/api.mjs"
mount('#counter', Counter)
```

Both methods create components with access to all your Shared Components and any 3rd Party Plugins which
we can preview in this example that uses **@servicestack/vue**'s
[PrimaryButton](https://docs.servicestack.net/vue/navigation#primarybutton)
and [ModalDialog](https://docs.servicestack.net/vue/modals):


```js
const Plugin = {
    template:`<div>
        <PrimaryButton @click="show=true">Open Modal</PrimaryButton>
        <ModalDialog v-if="show" @done="show=false">
            <div class="p-8">Hello @servicestack/vue!</div>
        </ModalDialog>
    </div>`,
    setup() {
        const show = ref(false)
        return { show }
    }
}
```

```html
<plugin></plugin>
```

<div class="text-center">
    <plugin id="plugin" class="text-2xl py-4"></plugin>
</div>

### Vue HTML Templates

An alternative progressive approach for creating Reactive UIs with Vue is by embedding its HTML markup directly in `.html` pages using
[HTML Template Syntax](https://vuejs.org/guide/essentials/template-syntax.html) which is both great for performance
as the DOM UI can be rendered before the Vue Component is initialized. UI elements you want hidden can use Vue's
[v-cloak](https://vuejs.org/api/built-in-directives.html#v-cloak) attribute where they'll be hidden until components are initialized.

It's also great for development as it lets you cohesively maintain most pages functionality need in the HTML page itself - in
isolation with the rest of the website, i.e. instead of spread across multiple external `.js` source files that for
SPAs unnecessarily increases the payload sizes of JS bundles with functionality that no other pages need.

With Vue's HTML syntax you can maintain the Vue template in HTML and just use embedded JavaScript for the Reactive UI's functionality, e.g:

```html
<div id="app">
    <primary-button v-on:click="show=true">Open Modal</primary-button>
    <modal-dialog v-if="show" v-on:done="show=false">
        <div class="p-8">Hello @servicestack/vue!</div>
    </modal-dialog>
</div>
<script>
const App = {
    setup() {
        const show = ref(false)
        return { show }
    }
}
mount('#app', App)
</script>
```

This is the approach used to develop [Vue Stable Diffusion](/posts/vue-stable-diffusion) where all functionality specific
to the page is maintained in the page itself, whilst any common functionality is maintained in external JS Modules loaded
on-demand by the Browser when needed.

### @servicestack/vue
[@servicestack/vue](https://github.com/ServiceStack/servicestack-vue) is our growing Vue 3 Tailwind component library with a number of rich Tailwind components useful
in .NET Web Apps, including Input Components with auto form validation binding which is used by all HTML forms in
the [razor](https://github.com/NetCoreTemplates/razor) template.

<vue-component-gallery></vue-component-gallery>

### @servicestack/client
[@servicestack/client](https://docs.servicestack.net/javascript-client) is our generic JS/TypeScript client library
which enables a terse, typed API for using your App's typed DTOs from the built-in
[JavaScript ES6 Classes](https://docs.servicestack.net/javascript-add-servicestack-reference) support to enable an effortless
end-to-end Typed development model for calling your APIs **without any build steps**, e.g:

```html
<input type="text" id="txtName">
<div id="result"></div>

<script type="module">
import { JsonApiClient, $1, on } from '@servicestack/client'
import { Hello } from '/types/mjs'

on('#txtName', {
    async keyup(el) {
        const client = JsonApiClient.create()
        const api = await client.api(new Hello({ name:el.target.value }))
        $1('#result').innerHTML = api.response.result
    }
})
</script>
```

For better IDE intelli-sense during development, save the annotated Typed DTOs to disk with:

:::sh
npm run dtos
:::

That can be referenced instead to unlock your IDE's static analysis type-checking and intelli-sense benefits during development:

```js
import { Hello } from '/js/dtos.mjs'
client.api(new Hello({ name }))
```

You'll typically use all these libraries in your **API-enabled** components as seen in the
[HelloApi.mjs](https://github.com/NetCoreTemplates/razor/blob/main/MyApp/wwwroot/mjs/components/HelloApi.mjs)
component on the home page which calls the [Hello](/ui/Hello) API on each key press:

```js
import { ref } from "vue"
import { useClient } from "@servicestack/vue"
import { Hello } from "../dtos.mjs"

export default {
    template:/*html*/`<div class="flex flex-wrap justify-center">
        <TextInput v-model="name" @keyup="update" />
        <div class="ml-3 mt-2 text-lg">{{ result }}</div>
    </div>`,
    props:['value'],
    setup(props) {
        let name = ref(props.value)
        let result = ref('')
        let client = useClient()

        async function update() {
            let api = await client.api(new Hello({ name }))
            if (api.succeeded) {
                result.value = api.response.result
            }
        }
        update()

        return { name, update, result }
    }
}
```

Which we can also mount below:

```html
<hello-api value="Vue 3"></hello-api>
```

<hello-api value="Vue 3" class="w-full font-semibold"></hello-api>

We'll also go through and explain other features used in this component:

#### `/*html*/`

Although not needed in [Rider](rider) (which can automatically infer HTML in strings), the `/*html*/` type hint can be used
to instruct tooling like the [es6-string-html](https://marketplace.visualstudio.com/items?itemName=Tobermory.es6-string-html)
VS Code extension to provide syntax highlighting and an enhanced authoring experience for HTML content in string literals.

### useClient

[useClient()](https://docs.servicestack.net/vue/use-client) provides managed APIs around the `JsonServiceClient`
instance registered in Vue App's with:

```js
let client = JsonApiClient.create()
app.provide('client', client)
```

Which maintains contextual information around your API calls like **loading** and **error** states, used by `@servicestack/vue` components to
enable its auto validation binding. Other functionality in this provider include:

```js
let { 
    api, apiVoid, apiForm, apiFormVoid, // Managed Typed ServiceClient APIs
    loading, error,                     // Maintains 'loading' and 'error' states
    setError, addFieldError,            // Add custom errors in client
    unRefs                              // Returns a dto with all Refs unwrapped
} = useClient()
```

Typically you would need to unwrap `ref` values when calling APIs, i.e:

```js
let client = JsonApiClient.create()
let api = await client.api(new Hello({ name:name.value }))
```

#### useClient - api

This is unnecessary in useClient `api*` methods which automatically unwraps ref values, allowing for the more pleasant API call:

```js
let api = await client.api(new Hello({ name }))
```

#### useClient - unRefs

But as DTOs are typed, passing reference values will report a type annotation warning in IDEs with type-checking enabled,
which can be resolved by explicitly unwrapping DTO ref values with `unRefs`:

```js
let api = await client.api(new Hello(unRefs({ name })))
```

#### useClient - setError

`setError` can be used to populate client-side validation errors which the
[SignUp.mjs](https://github.com/NetCoreTemplates/vue-mjs/blob/main/MyApp/wwwroot/Pages/SignUp.mjs)
component uses to report an invalid submissions when passwords don't match:

```js
const { api, setError } = useClient()
async function onSubmit() {
    if (password.value !== confirmPassword.value) {
        setError({ fieldName:'confirmPassword', message:'Passwords do not match' })
        return
    }
    //...
}
```

### Form Validation

All `@servicestack/vue` Input Components support contextual validation binding that's typically populated from API
[Error Response DTOs](https://docs.servicestack.net/error-handling) but can also be populated from client-side validation
as done above.

#### Explicit Error Handling

This populated `ResponseStatus` DTO can either be manually passed into each component's **status** property as done in [/TodoMvc](/TodoMvc):

```html
<template id="TodoMvc-template">
    <div class="mb-3">
        <text-input :status="store.error" id="text" label="" placeholder="What needs to be done?"
                    v-model="store.newTodo" v-on:keyup.enter.stop="store.addTodo()"></text-input>
    </div>
    <!-- ... -->
</template>
```

Where if you try adding an empty Todo the `CreateTodo` API will fail and populate its `store.error` reactive property with the
APIs Error Response DTO which the `<TextInput />` component checks to display any field validation errors adjacent to the HTML Input
with matching `id` fields:

```js
let store = {
    /** @type {Todo[]} */
    todos: [],
    newTodo:'',
    error:null,
    async addTodo() {
        this.todos.push(new Todo({ text:this.newTodo }))
        let api = await client.api(new CreateTodo({ text:this.newTodo }))
        if (api.succeeded)
            this.newTodo = ''
        else
            this.error = api.error
    },
    //...
}
```

#### Implicit Error Handling

More often you'll want to take advantage of the implicit validation support in `useClient()` which makes its state available to child
components, alleviating the need to explicitly pass it in each component as seen in razor's
[Contacts.mjs](https://github.com/NetCoreTemplates/razor/blob/net6/MyApp/wwwroot/Pages/Contacts.mjs) `Edit` component for its
Contacts page which doesn't do any manual error handling:

```js
const Edit = {
    template:/*html*/`<SlideOver @done="close" title="Edit Contact">
    <form @submit.prevent="submit">
      <input type="submit" class="hidden">
      <fieldset>
        <ErrorSummary except="title,name,color,filmGenres,age,agree" class="mb-4" />
        <div class="grid grid-cols-6 gap-6">
          <div class="col-span-6 sm:col-span-3">
            <SelectInput id="title" v-model="request.title" :options="enumOptions('Title')" />
          </div>
          <div class="col-span-6 sm:col-span-3">
            <TextInput id="name" v-model="request.name" required placeholder="Contact Name" />
          </div>
          <div class="col-span-6 sm:col-span-3">
            <SelectInput id="color" v-model="request.color" :options="colorOptions" />
          </div>
          <div class="col-span-6 sm:col-span-3">
            <SelectInput id="favoriteGenre" v-model="request.favoriteGenre" :options="enumOptions('FilmGenre')" />
          </div>
          <div class="col-span-6 sm:col-span-3">
            <TextInput type="number" id="age" v-model="request.age" />
          </div>
        </div>
      </fieldset>
    </form>
    <template #footer>
      <div class="flex justify-between space-x-3">
        <div><ConfirmDelete @delete="onDelete">Delete</ConfirmDelete></div>
        <div><PrimaryButton @click="submit">Update Contact</PrimaryButton></div>
      </div>
    </template>
  </SlideOver>`,
    props:['contact'],
    emits:['done'],
    setup(props, { emit }) {
        const client = useClient()
        const request = ref(new UpdateContact(props.contact))
        const colorOptions = propertyOptions(getProperty('UpdateContact','Color'))

        async function submit() {
            const api = await client.api(request.value)
            if (api.succeeded) close()
        }

        async function onDelete () {
            const api = await client.apiVoid(new DeleteContact({ id:props.id }))
            if (api.succeeded) close()
        }

        const close = () => emit('done')

        return { request, enumOptions, colorOptions, submit, onDelete, close }
    }
}
```

Effectively making form validation binding a transparent detail where all `@servicestack/vue`
Input Components are able to automatically apply contextual validation errors next to the fields they apply to:

![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/scripts/edit-contact-validation.png)

### AutoForm Components

We can elevate our productivity even further with
[Auto Form Components](https://docs.servicestack.net/vue/autoform) that can automatically generate an
instant API-enabled form with validation binding by just specifying the Request DTO you want to create the form of, e.g:

```html
<AutoCreateForm type="CreateBooking" formStyle="card" />
```

<div class="not-prose">
    <auto-create-form type="CreateBooking" form-style="card"></auto-create-form>
</div>

The AutoForm components are powered by your [App Metadata](https://docs.servicestack.net/vue/use-appmetadata) which allows creating
highly customized UIs from [declarative C# attributes](https://docs.servicestack.net/locode/declarative) whose customizations are
reused across all ServiceStack Auto UIs, including:

- [API Explorer](https://docs.servicestack.net/api-explorer)
- [Locode](https://docs.servicestack.net/locode/)
- [Blazor Tailwind Components](https://docs.servicestack.net/templates-blazor-components)

### Form Input Components

In addition to including Tailwind versions of the standard [HTML Form Inputs](https://docs.servicestack.net/vue/form-inputs) controls to create beautiful Tailwind Forms,
it also contains a variety of integrated high-level components:

- [FileInput](https://docs.servicestack.net/vue/fileinput)
- [TagInput](https://docs.servicestack.net/vue/taginput)
- [Autocomplete](https://docs.servicestack.net/vue/autocomplete)

### useAuth

Your Vue.js code can access Authenticated Users using [useAuth()](https://docs.servicestack.net/vue/use-auth)
which can also be populated without the overhead of an Ajax request by embedding the response of the built-in
[Authenticate API](/ui/Authenticate?tab=details) inside `_Layout.cshtml` with:

```html
<script type="module">
import { useAuth } from "@@servicestack/vue"
const { signIn } = useAuth()
signIn(@await Html.ApiAsJsonAsync(new Authenticate()))
</script>
```

Where it enables access to the below [useAuth()](https://docs.servicestack.net/vue/use-auth) utils for inspecting the
current authenticated user:

```js
const { 
    signIn,           // Sign In the currently Authenticated User
    signOut,          // Sign Out currently Authenticated User
    user,             // Access Authenticated User info in a reactive Ref<AuthenticateResponse>
    isAuthenticated,  // Check if the current user is Authenticated in a reactive Ref<boolean>
    hasRole,          // Check if the Authenticated User has a specific role
    hasPermission,    // Check if the Authenticated User has a specific permission
    isAdmin           // Check if the Authenticated User has the Admin role
} = useAuth()
```

This is used in [Bookings.mjs](https://github.com/NetCoreTemplates/razor/blob/main/MyApp/wwwroot/pages/Bookings.mjs)
to control whether the `<AutoEditForm>` component should enable its delete functionality:

```js
export default {
    template/*html*/:`
    <AutoEditForm type="UpdateBooking" :deleteType="canDelete ? 'DeleteBooking' : null" />
    `,
    setup(props) {
        const { hasRole } = useAuth()
        const canDelete = computed(() => hasRole('Manager'))
        return { canDelete }
    }
}
```

#### [JSDoc](https://jsdoc.app)

We get great value from using [TypeScript](https://www.typescriptlang.org) to maintain our libraries typed code bases, however it
does mandate using an external tool to convert it to valid JS before it can be run, something the new Razor Vue.js templates expressly avoids.

Instead it adds JSDoc type annotations to code where it adds value, which at the cost of slightly more verbose syntax enables much of the
same static analysis and intelli-sense benefits of TypeScript, but without needing any tools to convert it to valid JavaScript, e.g:

```js
/** @param {KeyboardEvent} e */
function validateSafeName(e) {
    if (e.key.match(/[\W]+/g)) {
        e.preventDefault()
        return false
    }
}
```

#### TypeScript Language Service

Whilst the code-base doesn't use TypeScript syntax in its code base directly, it still benefits from TypeScript's language services
in IDEs for the included libraries from the TypeScript definitions included in `/lib/typings`, downloaded in
[postinstall.js](https://github.com/NetCoreTemplates/razor/blob/main/MyApp/postinstall.js) after **npm install**.

### Import Maps

[Import Maps](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/script/type/importmap) is a useful browser feature that allows
specifying optimal names for modules, that can be used to map package names to the implementation it should use, e.g:

```csharp
@Html.StaticImportMap(new() {
    ["vue"]                  = "/lib/mjs/vue.mjs",
    ["@servicestack/client"] = "/lib/mjs/servicestack-client.mjs",
    ["@servicestack/vue"]    = "/lib/mjs/servicestack-vue.mjs",
})
```

Where they can be freely maintained in one place without needing to update any source code references.
This allows source code to be able to import from the package name instead of its physical location:

```js
import { ref } from "vue"
import { useClient } from "@servicestack/vue"
import { JsonApiClient, $1, on } from "@servicestack/client"
```

It's a great solution for specifying using local unminified debug builds during **Development**, and more optimal CDN hosted
production builds when running in **Production**, alleviating the need to rely on complex build tools to perform this code transformation for us:

```csharp
@Html.ImportMap(new()
{
    ["vue"]                  = ("/lib/mjs/vue.mjs",                 "https://unpkg.com/vue@3/dist/vue.esm-browser.prod.js"),
    ["@servicestack/client"] = ("/lib/mjs/servicestack-client.mjs", "https://unpkg.com/@servicestack/client@2/dist/servicestack-client.min.mjs"),
    ["@servicestack/vue"]    = ("/lib/mjs/servicestack-vue.mjs",    "https://unpkg.com/@servicestack/vue@3/dist/servicestack-vue.min.mjs")
})
```

Note: Specifying exact versions of each dependency improves initial load times by eliminating latency from redirects.

Or if you don't want your Web App to reference any external dependencies, have the ImportMap reference local minified production builds instead:

```csharp
@Html.ImportMap(new()
{
    ["vue"]                  = ("/lib/mjs/vue.mjs",                 "/lib/mjs/vue.min.mjs"),
    ["@servicestack/client"] = ("/lib/mjs/servicestack-client.mjs", "/lib/mjs/servicestack-client.min.mjs"),
    ["@servicestack/vue"]    = ("/lib/mjs/servicestack-vue.mjs",    "/lib/mjs/servicestack-vue.min.mjs")
})
```

#### Polyfill for Safari

Unfortunately Safari is the last modern browser to [support import maps](https://caniuse.com/import-maps) which is only now in
Technical Preview. Luckily this feature can be polyfilled with the [ES Module Shims](https://github.com/guybedford/es-module-shims):

```html
@if (Context.Request.Headers.UserAgent.Any(x => x.Contains("Safari") && !x.Contains("Chrome")))
{
    <script async src="https://ga.jspm.io/npm:es-module-shims@1.6.3/dist/es-module-shims.js"></script>
}
```

### Fast Component Loading

SPAs are notorious for being slow to load due to needing to download large blobs of JavaScript bundles that it needs to initialize
with their JS framework to mount their App component before it starts fetching the data from the server it needs to render its components.

A complex solution to this problem is to server render the initial HTML content then re-render it again on the client after the page loads.
A simpler solution is to avoid unnecessary ajax calls by embedding the JSON data the component needs in the page that loads it, which is what
[/TodoMvc](/TodoMvc) does to load its initial list of todos using the [Service Gateway](https://docs.servicestack.net/service-gateway)
to invoke APIs in process and embed its JSON response with:

```html
<script>todos = @await ApiResultsAsJsonAsync(new QueryTodos())</script>
<script type="module">
import TodoMvc from "/Pages/TodoMvc.mjs"
import { mount } from "/mjs/app.mjs"
mount('#todomvc', TodoMvc, { todos })
</script>
```

Where `ApiResultsAsJsonAsync` is a simplified helper that uses the `Gateway` to call your API and returns its unencoded JSON response:

```csharp
(await Gateway.ApiAsync(new QueryTodos())).Response?.Results.AsRawJson();
```

The result of which should render the List of Todos instantly when the page loads since it doesn't need to perform any additional Ajax requests
after the component is loaded.

### Fast Page Loading

We can get SPA-like page loading performance using htmx's [Boosting](https://htmx.org/docs/#boosting) feature which avoids full page reloads
by converting all anchor tags to use Ajax to load page content into the page body, improving perceived performance from needing to reload
scripts and CSS in `<head>`.

This is used in [Header.cshtml](https://github.com/NetCoreTemplates/razor/blob/main/MyApp/Pages/Shared/Header.cshtml) to **boost** all
main navigation links:

```html
<nav hx-boost="true">
    <ul>
        <li><a href="/Blog">Blog</a></li>
    </ul>
</nav>
```

htmx has lots of useful [real world examples](https://htmx.org/examples/) that can be activated with declarative attributes,
another useful feature is the [class-tools](https://htmx.org/extensions/class-tools/) extension to hide elements from
appearing until after the page is loaded:

```html
<div id="signin"></div>
<div class="hidden mt-5 flex justify-center" classes="remove hidden:load">
    @Html.SrcPage("SignIn.mjs")
</div>
```

Which is used to reduce UI yankiness from showing server rendered content before JS components have loaded.

### @servicestack/vue Library

[@servicestack/vue](https://docs.servicestack.net/vue/) is our cornerstone library for enabling a highly productive
Vue.js development model across our [Vue Tailwind Project templates](https://docs.servicestack.net/templates-vue) which
we'll continue to significantly invest in to unlock even greater productivity benefits in all Vue Tailwind Apps.

In addition to a variety of high-productive components, it also contains a core library of functionality
underpinning the Vue Components that most Web Apps should also find useful:

<vue-component-library class="mt-4"></vue-component-library>
