import { BackendServer } from "./BackendServer.js";

const server = new BackendServer({
    port: 4444,
    shell: "bash",
});

server.start();
