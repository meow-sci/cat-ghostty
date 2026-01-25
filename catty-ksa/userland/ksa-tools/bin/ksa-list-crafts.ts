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
      const craft = await client.call<CraftInfo | null>("get-current-craft");

      if (jsonOutput) {
        console.log(JSON.stringify(craft));
      } else {
        if (craft) {
          console.log(craft.name);
        }
        // Empty output if no current craft
      }
    } else {
      // List all crafts
      const crafts = await client.call<CraftInfo[]>("list-crafts");

      if (jsonOutput) {
        console.log(JSON.stringify(crafts, null, 2));
      } else {
        // One craft name per line for pipeline compatibility
        for (const craft of crafts) {
          console.log(craft.name);
        }
      }
    }
  } catch (error) {
    if (error instanceof Error) {
      console.error(`Error: ${error.message}`, error);
    } else {
      console.error(`Error: ${error}`);
    }
    process.exit(1);
  }
}

main();
