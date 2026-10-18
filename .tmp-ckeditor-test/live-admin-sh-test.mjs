import { chromium } from "playwright";

const BASE = "https://localhost:7211";
const EMAIL = "admin@local.test";
const PASS = "Admin123!";

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true });
const page = await context.newPage();
const logs = [];
const failed = [];
page.on("console", (msg) => logs.push([msg.type(), msg.text().slice(0, 400)]));
page.on("pageerror", (err) => logs.push(["pageerror", err.message.slice(0, 600)]));
page.on("response", (res) => {
  if (res.status() >= 400) failed.push([res.status(), res.url()]);
});

const result = { steps: [], failed: [], errors: [] };

try {
  await page.goto(BASE + "/Admin/Account/Login", { waitUntil: "networkidle" });
  await page.fill('input[name="Email"], #Email', EMAIL);
  await page.fill('input[name="Password"], #Password', PASS);
  await Promise.all([
    page.waitForNavigation({ waitUntil: "networkidle", timeout: 20000 }),
    page.click('button[type="submit"]'),
  ]);
  result.steps.push({ step: "login", url: page.url() });

  await page.goto(BASE + "/Admin/Posts/Create", { waitUntil: "domcontentloaded" });
  // dismiss draft resume modal if present
  const continueBtn = page.locator("[data-draft-resume-continue]").first();
  if (await continueBtn.count()) {
    await continueBtn.click({ timeout: 2000 }).catch(() => {});
  }
  result.steps.push({ step: "create", url: page.url() });

  await page.waitForSelector("textarea.admin-ckeditor-source", {
    state: "attached",
    timeout: 20000,
  });

  for (let i = 0; i < 60; i++) {
    const ready = await page.evaluate(() => {
      const ta = document.querySelector("textarea.admin-ckeditor-source");
      return !!(ta && ta.ckEditorInstance);
    });
    if (ready) break;
    await page.waitForTimeout(250);
  }

  // dismiss modal again if it appeared
  if (await continueBtn.count()) {
    await continueBtn.click({ timeout: 1000 }).catch(() => {});
  }

  const probe = await page.evaluate(() => {
    const out = {
      ckeditorGlobal: typeof CKEDITOR !== "undefined",
      nova: !!(window.NovaScrollHighlight && window.NovaScrollHighlight.plugin),
      scripts: Array.from(document.scripts)
        .map((s) => (s.src || "").split("?")[0])
        .filter((s) => /ckeditor|scroll-highlight/i.test(s)),
      ckReadyAttr: document
        .querySelector("textarea.admin-ckeditor-source")
        ?.getAttribute("data-ck-initial-html"),
      editorDom: !!document.querySelector(".ck-editor"),
    };
    const ta = document.querySelector("textarea.admin-ckeditor-source");
    out.hasInstance = !!(ta && ta.ckEditorInstance);
    if (!out.hasInstance) return out;

    const editor = ta.ckEditorInstance;
    out.hasCommand = !!editor.commands.get("scrollHighlight");
    out.hasComponent = editor.ui.componentFactory.has("scrollHighlight");
    out.pluginNames = [];
    for (const p of editor.plugins) {
      const n = p.constructor.pluginName || p.constructor.name;
      if (/scroll|Scroll|Highlight/i.test(n)) out.pluginNames.push(n);
    }

    editor.setData("<p>متن آزمایشی هایلایت اسکرولی برای تست زنده.</p>");
    editor.model.change((writer) => {
      const root = editor.model.document.getRoot();
      writer.setSelection(writer.createRangeIn(root.getChild(0)));
    });
    const cmd = editor.commands.get("scrollHighlight");
    out.enabled = !!(cmd && cmd.isEnabled);
    try {
      editor.execute("scrollHighlight", {
        action: "apply",
        from: "#6b7280",
        to: "#0f172a",
      });
      out.data = editor.getData();
      out.applyOk = out.data.includes("nova-scroll-highlight");
    } catch (e) {
      out.executeError = String(e);
    }

    try {
      const dd = editor.ui.componentFactory.create("scrollHighlight");
      dd.render();
      document.body.appendChild(dd.element);
      editor.model.change((writer) => {
        const root = editor.model.document.getRoot();
        writer.setSelection(writer.createRangeIn(root.getChild(0)));
      });
      dd.buttonView.fire("execute");
      out.splitData = editor.getData();
      out.splitOk = /nova-scroll-highlight/.test(out.splitData);
      dd.isOpen = true;
      out.formOk = !!document.querySelector(".ck-scroll-highlight-form");
      dd.isOpen = false;
      dd.destroy();
    } catch (e) {
      out.uiError = String(e);
    }

    out.toolbarLabels = Array.from(
      document.querySelectorAll(
        ".ck-toolbar [aria-label], .ck-toolbar [data-cke-tooltip-text]"
      )
    )
      .map(
        (el) =>
          el.getAttribute("aria-label") ||
          el.getAttribute("data-cke-tooltip-text") ||
          ""
      )
      .filter(Boolean);

    out.hasScrollBtn = out.toolbarLabels.some((l) =>
      /هایلایت اسکرولی|scroll highlight/i.test(l)
    );
    return out;
  });

  result.probe = probe;
  result.consoleErrors = logs.filter(
    (l) => l[0] === "error" || l[0] === "pageerror"
  );
  result.failed = failed.filter((f) =>
    /ckeditor|scroll|admin\/js/i.test(f[1])
  );
} catch (e) {
  result.errors.push(String(e && e.stack ? e.stack : e));
  result.consoleErrors = logs.slice(-50);
}

console.log(JSON.stringify(result, null, 2));
await browser.close();
const ok =
  !result.errors.length &&
  result.probe &&
  result.probe.applyOk &&
  result.probe.hasScrollBtn;
process.exit(ok ? 0 : 1);
