const fs = require('fs');
const path = require('path');

const dir = 'C:/Users/ezen601/Desktop/Jason/unityctl/ralph/competitor-analysis';

const files = {
  'CoderGamester/mcp-unity': path.join(dir, 'competitor_codergamester_schema.json'),
};

// Only process files that exist, compute the rest from known data
for (const [name, fpath] of Object.entries(files)) {
  try {
    const data = JSON.parse(fs.readFileSync(fpath, 'utf8'));
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
    const jsonStr = JSON.stringify(data);
    console.log(`${name}: tools=${tools.length}, params=${params}, chars=${jsonStr.length}, tokens=${Math.round(jsonStr.length/4)}`);
  } catch(e) {
    console.log(`${name}: ERROR ${e.message}`);
  }
}
