import { chromium } from "playwright";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const file = path.join(__dirname, "frontend-sh-test.html");

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
await page.goto("file:///" + file.replace(/\\/g, "/"), { waitUntil: "networkidle" });
await page.waitForFunction(() => window.__FE_SH__ && window.__FE_SH__.ready, null, {
  timeout: 10000,
});
const before = await page.evaluate(() => window.__FE_SH__);

// Scroll so trigger advances
await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight * 0.45));
await page.waitForTimeout(800);
const mid = await page.evaluate(() => {
  const el = document.querySelector(".nova-scroll-highlight");
  return {
    progress: el.getAttribute("data-nova-sh-progress"),
    pVar: getComputedStyle(el).getPropertyValue("--nova-sh-p").trim(),
  };
});

await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
await page.waitForTimeout(800);
const end = await page.evaluate(() => {
  const el = document.querySelector(".nova-scroll-highlight");
  return {
    progress: el.getAttribute("data-nova-sh-progress"),
    pVar: getComputedStyle(el).getPropertyValue("--nova-sh-p").trim(),
  };
});

const result = { before, mid, end };
console.log(JSON.stringify(result, null, 2));
await browser.close();

const ok =
  before.ready &&
  before.hasCharSpans === 0 &&
  before.text &&
  before.text.includes("تیروئید") &&
  Number(end.progress) >= Number(mid.progress) &&
  Number(end.progress) > 0;
process.exit(ok ? 0 : 1);
