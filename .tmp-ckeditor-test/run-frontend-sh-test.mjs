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

// Mid scroll — progress should advance
await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight * 0.4));
await page.waitForTimeout(200);
const mid = await page.evaluate(() => {
  const el = document.querySelector(".scroll-scrub-highlight");
  return {
    progress: Number(el.getAttribute("data-scroll-scrub-progress") || "0"),
    done: el.classList.contains("is-scroll-scrub-done"),
    p0: el.querySelector(".scroll-scrub-line")?.style.getPropertyValue("--scroll-scrub-p"),
  };
});

// Full scroll down — should lock at 100
await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
await page.waitForTimeout(200);
const end = await page.evaluate(() => {
  const el = document.querySelector(".scroll-scrub-highlight");
  const word = el.querySelector(".scroll-scrub-word");
  return {
    progress: Number(el.getAttribute("data-scroll-scrub-progress") || "0"),
    done: el.classList.contains("is-scroll-scrub-done"),
    onLines: el.querySelectorAll(".scroll-scrub-line.is-scroll-scrub-on").length,
    totalLines: el.querySelectorAll(".scroll-scrub-line").length,
    bgSize: word ? getComputedStyle(word).backgroundSize : null,
  };
});

// Scroll back up — must NOT reverse
await page.evaluate(() => window.scrollTo(0, 0));
await page.waitForTimeout(200);
const afterUp = await page.evaluate(() => {
  const el = document.querySelector(".scroll-scrub-highlight");
  const word = el.querySelector(".scroll-scrub-word");
  return {
    progress: Number(el.getAttribute("data-scroll-scrub-progress") || "0"),
    done: el.classList.contains("is-scroll-scrub-done"),
    onLines: el.querySelectorAll(".scroll-scrub-line.is-scroll-scrub-on").length,
    bgSize: word ? getComputedStyle(word).backgroundSize : null,
  };
});

const result = { before, mid, end, afterUp };
console.log(JSON.stringify(result, null, 2));
await browser.close();

const ok =
  before.ready &&
  before.words > 0 &&
  before.lines > 0 &&
  end.done &&
  end.progress === 100 &&
  end.onLines === end.totalLines &&
  afterUp.done &&
  afterUp.progress === 100 &&
  afterUp.onLines === end.totalLines &&
  String(afterUp.bgSize || "").includes("100%");
process.exit(ok ? 0 : 1);
