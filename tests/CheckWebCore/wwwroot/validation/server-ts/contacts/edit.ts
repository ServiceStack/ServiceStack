import { bootstrapForm } from "@servicestack/client";

const form = document.querySelector("form");
bootstrapForm(form,{
    success: function () {
        location.href = '/validation/server-ts/contacts/';
    }
});
