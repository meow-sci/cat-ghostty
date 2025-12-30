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

let limit: number | null = null;

if (typeof values.limit === "string") {
  const parsed = parseInt(values.limit);
  if (isNaN(parsed)) {
    throw new Error(`--limit value '${values.limit}' is not a number`);
  }
  limit = parsed
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

let sql: string = "select * from trace order by id asc";

if (typeof limit === "number") {
sql = `
SELECT * from 
(
  SELECT * FROM trace WHERE type != 'utf8' ORDER BY id DESC LIMIT ${limit}
) AS t
ORDER BY id ASC
`;
}

const query = db.query(sql);
const result = query.all() as Row[];

for (const row of result) {

  const timestamp = new Date(row.time).toISOString();

  const rowCol = `(${row.row.toString().padStart(3, " ")},${row.col.toString().padStart(3, " ")})`;

  console.log(`${timestamp} ${rowCol} ${row.type.padEnd(9, " ")} ${row.printable ?? row.escape_seq}`);
}