import { chromium } from "playwright";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const file = path.join(__dirname, "ui-flow-test.html");

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
page.on("console", (msg) => console.log("BROWSER:", msg.type(), msg.text()));
page.on("pageerror", (err) => console.log("PAGEERROR:", err.message));

await page.goto("file:///" + file.replace(/\\/g, "/"), { waitUntil: "networkidle" });
await page.waitForFunction(() => window.__UI_TEST__, null, { timeout: 30000 });
const result = await page.evaluate(() => window.__UI_TEST__);
console.log(JSON.stringify(result, null, 2));
await browser.close();
process.exit(result.some((r) => !r.ok) ? 1 : 0);
