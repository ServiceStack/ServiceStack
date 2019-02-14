import { Vue } from 'vue';
import { errorResponse, errorResponseExcept } from '@servicestack/client';
import { client, emailRules, passwordRules } from './shared';
import { Authenticate } from "../../dtos";

declare var CONTINUE:any;

new Vue({
    el: '#app',
    computed: {
        errorSummary: function() {
            return errorResponseExcept.call(this, 'userName,password');
        }, 
    },
    methods: {
        async submit() {
            const form = (this.$refs.form as HTMLFormElement);
            if (form.validate()) {
                try {
                    this.loading = true;

                    const response = await client.post(new Authenticate({
                        provider: 'credentials',
                        userName: this.userName,
                        password: this.password,
                        rememberMe: this.rememberMe,
                    }));

                    location.href = CONTINUE;
                } catch (e) {
                    this.responseStatus = e.responseStatus || e;
                } finally {
                    this.loading = false;
                    form.resetValidation();
                }
            }
        },
        switchUser(email:string) {
            this.userName = email;
            this.password = 'p@55wOrd';
        },
        errorResponse
    },
    data: () => ({
        loading: false,
        valid: true,
        userName: "",
        password: "",
        rememberMe: true,
        emailRules, passwordRules,
        responseStatus: null
    }),
});

