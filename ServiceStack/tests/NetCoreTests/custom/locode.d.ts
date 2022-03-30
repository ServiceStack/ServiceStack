import { App, CrudApisState, LocodeRoutes, LocodeRoutesExtend, LocodeSettings, LocodeStore, Routes } from "./types";

/** Method arguments of custom Create Form Components */
export interface CreateComponentArgs {
    store: LocodeStore;
    routes: LocodeRoutes & LocodeRoutesExtend & Routes;
    settings: LocodeSettings;
    state: () => CrudApisState;
    save: () => void;
    done: () => void;
}
/** Method Signature of custom Create Form Components */
export declare type CreateComponent = (args:CreateComponentArgs) => Record<string,any>;

/** Method arguments of custom Edit Form Components */
export interface EditComponentArgs {
    store: LocodeStore;
    routes: LocodeRoutes & LocodeRoutesExtend & Routes;
    settings: LocodeSettings;
    state: () => CrudApisState;
    save: () => void;
    done: () => void;
}
/** Method signature of custom Edit Form Components */
export declare type EditComponent = (args:EditComponentArgs) => Record<string,any>;

/** API Explorer App instance */
export let App:App;
