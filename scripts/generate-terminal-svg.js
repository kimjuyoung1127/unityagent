#!/usr/bin/env node
/**
 * Converts ANSI terminal output to a styled SVG file for README embedding.
 * Usage: node generate-terminal-svg.js <input.ansi> <output.svg> [title]
 */
const fs = require('fs');
const { execSync } = require('child_process');

const [,, inputFile, outputFile, title = ''] = process.argv;
if (!inputFile || !outputFile) {
    console.error('Usage: node generate-terminal-svg.js <input.ansi> <output.svg> [title]');
    process.exit(1);
}

const ansi = fs.readFileSync(inputFile, 'utf8');

// Convert ANSI to HTML via ansi-to-html
const Convert = require('ansi-to-html');
const converter = new Convert({
    fg: '#c0c0c0',
    bg: '#1e1e2e',
    colors: {
        0: '#45475a', 1: '#f38ba8', 2: '#a6e3a1', 3: '#f9e2af',
        4: '#89b4fa', 5: '#cba6f7', 6: '#94e2d5', 7: '#bac2de',
        8: '#585b70', 9: '#f38ba8', 10: '#a6e3a1', 11: '#f9e2af',
        12: '#89b4fa', 13: '#cba6f7', 14: '#94e2d5', 15: '#a6adc8'
    }
});

const html = converter.toHtml(ansi);

// Count lines for height calculation
const lines = ansi.split('\n');
const lineCount = lines.length;
const charWidth = 8.4;
const lineHeight = 20;
const padding = 16;
const titleBarHeight = title ? 36 : 0;
const contentHeight = lineCount * lineHeight + padding * 2 + titleBarHeight;

// Find max line width (strip ANSI codes)
const stripAnsi = (str) => str.replace(/\x1b\[[0-9;]*m/g, '');
const maxChars = Math.max(...lines.map(l => stripAnsi(l).length), 60);
const contentWidth = Math.max(maxChars * charWidth + padding * 2, 500);

const titleBar = title ? `
    <g>
      <circle cx="20" cy="18" r="6" fill="#f38ba8"/>
      <circle cx="38" cy="18" r="6" fill="#f9e2af"/>
      <circle cx="56" cy="18" r="6" fill="#a6e3a1"/>
      <text x="${contentWidth / 2}" y="22" text-anchor="middle"
            font-family="monospace" font-size="13" fill="#6c7086">${escapeXml(title)}</text>
    </g>` : '';

const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${contentWidth}" height="${contentHeight}">
  <style>
    .terminal { font-family: 'Cascadia Code', 'JetBrains Mono', 'Fira Code', monospace; font-size: 14px; }
    .terminal span { white-space: pre; }
  </style>
  <rect width="100%" height="100%" rx="8" fill="#1e1e2e"/>
  ${titleBar}
  <foreignObject x="${padding}" y="${padding + titleBarHeight}" width="${contentWidth - padding * 2}" height="${contentHeight - padding - titleBarHeight}">
    <div xmlns="http://www.w3.org/1999/xhtml" class="terminal" style="color: #cdd6f4; line-height: ${lineHeight}px; white-space: pre-wrap; word-break: break-all;">
${html}
    </div>
  </foreignObject>
</svg>`;

fs.writeFileSync(outputFile, svg);
console.log(`Generated: ${outputFile} (${contentWidth}x${contentHeight})`);

function escapeXml(s) {
    return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}
