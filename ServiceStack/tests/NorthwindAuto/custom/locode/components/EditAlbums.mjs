import { useClient, useMetadata } from "@servicestack/vue"
import { ref } from "vue"
import { UpdateAlbums } from "/types/mjs"

export const EditAlbums = {
    template:/*html*/`
      <ModalDialog @done="done" sizeClass="">
        <div class="album-form relative flex flex-col">
          <ErrorSummary except="title,artistId" />
          <form @submit.prevent="submit" class="m-4 shadow-md rounded-full w-96 h-96 max-w-96 flex justify-center items-center">
            <div class="flex flex-col justify-center items-center text-center">
              <h1 class="text-3xl font-medium text-rose-500 mb-4">Edit Album {{ request.albumId }}</h1>
              <fieldset>
                <input type="hidden" name="albumId" :value="request.albumId">
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
    props: ['model','type','deleteType'],
    emits: ['done','save'],
    setup(props, { emit }) {
        const client = useClient()
        const { typeOf } = useMetadata()

        const dataModelType = typeOf("Albums")
        const lookupProp = dataModelType.properties.find(x => x.name === 'ArtistId')
        const request = ref(new UpdateAlbums(props.model))

        /** @param {Event} e */
        async function submit(e) {
            const form = e.target
            const api = await client.apiForm(new UpdateAlbums(), new FormData(form))
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