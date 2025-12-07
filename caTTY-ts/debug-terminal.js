// Debug script to inspect terminal state
// Run this in the browser console when the issue occurs

function debugTerminal() {
  // Find the terminal display element
  const display = document.querySelector('.terminal-display') || document.querySelector('[class*="terminal"]');
  
  if (!display) {
    console.error('Could not find terminal display element');
    return;
  }
  
  console.log('=== Terminal Display Debug ===');
  console.log('Display element:', display);
  console.log('Display dimensions:', {
    width: display.offsetWidth,
    height: display.offsetHeight,
    clientWidth: display.clientWidth,
    clientHeight: display.clientHeight
  });
  
  // Find all line elements
  const lines = display.querySelectorAll('div[style*="position: absolute"]');
  console.log(`\nTotal line elements: ${lines.length}`);
  
  // Group lines by their top position
  const linesByRow = new Map();
  lines.forEach(line => {
    const top = line.style.top;
    if (!linesByRow.has(top)) {
      linesByRow.set(top, []);
    }
    linesByRow.set(top, [...linesByRow.get(top), line]);
  });
  
  console.log(`\nLines grouped by row:`);
  const sortedRows = Array.from(linesByRow.keys()).sort((a, b) => {
    const aNum = parseFloat(a);
    const bNum = parseFloat(b);
    return aNum - bNum;
  });
  
  sortedRows.forEach(top => {
    const linesAtRow = linesByRow.get(top);
    const rowNum = parseFloat(top);
    const content = linesAtRow.map(l => l.textContent).join('');
    const hasContent = content.trim().length > 0;
    
    console.log(`Row ${rowNum} (${top}): ${linesAtRow.length} element(s), ` +
                `content: "${content.substring(0, 50)}${content.length > 50 ? '...' : ''}", ` +
                `hasContent: ${hasContent}`);
    
    if (!hasContent && linesAtRow.length > 0) {
      console.warn(`  ⚠️  Row ${rowNum} has elements but no visible content!`);
      linesAtRow.forEach((line, i) => {
        console.log(`    Element ${i}:`, {
          textContent: line.textContent,
          innerHTML: line.innerHTML,
          childNodes: line.childNodes.length,
          style: line.style.cssText
        });
      });
    }
  });
  
  // Check for gaps in row numbers
  console.log(`\nChecking for gaps in row numbers:`);
  for (let i = 0; i < sortedRows.length - 1; i++) {
    const currentRow = parseFloat(sortedRows[i]);
    const nextRow = parseFloat(sortedRows[i + 1]);
    const gap = nextRow - currentRow;
    
    if (gap > 1) {
      console.warn(`  ⚠️  Gap detected: rows ${currentRow} to ${nextRow} (gap of ${gap - 1} rows)`);
    }
  }
  
  return {
    display,
    lines,
    linesByRow,
    sortedRows
  };
}

// Export to window for easy access
window.debugTerminal = debugTerminal;

console.log('Debug function loaded. Run debugTerminal() to inspect terminal state.');
