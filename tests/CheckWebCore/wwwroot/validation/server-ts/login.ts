import {bindHandlers, bootstrap} from "@servicestack/client";

bootstrap(); //converts data-invalid attributes into Bootstrap v4 error messages.

bindHandlers({
    switchUser: (u: string) => {
        (document.querySelector("[name=userName]") as HTMLInputElement).value = u;
        (document.querySelector("[name=password]") as HTMLInputElement).value = 'p@55wOrd';
    }
});
