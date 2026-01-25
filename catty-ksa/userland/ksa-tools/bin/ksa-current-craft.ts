#!/usr/bin/env bun
/**
 * ksa-current-craft - Get the currently controlled craft in KSA
 * 
 * Usage:
 *   ksa-current-craft        # Print craft name (or empty if none)
 *   ksa-current-craft --json # Output as JSON object
 * 
 * Output is designed to be pipeline-compatible:
 *   craft=$(ksa-current-craft)
 *   ksa-current-craft | xargs -I{} echo "Controlling: {}"
 */

import { KsaRpcClient, type CraftInfo } from "@ksa/rpc-client";

async function main() {
  const args = process.argv.slice(2);
  const jsonOutput = args.includes("--json");

  try {
    const client = new KsaRpcClient();
    const craft = await client.call<CraftInfo | null>("get-current-craft");

    if (jsonOutput) {
      // Output full JSON object or null
      console.log(JSON.stringify(craft));
    } else {
      // Output craft name if exists, empty otherwise
      if (craft) {
        console.log(craft.name);
      }
    }
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
