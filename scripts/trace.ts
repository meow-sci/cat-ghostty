import { Database } from "bun:sqlite";
import { parseArgs } from "util";

const { values, positionals } = parseArgs({
  args: Bun.argv,
  options: {
    db: {
      type: "string",
    },
    limit: {
      type: "string",
    },
  },
  strict: true,
  allowPositionals: true,
});


if (!values.db) {
  throw new Error(`--db not set`);
}

const dbFile = Bun.file(values.db);
console.log(`values.db=${values.db} exists=${await dbFile.exists()}`)

if (!(await dbFile.exists())) {
  throw new Error(`--db file at '${dbFile.name}' not found`);
}

const db = new Database(dbFile.name);

let limitQueryPart = "";

if (typeof values.limit === "string") {
  const limit = parseInt(values.limit);
  if (isNaN(limit)) {
    throw new Error(`--limit value '${values.limit}' is not a number`);
  }
  limitQueryPart = ` limit ${limit}`;
}


interface Row {
  id: number;
  time: number;
  type: "CSI" | "ESC" | "OSC" | "SGR" | "control" | "printable" | "utf8";
  escape_seq?: string;
  printable?: string;
  direction: string;
  row: number;
  col: number;

}

const query = db.query(`select * from trace${limitQueryPart};`);
const result = query.all() as Row[];

for (const row of result) {

  const rowCol = `(${row.row.toString().padStart(3, " ")},${row.col.toString().padStart(3, " ")})`;

  console.log(`${rowCol} ${row.type.padEnd(9, " ")} ${row.printable ?? row.escape_seq}`);
}