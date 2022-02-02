import { JsonServiceClient } from "@servicestack/client";

export const client = new JsonServiceClient();

export const nameRules = [
    (v:string) => !!v || 'Name is required',
];

export const emailRules = [
    (v:string) => !!v || 'E-mail is required',
    (v:string) => /^\w+([\.-]?\w+)*@\w+([\.-]?\w+)*(\.\w{2,3})+$/.test(v) || 'E-mail must be valid'
];

export const passwordRules = [
    (v:string) => !!v || 'Password is required',
    (v:string) => v.length > 6 || 'Password must be grater than 6 characters'
];

export const confirmPasswordRules = [
    (v:string) => !!v || 'Password is required'
];
