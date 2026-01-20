import { connect } from "bun";
import type { RpcRequest, RpcResponse, ClientOptions } from "./types";

/**
 * RPC client for communicating with Kitten Space Agency game via Unix domain sockets
 */
export class KsaRpcClient {
  private socketPath: string;
  private timeout: number;

  /**
   * Create a new KSA RPC client
   * @param socketPath - Path to Unix domain socket (defaults to KSA_RPC_SOCKET env var)
   * @param options - Client configuration options
   */
  constructor(socketPath?: string, options?: ClientOptions) {
    this.socketPath = socketPath ?? process.env.KSA_RPC_SOCKET ?? "";
    this.timeout = options?.timeout ?? 5000;

    if (!this.socketPath) {
      throw new Error(
        "Socket path not provided. Set KSA_RPC_SOCKET environment variable or pass socketPath to constructor."
      );
    }
  }

  /**
   * Call an RPC action on the game server
   * @param action - Action name to invoke
   * @param params - Optional parameters for the action
   * @returns Promise resolving to the response data
   * @throws Error if the request fails or times out
   */
  async call<T = unknown>(
    action: string,
    params?: Record<string, unknown>
  ): Promise<T> {
    const request: RpcRequest = { action, params };
    const requestJson = JSON.stringify(request) + "\n";

    return new Promise<T>(async (resolve, reject) => {
      const timeoutId = setTimeout(() => {
        reject(new Error(`Request timed out after ${this.timeout}ms`));
      }, this.timeout);

      try {
        // Connect to Unix domain socket
        const socket = await connect({
          unix: this.socketPath,
          socket: {
            data(socket, data) {
              clearTimeout(timeoutId);

              // Parse response
              const responseText = Buffer.from(data).toString("utf-8").trim();
              try {
                const response: RpcResponse<T> = JSON.parse(responseText);

                if (response.success) {
                  resolve(response.data as T);
                } else {
                  reject(new Error(response.error ?? "Unknown error"));
                }
              } catch (parseError) {
                reject(
                  new Error(
                    `Failed to parse response: ${parseError instanceof Error ? parseError.message : String(parseError)}`
                  )
                );
              }

              // Close socket after receiving response
              socket.end();
            },
            error(socket, error) {
              clearTimeout(timeoutId);
              reject(error);
            },
            close(socket) {
              clearTimeout(timeoutId);
            },
          },
        });

        // Send request
        socket.write(requestJson);
      } catch (error) {
        clearTimeout(timeoutId);
        reject(
          new Error(
            `Failed to connect to socket: ${error instanceof Error ? error.message : String(error)}`
          )
        );
      }
    });
  }

  /**
   * Get the configured socket path
   */
  getSocketPath(): string {
    return this.socketPath;
  }
}
