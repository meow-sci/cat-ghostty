import { BackendServer } from "./BackendServer.js";

const server = new BackendServer({
    port: 4321,
    shell: "bash",
});

server.start();
