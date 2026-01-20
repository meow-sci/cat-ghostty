#!/usr/bin/env bun
/**
 * ksa-list-crafts - List all crafts in the KSA game
 * 
 * Usage:
 *   ksa-list-crafts              # List all craft names
 *   ksa-list-crafts --json       # Output as JSON array
 *   ksa-list-crafts --current    # Show only current craft name
 * 
 * Output is designed to be pipeline-compatible:
 *   ksa-list-crafts | grep rocket | head -n1
 */

import { KsaRpcClient, type CraftInfo } from "@ksa/rpc-client";

async function main() {
  const args = process.argv.slice(2);
  const jsonOutput = args.includes("--json");
  const currentOnly = args.includes("--current");

  try {
    const client = new KsaRpcClient();

    if (currentOnly) {
      // Get only the current craft
      const response = await client.request<CraftInfo | null>("get-current-craft");
      
      if (!response.success) {
        console.error(`Error: ${response.error}`);
        process.exit(1);
      }

      if (jsonOutput) {
        console.log(JSON.stringify(response.data));
      } else {
        if (response.data) {
          console.log(response.data.name);
        }
        // Empty output if no current craft
      }
    } else {
      // List all crafts
      const response = await client.request<CraftInfo[]>("list-crafts");
      
      if (!response.success) {
        console.error(`Error: ${response.error}`);
        process.exit(1);
      }

      const crafts = response.data || [];

      if (jsonOutput) {
        console.log(JSON.stringify(crafts, null, 2));
      } else {
        // One craft name per line for pipeline compatibility
        for (const craft of crafts) {
          console.log(craft.name);
        }
      }
    }

    client.close();
  } catch (error) {
    if (error instanceof Error) {
      console.error(`Error: ${error.message}`);
    } else {
      console.error(`Error: ${error}`);
    }
    process.exit(1);
  }
}

main();
