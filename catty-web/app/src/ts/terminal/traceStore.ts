import { atom } from "nanostores";

import { traceSettings } from "@catty/terminal-emulation";

export const traceEnabledStore = atom<boolean>(traceSettings.enabled);

traceEnabledStore.subscribe((enabled) => {
  // Mutate the existing singleton object so all imports observe the change.
  traceSettings.enabled = enabled;
});
