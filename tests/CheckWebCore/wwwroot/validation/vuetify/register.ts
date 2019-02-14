import { Vue } from 'vue';
import { errorResponse, errorResponseExcept, splitOnFirst, toPascalCase } from '@servicestack/client';
import { client, nameRules, emailRules, passwordRules, confirmPasswordRules } from './shared';
import { Register } from '../../dtos';

declare var CONTINUE:any;

new Vue({
    el: '#app',
    computed: {
        errorSummary: function() {
            return errorResponseExcept.call(this, 'displayName,email,password,confirmPassword');
        },
    },
    methods: {
        async submit() {
            const form = (this.$refs.form as HTMLFormElement);
            if (form.validate()) {
                try {
                    this.loading = true;

                    const response = await client.post(new Register({
                        displayName: this.displayName,
                        email: this.email,
                        password: this.password,
                        confirmPassword: this.confirmPassword,
                        autoLogin: this.autoLogin,
                    }));

                    location.href = '/validation/vuetify/';
                } catch (e) {
                    this.responseStatus = e.responseStatus || e;
                } finally {
                    this.loading = false;
                    form.resetValidation();
                }
            }
        },
        switchUser(email:string) {
            const names = email.split('@');
            this.displayName = toPascalCase(names[0]) + ' ' + toPascalCase(splitOnFirst(names[1], '.')[0]);
            this.email = email;
            this.password = this.confirmPassword = 'p@55wOrd';
        },
        errorResponse
    },
    data: () => ({
        loading: false,
        valid: true,
        displayName: "",
        email: "",
        password: "",
        confirmPassword: "",
        autoLogin: true,
        nameRules, emailRules, passwordRules, confirmPasswordRules,
        responseStatus: null
    }),
});
