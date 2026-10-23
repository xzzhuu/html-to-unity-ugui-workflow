// Serve and test the maintained Starport example on an available loopback port.
const fs = require('fs');
const path = require('path');
const http = require('http');
const { once } = require('events');
const root = path.resolve(__dirname, '../Workspaces/StarportSalvage');
const types = { '.html':'text/html; charset=utf-8', '.css':'text/css', '.js':'application/javascript', '.json':'application/json', '.png':'image/png', '.svg':'image/svg+xml' };
let server;
(async () => {
  try {
    if (!process.env.STARPORT_BASE_URL) {
      server = http.createServer((request, response) => {
        let file;
        try {
          const name = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
          file = path.resolve(root, '.' + (name === '/' ? '/index.html' : name));
          if (!file.startsWith(root + path.sep)) throw new Error('Outside workspace');
        } catch {
          response.writeHead(400).end();
          return;
        }
        fs.readFile(file, (error, data) => {
          if (error) { response.writeHead(404).end(); return; }
          response.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream' });
          response.end(data);
        });
      });
      server.listen(0, '127.0.0.1');
      await once(server, 'listening');
      process.env.STARPORT_BASE_URL = `http://127.0.0.1:${server.address().port}/`;
    }
    await require('../Workspaces/StarportSalvage/design-tools/verify_browser.cjs')();
  } finally {
    if (server) await new Promise(resolve => server.close(resolve));
  }
})().catch(error => { console.error(error.message); process.exitCode = 1; });
