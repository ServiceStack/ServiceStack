import { bindHandlers, bootstrapForm } from "@servicestack/client";
import {AuthenticateResponse} from "../../dtos";

declare var CONTINUE:string;

bootstrapForm(document.querySelector('form'), {
    success: (r: AuthenticateResponse) => {
        location.href = CONTINUE;
    }
});

bindHandlers({
    switchUser: (u: string) => {
        (document.querySelector("[name=userName]") as HTMLInputElement).value = u;
        (document.querySelector("[name=password]") as HTMLInputElement).value = 'p@55wOrd';
    }
});
