import { BackendServer } from "./BackendServer.js";

const server = new BackendServer({
    port: 4444,
    // shell: "zsh", // Let the server auto-detect the appropriate shell for the platform
    // shell: "bash",
});

server.start();
