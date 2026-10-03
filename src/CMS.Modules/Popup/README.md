# Popup Module

Marketing / lead popups for the public site, managed from Admin.

## Structure

- `Domain` — `PopupItem` aggregate, trigger/frequency/page-target enums
- `Application` — DTOs, validators, `IPopupService`, `IPublicPopupQuery`, page matcher
- `Infrastructure` — `PopupDbContext` (schema `popup`), EF configs, services, default seeder
- `Web` — Admin CRUD + `PopupHost` ViewComponent

## Public API

- Layout injects `@await Component.InvokeAsync("PopupHost")` (only active popups matching current path)
- Manual open: `data-popup-open="slug-or-id"` or `MirkaPopup.open('slug')`
- JS API: `MirkaPopup.open`, `close`, `toggle`, `isOpen`
- Assets: `wwwroot/site/css/popup.css`, `wwwroot/site/js/popup.js`

## Forms

Optional `FormId` embeds existing Forms via `FormEmbed` ViewComponent. Submit from inside a popup uses JSON Accept headers so validation errors stay in the dialog.

## Extensibility

- `TriggerType` is a string (`manual` / `timer` / `scroll`) plus optional `TriggerConfigJson`
- `ExtensionSettingsJson` reserved for scheduling, animation, analytics, A/B, etc.
