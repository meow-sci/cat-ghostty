import { readdir, readFile, mkdir } from "node:fs/promises";
import { join, basename, extname } from "node:path";
import { $ } from "bun";

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

async function main() {
  const testProjectDir = join(process.cwd(), "caTTY.Core.Tests");



  // console.log(`Discovering tests with "only_" category in ${testProjectDir}...`);

  const filesToRun: { filePath: string; category: string }[] = [];

  for await (const file of getFiles(testProjectDir)) {
    if (extname(file) !== ".cs") continue;

    const content = await readFile(file, "utf-8");
    // Look for [Category("only_...")]
    const categoryMatch = content.match(/\[Category\("(only_[^"]+)"\)\]/);

    if (categoryMatch) {
      filesToRun.push({
        filePath: file,
        category: categoryMatch[1]
      });
    }
  }

  const total = filesToRun.length;
  // console.log(`Found ${total} classes to run.`);


  if (total === 0) {
    // console.log("No tests with 'only_' category found. Use add_test_categories.ts first or manually add the prefix.");
    return;
  }

  let completed = 0;

  for (const { category } of filesToRun) {
    console.log(`dotnet test --filter "Category=${category}"`);

  }

}

main().catch(console.error);
