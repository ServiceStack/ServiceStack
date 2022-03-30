import { App, Breakpoints, ExplorerRoutes, ExplorerStore, Routes, MetadataOperationType } from "../dist/shared";

/** Method arguments of custom Doc Components */
export interface DocComponentArgs {
    store: ExplorerStore;
    routes: ExplorerRoutes & Routes;
    breakpoints: Breakpoints;
    op: () => MetadataOperationType;
}

/** Method Signature of custom Doc Components */
export declare type DocComponent = (args:DocComponentArgs) => Record<string,any>;

/** API Explorer App instance */
export let App:App;