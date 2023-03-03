export const RegisterDocs = {
    template:/*html*/`
      <div class="max-w-screen-md mx-auto text-center">
          <h2 class="text-2xl font-medium mb-3">Register API</h2>
          <p class="text-gray-500">
            Public API users can use to create a new User Account, can be added to your AppHost with:
          </p>
          <pre class="my-3"><code v-highlightjs="'Plugins.Add(new RegistrationFeature());'"></code></pre>
      </div>    
    `,
    setup() {
        const highlight = globalThis.hljs.highlight
        return { highlight }
    }
}
