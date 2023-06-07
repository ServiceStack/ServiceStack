import { ref } from "vue"
import { useClient, useMetadata } from "@servicestack/vue"
import { CreateAlbums } from "/types/mjs"

export const NewAlbums = {
    template:/*html*/`
    <ModalDialog @done="done" sizeClass="">
      <div class="album-form relative flex flex-col">
        <ErrorSummary except="title,artistId"/>
        <form @submit.prevent="submit" class="m-4 shadow-md rounded-full w-96 h-96 flex justify-center items-center">
          <div class="flex flex-col justify-center items-center text-center">
            <h1 class="text-3xl font-medium text-rose-500 mb-4">New Album</h1>
            <fieldset>
              <TextInput id="title" v-model="request.title" label="" placeholder="Album Title" class="mb-3" />

              <LookupInput id="artistId" v-model="request" label="" placeholder="Select Artist"
                           :input="lookupProp.input" :metadataType="dataModelType" class="mb-3" />

              <SubmitAlbumButton />
            </fieldset>
          </div>
        </form>
      </div>
    </ModalDialog>
    `,
    props: ['type'],
    emits: ['done','save'],
    setup(props, { emit }) {
        const client = useClient()
        const { typeOf } = useMetadata()
        
        const dataModelType = typeOf("Albums")
        const lookupProp = dataModelType.properties.find(x => x.name === 'ArtistId')
        const request = ref(new CreateAlbums())

        /** @param {Event} e */
        async function submit(e) {
            const form = e.target
            const api = await client.apiForm(new CreateAlbums(), new FormData(form))
            if (api.succeeded) {
                emit('save', api.response)
            }
        }
        
        function done() { 
            emit('done')
        }
        
        return { request, lookupProp, dataModelType, submit, done }
    }
}
