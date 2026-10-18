import { chromium } from "playwright";

const BASE = "https://localhost:7211";
const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true });
const page = await context.newPage();

await page.goto(BASE + "/Admin/Account/Login", { waitUntil: "networkidle" });
await page.fill('input[name="Email"], #Email', "admin@local.test");
await page.fill('input[name="Password"], #Password', "Admin123!");
await Promise.all([
  page.waitForNavigation({ waitUntil: "networkidle" }),
  page.click('button[type="submit"]'),
]);
await page.goto(BASE + "/Admin/Posts/Create", { waitUntil: "domcontentloaded" });
const cont = page.locator("[data-draft-resume-continue]").first();
if (await cont.count()) await cont.click().catch(() => {});
await page.waitForSelector("textarea.admin-ckeditor-source", { state: "attached" });
for (let i = 0; i < 60; i++) {
  if (await page.evaluate(() => document.querySelector("textarea.admin-ckeditor-source")?.ckEditorInstance)) break;
  await page.waitForTimeout(250);
}

const info = await page.evaluate(() => {
  const labels = (root) =>
    Array.from(root.querySelectorAll("[aria-label], [data-cke-tooltip-text]"))
      .map((el) => el.getAttribute("aria-label") || el.getAttribute("data-cke-tooltip-text") || "")
      .filter((t) => /هایلایت|برجسته|scroll/i.test(t));

  const toolbar = document.querySelector(".ck-toolbar");
  const grouped = document.querySelector(".ck-toolbar__grouped-dropdown");
  const editable = document.querySelector(".ck-editor__editable");

  return {
    viewport: { w: window.innerWidth, h: window.innerHeight },
    toolbarWidth: toolbar ? toolbar.getBoundingClientRect().width : null,
    editorWidth: editable ? editable.getBoundingClientRect().width : null,
    hasGroupedDropdown: !!grouped,
    groupedVisible: !!(grouped && grouped.offsetParent !== null),
    mainToolbarHits: labels(toolbar || document),
    insideGrouped: grouped ? labels(grouped) : [],
    // elements with label that are not displayed
    hiddenScrollButtons: Array.from(
      document.querySelectorAll('[aria-label="هایلایت اسکرولی متن"]')
    ).map((el) => {
      const st = getComputedStyle(el);
      const r = el.getBoundingClientRect();
      return {
        display: st.display,
        visibility: st.visibility,
        w: r.width,
        h: r.height,
        inGrouped: !!el.closest(".ck-toolbar__grouped-dropdown"),
        className: el.className,
      };
    }),
  };
});

console.log(JSON.stringify(info, null, 2));
await browser.close();
