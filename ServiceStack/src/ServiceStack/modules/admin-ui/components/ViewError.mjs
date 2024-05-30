const ViewError = {
    template:`
      <div>
        <h3 class="text-2xl text-red-700 mb-3">{{error.errorCode}}</h3>
        <h3 class="text-red-700 mb-3">{{error.message}}</h3>
        <pre v-if="error.stackTrace" class="mb-4">{{error.stackTrace}}</pre>
        <HtmlFormat v-if="error.errors?.length" :value="error.errors" class="mb-4" />
        <HtmlFormat v-if="error.meta" :value="error.meta" />
      </div>
    `,
    props: {
        error: Object
    },
    setup(props) {
    }
}
