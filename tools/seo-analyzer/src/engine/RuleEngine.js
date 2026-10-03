/**
 * Simple RuleEngine: register(rule) → run(context) → results[]
 */

export class RuleEngine {
  constructor() {
    /** @type {Array<(ctx: any) => any | any[]>} */
    this.rules = [];
  }

  /**
   * @param {(ctx: any) => any | any[]} ruleFn
   */
  register(ruleFn) {
    if (typeof ruleFn === 'function') {
      this.rules.push(ruleFn);
    }
    return this;
  }

  /**
   * @param {any} context
   * @returns {import('./types.js').SeoRuleResult[]}
   */
  run(context) {
    const results = [];
    for (const rule of this.rules) {
      try {
        const out = rule(context);
        if (!out) continue;
        if (Array.isArray(out)) {
          for (const item of out) {
            if (item && item.id) results.push(item);
          }
        } else if (out.id) {
          results.push(out);
        }
      } catch (err) {
        results.push({
          id: 'rule-error',
          category: 'system',
          status: 'problem',
          score: 0,
          title: 'خطای قانون',
          description: err && err.message ? String(err.message) : 'خطای ناشناخته در تحلیل',
          fix: 'لطفاً صفحه را تازه کنید یا با پشتیبانی تماس بگیرید.',
        });
      }
    }
    return results;
  }
}
