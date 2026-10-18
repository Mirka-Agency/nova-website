import { chromium } from "playwright";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const file = path.join(__dirname, "scroll-scrub-plugin-test.html");

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
page.on("pageerror", (e) => console.error("PAGEERROR", e.message));
page.on("console", (m) => {
  if (m.type() === "error") console.error("CONSOLE", m.text());
});
await page.goto("file:///" + file.replace(/\\/g, "/"), { waitUntil: "networkidle" });
await page.waitForFunction(() => window.__PLUGIN_TEST__, null, { timeout: 20000 });
const r = await page.evaluate(() => window.__PLUGIN_TEST__);
console.log(JSON.stringify(r, null, 2));
await browser.close();

const ok =
  !r.error &&
  r.hasCommand &&
  r.hasComponent &&
  r.applyOk &&
  r.removeOk &&
  r.upcastOk &&
  r.legacyOk;
process.exit(ok ? 0 : 1);
