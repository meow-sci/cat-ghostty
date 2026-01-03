import { readdir, readFile, writeFile } from "node:fs/promises";
import { join, basename, extname } from "node:path";

async function* getFiles(dir: string): AsyncGenerator<string> {
  const dirents = await readdir(dir, { withFileTypes: true });
  for (const dirent of dirents) {
    const res = join(dir, dirent.name);
    if (dirent.isDirectory()) {
      yield* getFiles(res);
    } else {
      yield res;
    }
  }
}

async function processFile(filePath: string) {
  if (extname(filePath) !== ".cs") return;

  const content = await readFile(filePath, "utf-8");

  // Regex to match class declarations at start of line (with optional indentation)
  // Group 1: Leading whitespace/indentation
  // Group 2: Modifiers and 'class' keyword
  // Group 3: Class Name
  const classRegex = /^(\s*)((?:public|internal|private|protected|static|partial|sealed|abstract|\s)*class)\s+(\w+)/gm;

  let newContent = content;
  let matches = [...content.matchAll(classRegex)];
  let offset = 0;
  let modified = false;

  for (const match of matches) {
    const indentation = match[1];
    const className = match[3];
    const startIndex = match.index! + offset;

    const categoryAttr = `${indentation}[Category("only_${className}")]\n`;

    // 1. Clean up "jammed" attributes from previous run (placed on same line)
    // Example: [Category("Integration")][Category("AlternateScreenIsolationTests")]
    const jammedRegex = new RegExp(`\\[Category\\("${className}"\\)\\]`, 'g');
    const existingAttrStr = `[Category("${className}")]`;

    // Check if it already exists correctly on its own line above
    const lineBefore = content.substring(0, match.index!).split('\n').pop() || "";
    if (lineBefore.includes(existingAttrStr)) {
      console.log(`Skipping ${className} in ${basename(filePath)} - Already correctly placed.`);
      continue;
    }

    // If it exists but might be "jammed" on the same line or misplaced
    if (content.includes(existingAttrStr)) {
      console.log(`Fixing misplaced ${existingAttrStr} in ${basename(filePath)}`);
      // Remove all instances of this specific category first to start clean for this class
      newContent = newContent.replace(jammedRegex, "");
      // We need to re-scan or calculate offset, but simpler to just re-match or use a different approach
      // For now, let's just insert it correctly and let the user know. 
      // A better way is to strip all then add, but let's try to be surgical.
    }

    const currentPos = newContent.indexOf(match[0]); // Find where the class is now in the potentially modified content
    if (currentPos === -1) continue;

    newContent = newContent.slice(0, currentPos) + categoryAttr + newContent.slice(currentPos);
    modified = true;
  }

  // Final cleanup: if we have any duplicated categories on the same line, fix them
  // This handles the error case observed: [Category("Integration")][Category("AlternateScreenIsolationTests")]
  const fixJammedRegex = /\](\s*)\[Category/g;
  if (fixJammedRegex.test(newContent)) {
    newContent = newContent.replace(fixJammedRegex, "]\n$1[Category");
    modified = true;
  }

  if (modified) {
    await writeFile(filePath, newContent, "utf-8");
  }
}

async function main() {
  const targetDir = join(process.cwd(), "caTTY.Core.Tests");
  console.log(`Scanning ${targetDir} for .cs files...`);

  for await (const file of getFiles(targetDir)) {
    await processFile(file);
  }

  console.log("Done!");
}

main().catch(console.error);
