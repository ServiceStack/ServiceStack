import { Vue } from 'vue';
import { errorResponse, errorResponseExcept, splitOnFirst, toPascalCase } from '@servicestack/client';
import { client, nameRules, } from '../shared';
import {CreateContact, DeleteContact, GetContact, GetContacts, Title, UpdateContact} from '../../../dtos';

declare var DATA:any;

new Vue({
    el: '#app',
    computed: {
        heading: function() {
            return this.update ? 'Edit new Contact' : 'Add new Contact';
        },
        action: function() {
            return this.update? 'Update Contact' : 'Add Contact';
        },
        errorSummary: function() {
            return errorResponseExcept.call(this, 'title,name,color,filmGenres,age,agree');
        },
    },
    methods: {
        async submit() {
            const form = (this.$refs.form as HTMLFormElement); 
            if (form.validate()) {
                try {
                    this.loading = true;
                    
                    const request = {
                        title: this.title as Title,
                        name: this.name,
                        color: this.color,
                        filmGenres: this.filmGenres,
                        age: this.age,
                    };
                    
                    if (this.update) {
                        await client.post(new UpdateContact({...request, id: this.id }));
                    } else {
                        await client.post(new CreateContact({...request, agree: this.agree }));
                    }

                    this.update = false;
                    form.reset();
                    
                } catch (e) {
                    this.responseStatus = e.responseStatus || e;
                } finally {
                    this.loading = false;
                    form.resetValidation();
                }
                await this.refresh();
            }
        },
        async refresh() {
            this.contacts = (await client.get(new GetContacts())).results;
        },
        reset() {
            (this.$refs.form as HTMLFormElement).reset();
        },
        cancel() {
            this.reset();
            this.update = false;
        },
        async edit(id:number) {
          this.update = true;
          const contact = (await client.get(new GetContact({ id }))).result;
          console.log(contact);
          Object.assign(this, contact);
        },
        async remove(id:number) {
            if (!confirm('Are you sure?'))
                return;

            await client.delete(new DeleteContact({ id }));
            const response = await client.get(new GetContacts());
            await this.refresh();
        },
        errorResponse
    },
    data: () => ({
        loading: false,
        valid: true,
        update: false, 
        ...DATA,

        id:0,
        title: "",
        name: "",
        color: "",
        filmGenres: [],
        age: 13,
        agree: false,
        nameRules,
        responseStatus: null
    }),
});
