import { bootstrapForm, bindHandlers, JsonServiceClient } from "@servicestack/client";
import {Contact, DeleteContact, GetContact, GetContacts} from "../../../dtos";

declare var CONTACTS:Contact[];

const client = new JsonServiceClient();

const form = document.querySelector("form")!;
bootstrapForm(form,{
    success: function (r:{result:Contact}) {
        form.reset();
        CONTACTS.push(r.result);
        render();
    }
});
bindHandlers({
    deleteContact: async function(id:number) {
        if (!confirm('Are you sure?'))
            return;

        await client.delete(new DeleteContact({ id }));
        const response = await client.get(new GetContacts());
        CONTACTS = response.results;
        render();
    }
});

const contactRow = (contact:Contact) =>
    `<tr style="background:${contact.color}">
        <td>${contact.title} ${contact.name} (${contact.age})</td>
        <td><a href="/validation/client-ts/contacts/${contact.id}/edit">edit</a></td>
        <td><button class="btn btn-sm btn-primary" data-click="deleteContact:${contact.id}">delete</button></td>
    </tr>`;

function render() {
    let sb = "";
    if (CONTACTS.length > 0) {
        for (let i=0; i<CONTACTS.length; i++) {
            sb += contactRow(CONTACTS[i])
        }
    } else {
        sb = "<tr><td>There are no contacts.</td></tr>";
    }
    document.querySelector("#results")!.innerHTML = `<tbody>${sb}</tbody>`;
}

render();
