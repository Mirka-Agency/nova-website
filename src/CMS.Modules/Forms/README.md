# Forms module

Theme-independent form engine with admin builder, public schema contract, and submission pipeline.

## Architecture

| Layer | Responsibility |
|-------|----------------|
| **Core** (`Domain` / `Application` / `Infrastructure`) | Form definitions, `FormVersion.SchemaJson`, field type registry, actions, anti-spam, submissions |
| **Web (Admin)** | Builder UI (tabs), responses inbox, CSV export |
| **Web (Public)** | Render + submit; theme consumes the public contract JSON |
| **Theme** | Must use public contract / type ids only — no secrets, no action configs |

## System forms (code seed)

Define stable forms in `SystemFormCatalog` (Application). On startup `FormSystemSeeder` creates missing forms/fields.

- Forms/fields get `IsSystem = true`
- Admin cannot delete system forms or system fields
- System **Key**, **Slug**, and field **Key / Type / Required / Options** are readonly
- Labels, help text, submit/actions/anti-spam remain editable
- Reference in code by `Key` via `GetPublicContractByKeyAsync` / `FormEmbed(key: "...")`

Example registration:

```csharp
public static IReadOnlyList<SystemFormDefinition> All { get; } =
[
    Contact(), // or your own SystemFormDefinition(...)
];
```

## SchemaJson as source of truth

`FormVersion.SchemaJson` holds the full form document:

- `fields` — typed field definitions (`FormFieldTypeIds`)
- `submitBehavior` — `message` | `redirect` | `page`
- `actions` — `email_notification`, `auto_reply`, `webhook`
- `antiSpam` — provider + config
- `settings` — e.g. `submitButtonText`

Admin saves write SchemaJson via `FormSchemaLegacyMapper.ApplySaveOverrides` (rebuilds actions completely from the save command). Field table rows stay synced for admin/editing; settings/actions live on the schema.

## Public contract endpoints

Theme-safe JSON (no secrets / no action configs):

| Endpoint | Lookup |
|----------|--------|
| `GET /forms/{slug}/schema` | By slug |
| `GET /forms/key/{key}/schema` | By stable key |
| `GET /forms/by-id/{id}/schema` | By form id |

Mapped by `FormPublicContractMapper` from published schema (+ form meta). Public HTML routes use the same contract (`GetPublicContract*`).

## Submit behavior

| Type | Result |
|------|--------|
| `message` | Show success message (no redirect) |
| `redirect` | Redirect to absolute `http(s)` URL or site-relative path |
| `page` | Redirect to a **site-relative path** (e.g. `/thanks`) — stored as `submitBehavior.url` until a Pages CMS exists |

## Anti-spam (public)

| Provider | Public UI |
|----------|-----------|
| `honeypot` | Hidden field (default) |
| `simple_captcha` | Text answer field |
| `turnstile` / `recaptcha` / `hcaptcha` | Official widget when global `Forms:AntiSpam:*:SiteKey` is set; server verifies with `Forms:AntiSpam:*:SecretKey` from env (missing secret **rejects**) |

## Webhook

POST envelope: `event`, `form`, `submission`, `data`, `context`. Optional HMAC via action `config.secretKey` → header `X-Form-Signature: sha256=<hex>`.

## Version history

Publishing / saving a **published** form with schema changes archives the previous published snapshot and creates a new published version (+ draft copy). Advanced tab lists versions and can restore settings/actions into the current draft.

## Field options

Admin `OptionsCsv` supports `value:label` (or `value=label`) per token, e.g. `yes:بله|no:خیر`. Schema/public options already expose distinct `value` / `label`.

## Conditional visibility

Fields may declare `visibility.mode` (`all` / `any`) and multiple `conditions[]` (field / operator / value). Admin FieldForm supports add/remove condition rows; public JS hides/shows inputs; submit skips hidden required fields.
## Dual-write status

| Surface | Status |
|---------|--------|
| **Form settings columns** (`SuccessMessage`, `RedirectUrl`, `SubmitButtonText`, notify/auto-reply, `EnableCaptcha`, …) | **Dropped**. SchemaJson is the only store for submit/actions/antiSpam/settings. |
| **SubmissionValues** | **Dropped**. `DataJson` is the only store for submission field data. |

## Field type ids

Stable snake_case ids in SchemaJson / public contract (`FormFieldTypeIds`):

| Id | Notes |
|----|-------|
| `text`, `textarea`, `email`, `tel`, `number`, `url` | Basic inputs |
| `select`, `radio`, `checkbox`, `checkbox_group` | Choice |
| `date`, `time`, `datetime` | Temporal |
| `file`, `hidden`, `consent` | Special inputs |
| `heading`, `paragraph`, `divider` | Layout (non-input) |
| `captcha` | Legacy — prefer `antiSpam.simple_captcha` |

Handlers live in the field type registry; themes should switch on these ids, not CLR enums.
