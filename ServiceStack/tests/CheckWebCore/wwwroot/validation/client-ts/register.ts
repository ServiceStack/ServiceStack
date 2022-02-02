import {bindHandlers, bootstrapForm, splitOnFirst, toPascalCase} from "@servicestack/client";
import {AuthenticateResponse} from "../../dtos";

bootstrapForm(document.querySelector('form'), {
    success: (r:AuthenticateResponse) => {
        location.href = '/validation/client-ts/';
    }
});

bindHandlers({
    newUser: (u: string) => {
        const $ = (sel:string) => document.querySelector(sel) as HTMLInputElement;

        const names = u.split('@');
        $("[name=displayName]").value = toPascalCase(names[0]) + " " + toPascalCase(splitOnFirst(names[1],'.')[0]); 
        $("[name=email]").value = u;
        $("[name=password]").value = $("[name=confirmPassword]").value = 'p@55wOrd';
    }
});
