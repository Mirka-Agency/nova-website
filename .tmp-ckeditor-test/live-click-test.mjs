import { chromium } from "playwright";

const BASE = "https://localhost:7211";
const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true });
const page = await context.newPage();
page.on("pageerror", (e) => console.log("PAGEERROR", e.message));

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
if (await cont.count()) await cont.click().catch(() => {});

await page.evaluate(() => {
  const editor = document.querySelector("textarea.admin-ckeditor-source").ckEditorInstance;
  editor.setData("<p>یک دو سه چهار پنج شش هفت هشت نه ده.</p>");
  editor.editing.view.focus();
  editor.model.change((writer) => {
    const root = editor.model.document.getRoot();
    writer.setSelection(writer.createRangeIn(root.getChild(0)));
  });
});

const btnInfo = await page.evaluate(() => {
  const nodes = Array.from(
    document.querySelectorAll(".ck-toolbar [data-cke-tooltip-text], .ck-toolbar [aria-label]")
  ).filter((el) => {
    const t =
      el.getAttribute("data-cke-tooltip-text") ||
      el.getAttribute("aria-label") ||
      "";
    return t.includes("هایلایت اسکرولی");
  });
  return nodes.map((el) => ({
    tag: el.tagName,
    className: el.className,
    tooltip: el.getAttribute("data-cke-tooltip-text"),
    aria: el.getAttribute("aria-label"),
    disabled: el.getAttribute("aria-disabled") || el.classList.contains("ck-disabled"),
  }));
});
console.log("BTN_INFO", JSON.stringify(btnInfo, null, 2));

// Click first matching toolbar control (action side)
await page.evaluate(() => {
  const el = Array.from(
    document.querySelectorAll(".ck-toolbar button, .ck-toolbar .ck-button")
  ).find((b) => {
    const t =
      b.getAttribute("data-cke-tooltip-text") ||
      b.getAttribute("aria-label") ||
      "";
    return t.includes("هایلایت اسکرولی") && !b.classList.contains("ck-splitbutton__arrow");
  });
  if (!el) throw new Error("action button not found");
  el.click();
});

await page.waitForTimeout(400);
const afterAction = await page.evaluate(() => {
  const editor = document.querySelector("textarea.admin-ckeditor-source").ckEditorInstance;
  return {
    data: editor.getData(),
    collapsed: editor.model.document.selection.isCollapsed,
    value: editor.commands.get("scrollHighlight").value,
  };
});
console.log("AFTER_ACTION", JSON.stringify(afterAction, null, 2));

// Reset and try via arrow + apply
await page.evaluate(() => {
  const editor = document.querySelector("textarea.admin-ckeditor-source").ckEditorInstance;
  editor.setData("<p>یک دو سه چهار پنج شش هفت هشت نه ده.</p>");
  editor.model.change((writer) => {
    const root = editor.model.document.getRoot();
    writer.setSelection(writer.createRangeIn(root.getChild(0)));
  });
});

await page.evaluate(() => {
  const el = Array.from(
    document.querySelectorAll(".ck-toolbar .ck-splitbutton__arrow, .ck-toolbar button")
  ).find((b) => {
    const t =
      b.getAttribute("data-cke-tooltip-text") ||
      b.getAttribute("aria-label") ||
      "";
    return (
      b.classList.contains("ck-splitbutton__arrow") ||
      (t.includes("هایلایت اسکرولی") && b.classList.contains("ck-splitbutton__arrow"))
    );
  });
  // more reliable:
  const arrow = document.querySelector(
    ".ck-toolbar .ck-splitbutton__arrow"
  );
  // find the scroll highlight splitbutton specifically
  const split = Array.from(document.querySelectorAll(".ck-toolbar .ck-splitbutton")).find(
    (s) =>
      (s.getAttribute("data-cke-tooltip-text") || "").includes("هایلایت") ||
      s.querySelector("[data-cke-tooltip-text*='هایلایت اسکرولی']")
  );
  const a = split ? split.querySelector(".ck-splitbutton__arrow") : arrow;
  if (!a) throw new Error("arrow not found");
  a.click();
});

await page.waitForTimeout(300);
const formVisible = await page.evaluate(
  () => !!document.querySelector(".ck-scroll-highlight-form")
);
console.log("FORM_VISIBLE", formVisible);

if (formVisible) {
  await page.evaluate(() => {
    const btn = document.querySelector(
      ".ck-scroll-highlight-form button.ck-button-action, .ck-scroll-highlight-form .ck-button-action"
    );
    if (!btn) throw new Error("apply missing");
    btn.click();
  });
  await page.waitForTimeout(300);
}

const afterForm = await page.evaluate(() => {
  const editor = document.querySelector("textarea.admin-ckeditor-source").ckEditorInstance;
  return { data: editor.getData() };
});
console.log("AFTER_FORM", JSON.stringify(afterForm, null, 2));

await browser.close();
const ok =
  afterAction.data.includes("nova-scroll-highlight") ||
  afterForm.data.includes("nova-scroll-highlight");
process.exit(ok ? 0 : 1);
