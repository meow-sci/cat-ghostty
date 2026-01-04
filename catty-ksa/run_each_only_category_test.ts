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
  const logDir = join(process.cwd(), "testlogs");

  await mkdir(logDir, { recursive: true });

  console.log(`Discovering tests with "only_" category in ${testProjectDir}...`);

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
  console.log(`Found ${total} classes to run.`);

  return;

  if (total === 0) {
    console.log("No tests with 'only_' category found. Use add_test_categories.ts first or manually add the prefix.");
    return;
  }

  let completed = 0;

  for (const { category } of filesToRun) {
    const logFileStdout = join(logDir, `${category}_stdout.log`);
    const logFileStderr = join(logDir, `${category}_stderr.log`);
    console.log(`[${completed + 1}/${total}] Running tests for category: ${category} into ${logFileStdout}`);

    try {
      // Run dotnet test and redirect output to log file
      // We use Bun shell to capture everything easily
      const { stdout, stderr, exitCode } = await $`dotnet test --filter "Category=${category}"`.cwd(testProjectDir).quiet().nothrow();
      await Bun.write(logFileStdout, stdout);
      await Bun.write(logFileStderr, stderr);
      console.log(`   Done. Results saved to ${basename(logFileStdout)}`);
    } catch (err) {
      console.error(`   Error running tests for ${category}. Check ${basename(logFile)} for details.`);
    }

    completed++;
    console.log(`${completed}/${total} classes complete.`);
  }

  console.log("\nAll requested test runs finished.");
}

main().catch(console.error);
