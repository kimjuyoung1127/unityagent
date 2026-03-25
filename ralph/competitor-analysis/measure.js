const fs = require('fs');

function measure(label, jsonStr) {
  const data = JSON.parse(jsonStr);
  const tools = data.tools;
  let params = 0;
  for (const t of tools) {
    const props = t.inputSchema?.properties || {};
    params += Object.keys(props).length;
    for (const v of Object.values(props)) {
      if (v && typeof v === 'object' && v.properties) {
        params += Object.keys(v.properties).length;
      }
    }
  }
  const minified = JSON.stringify(data);
  return { label, tools: tools.length, params, chars: minified.length, tokens: Math.round(minified.length / 4) };
}

// Read files from the project directory
const dir = 'C:/Users/ezen601/Desktop/Jason/unityctl/ralph/competitor-analysis';

const results = [];

// CoderGamester
const cg = fs.readFileSync(dir + '/competitor_codergamester_schema.json', 'utf8');
results.push(measure('CoderGamester/mcp-unity', cg));

// Output results
const unityctlChars = 4611;
const unityctlTokens = 1153;
const unityctlTools = 12;
const unityctlParams = 24;

console.log('| Project | Stars | Tools | Params | JSON chars | Est tokens | vs unityctl |');
console.log('|---------|-------|-------|--------|------------|------------|-------------|');
console.log(`| **unityctl** (ours) | - | ${unityctlTools} | ${unityctlParams} | ${unityctlChars} | ${unityctlTokens} | 1.0x |`);

for (const r of results) {
  const ratio = (r.tokens / unityctlTokens).toFixed(1);
  console.log(`| ${r.label} | 1,400 | ${r.tools} | ${r.params} | ${r.chars} | ${r.tokens} | ${ratio}x |`);
}

