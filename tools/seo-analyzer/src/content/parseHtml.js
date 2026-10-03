/**
 * Parse HTML content for SEO signals.
 * Uses DOMParser in browser; falls back to lightweight regex in non-DOM envs.
 */

/**
 * @typedef {Object} ParsedContent
 * @property {string} plainText
 * @property {string} firstParagraph
 * @property {{level:number,text:string}[]} headings
 * @property {{href:string,text:string,internal:boolean}[]} links
 * @property {{src:string,alt:string}[]} images
 * @property {string[]} paragraphs
 */

function stripTags(html) {
  return String(html || '')
    .replace(/<script[\s\S]*?<\/script>/gi, ' ')
    .replace(/<style[\s\S]*?<\/style>/gi, ' ')
    .replace(/<br\s*\/?>/gi, '\n')
    .replace(/<\/(p|div|li|h[1-6]|tr|blockquote)>/gi, '\n')
    .replace(/<[^>]+>/g, ' ')
    .replace(/&nbsp;/gi, ' ')
    .replace(/&amp;/gi, '&')
    .replace(/&lt;/gi, '<')
    .replace(/&gt;/gi, '>')
    .replace(/&quot;/gi, '"')
    .replace(/&#39;/gi, "'")
    .replace(/[ \t\f\v]+/g, ' ')
    .replace(/\n[ \t]+/g, '\n')
    .replace(/[ \t]+\n/g, '\n')
    .replace(/\n{2,}/g, '\n')
    .trim();
}

function isInternalHref(href) {
  if (!href) return false;
  const h = href.trim();
  if (h.startsWith('#') || h.startsWith('mailto:') || h.startsWith('tel:') || h.startsWith('javascript:')) {
    return false;
  }
  if (h.startsWith('/') || h.startsWith('./') || h.startsWith('../')) return true;
  try {
    if (typeof window !== 'undefined' && window.location) {
      const u = new URL(h, window.location.origin);
      return u.origin === window.location.origin;
    }
  } catch {
    /* ignore */
  }
  return !/^https?:\/\//i.test(h);
}

/** Prefer block-level lines so Persian text without "." still splits by paragraph. */
function plainTextFromBlocks(root) {
  const lines = [];
  const blocks = root.querySelectorAll('p, li, h1, h2, h3, h4, h5, h6, blockquote, td, th');
  const blockSet = new Set(blocks);

  if (blocks.length) {
    blocks.forEach((el) => {
      // Skip nested duplicates (e.g. p inside li)
      if (el.parentElement && blockSet.has(el.parentElement)) return;
      const t = (el.textContent || '').replace(/\s+/g, ' ').trim();
      if (t) lines.push(t);
    });
  }
  if (lines.length) return lines.join('\n');
  return (root.textContent || '').replace(/\s+/g, ' ').trim();
}

function parseWithDom(html) {
  const parser = new DOMParser();
  const doc = parser.parseFromString(`<div id="__root">${html}</div>`, 'text/html');
  const root = doc.getElementById('__root') || doc.body;

  const headings = [];
  root.querySelectorAll('h1,h2,h3,h4,h5,h6').forEach((el) => {
    const level = Number(el.tagName.substring(1));
    headings.push({ level, text: (el.textContent || '').trim() });
  });

  const paragraphs = [];
  root.querySelectorAll('p').forEach((el) => {
    const t = (el.textContent || '').replace(/\s+/g, ' ').trim();
    if (t) paragraphs.push(t);
  });

  const links = [];
  root.querySelectorAll('a[href]').forEach((el) => {
    const href = el.getAttribute('href') || '';
    links.push({
      href,
      text: (el.textContent || '').trim(),
      internal: isInternalHref(href),
    });
  });

  const images = [];
  root.querySelectorAll('img').forEach((el) => {
    images.push({
      src: el.getAttribute('src') || '',
      alt: el.getAttribute('alt') || '',
    });
  });

  const plainText = plainTextFromBlocks(root);
  const firstParagraph =
    paragraphs[0] ||
    (() => {
      const block = root.querySelector('p,div,li');
      return block
        ? (block.textContent || '').replace(/\s+/g, ' ').trim()
        : plainText.split('\n')[0] || '';
    })();

  return { plainText, firstParagraph, headings, links, images, paragraphs };
}

function parseWithRegex(html) {
  const source = String(html || '');
  const headings = [];
  const headingRe = /<h([1-6])[^>]*>([\s\S]*?)<\/h\1>/gi;
  let m;
  while ((m = headingRe.exec(source))) {
    headings.push({ level: Number(m[1]), text: stripTags(m[2]).replace(/\n/g, ' ').trim() });
  }

  const paragraphs = [];
  const pRe = /<p[^>]*>([\s\S]*?)<\/p>/gi;
  while ((m = pRe.exec(source))) {
    const t = stripTags(m[1]).replace(/\n/g, ' ').trim();
    if (t) paragraphs.push(t);
  }

  const links = [];
  const aRe = /<a\s+[^>]*href=["']([^"']+)["'][^>]*>([\s\S]*?)<\/a>/gi;
  while ((m = aRe.exec(source))) {
    links.push({
      href: m[1],
      text: stripTags(m[2]).replace(/\n/g, ' ').trim(),
      internal: isInternalHref(m[1]),
    });
  }

  const images = [];
  const imgRe = /<img\s+[^>]*>/gi;
  while ((m = imgRe.exec(source))) {
    const tag = m[0];
    const src = (tag.match(/src=["']([^"']*)["']/i) || [])[1] || '';
    const altMatch = tag.match(/alt=["']([^"']*)["']/i);
    const alt = altMatch ? altMatch[1] : '';
    const hasAltAttr = /\salt\s*=/i.test(tag);
    images.push({ src, alt: hasAltAttr ? alt : '' });
  }

  const plainText = paragraphs.length ? paragraphs.join('\n') : stripTags(source);
  const firstParagraph = paragraphs[0] || plainText.split('\n')[0] || '';

  return { plainText, firstParagraph, headings, links, images, paragraphs };
}

/**
 * @param {string} html
 * @returns {ParsedContent}
 */
export function parseHtml(html) {
  if (!html || !String(html).trim()) {
    return {
      plainText: '',
      firstParagraph: '',
      headings: [],
      links: [],
      images: [],
      paragraphs: [],
    };
  }

  if (typeof DOMParser !== 'undefined') {
    try {
      return parseWithDom(html);
    } catch {
      return parseWithRegex(html);
    }
  }
  return parseWithRegex(html);
}
