let yOffset = 0;
let xOffset = 0;

function addLast() {
  const el = document.getElementById("display")!;

  const span = document.createElement("span");
  span.innerText = "Z";

  span.style.position = "absolute";
  span.style.left = `${xOffset++}ch`;
  span.style.top = `${yOffset++}ch`;

  el.appendChild(span);

}

export function TerminalPage() {



  return (
    <main id="root">
      <section id="display"></section>

      <input
        id="input"
        type="text"
        autoFocus
        autoComplete="off"
        autoCorrect="off"
        autoCapitalize="off"
        spellCheck="false"
      />

      <div id="actions">
        <button onClick={addLast}>add last</button>
      </div>
    </main>

  )
}
