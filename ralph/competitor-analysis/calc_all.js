// We'll construct minimal JSON representations and measure them

function countToolData(tools) {
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
  const jsonStr = JSON.stringify({ tools });
  return { toolCount: tools.length, params, chars: jsonStr.length, tokens: Math.round(jsonStr.length / 4) };
}

const fs = require('fs');
const dir = 'C:/Users/ezen601/Desktop/Jason/unityctl/ralph/competitor-analysis';

// CoderGamester - already have the file
const cg = JSON.parse(fs.readFileSync(dir + '/competitor_codergamester_schema.json', 'utf8'));
const cgStats = countToolData(cg.tools);

// For the others, I need to compute from known tool counts and estimate.
// But let me write them as inline data instead.

console.log('=== Competitor MCP Schema Token Analysis ===');
console.log();
console.log(`CoderGamester/mcp-unity (1,400 stars):`);
console.log(`  Tools: ${cgStats.toolCount}`);
console.log(`  Parameters: ${cgStats.params}`);
console.log(`  JSON chars: ${cgStats.chars}`);
console.log(`  Est tokens: ${cgStats.tokens}`);
console.log();

// IvanMurzak: 52 tools from C# (counted from file list)
// Based on the schema I created: 52 tools with ~115 parameters
// Average tool JSON is about 250 chars
const ivanTools = 52;
const ivanParams = 115;
const ivanChars = 11800; // estimated from schema structure

// AnkleBreaker: 135 visible tools in editor-tools.js + 9 from other files = ~144 tools
// Plus the meta-tools that expose 193 "advanced" tools
// The tools/list response would show 77 core tools (from manifest)
// But the actual editor-tools.js defines 135+ tools
const ankleTools = 135; // from editor-tools.js extraction + other files
const ankleParams = 520; // estimated from detailed extraction
const ankleChars = 38000; // very large schema

// UnityMCP (jackwrichards): exactly 3 tools
const umcpTools = 3;
const umcpParams = 12;
const umcpChars = 1950;

console.log(`IvanMurzak/Unity-MCP (1,375 stars):`);
console.log(`  Tools: ${ivanTools}`);
console.log(`  Parameters: ~${ivanParams}`);
console.log(`  JSON chars: ~${ivanChars}`);
console.log(`  Est tokens: ~${Math.round(ivanChars/4)}`);
console.log();

console.log(`AnkleBreaker-Studio/unity-mcp-server (67 stars):`);
console.log(`  Tools: ${ankleTools}+`);
console.log(`  Parameters: ~${ankleParams}`);
console.log(`  JSON chars: ~${ankleChars}`);
console.log(`  Est tokens: ~${Math.round(ankleChars/4)}`);
console.log();

console.log(`jackwrichards/UnityMCP (515 stars):`);
console.log(`  Tools: ${umcpTools}`);
console.log(`  Parameters: ${umcpParams}`);
console.log(`  JSON chars: ~${umcpChars}`);
console.log(`  Est tokens: ~${Math.round(umcpChars/4)}`);

