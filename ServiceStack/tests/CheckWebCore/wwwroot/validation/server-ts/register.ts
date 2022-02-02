import { bindHandlers, bootstrap, splitOnFirst, toPascalCase } from "@servicestack/client";

bootstrap(); //converts data-invalid attributes into Bootstrap v4 error messages.

bindHandlers({
    newUser: (u: string) => {
        const $ = (sel:string) => document.querySelector(sel) as HTMLInputElement;

        const names = u.split('@');
        $("[name=displayName]").value = toPascalCase(names[0]) + " " + toPascalCase(splitOnFirst(names[1],'.')[0]);
        $("[name=email]").value = u;
        $("[name=password]").value = $("[name=confirmPassword]").value = 'p@55wOrd';
    }
});
