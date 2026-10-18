import { chromium } from "playwright";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const file = path.join(__dirname, "scroll-highlight-test.html");

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
page.on("console", (msg) => console.log("BROWSER:", msg.type(), msg.text()));
page.on("pageerror", (err) => console.log("PAGEERROR:", err.message));

await page.goto("file:///" + file.replace(/\\/g, "/"), {
  waitUntil: "networkidle",
});
await page.waitForFunction(() => window.__SH_TEST__, null, { timeout: 30000 });
const result = await page.evaluate(() => window.__SH_TEST__);
console.log(JSON.stringify(result, null, 2));
const failed = (result || []).filter((r) => !r.ok);
await browser.close();
process.exit(failed.length ? 1 : 0);
