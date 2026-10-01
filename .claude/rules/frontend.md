# Frontend Rules — ASP.NET Core MVC (Razor) + Vanilla JavaScript

> Standards for the approved frontend stack. See [`tech-stack.md`](tech-stack.md) for the full stack rationale. **No JavaScript application frameworks, no TypeScript, no mandatory bundler** — this is a hard project rule. **Bootstrap 5.3.8 (CSS + JS bundle)** is the official CSS framework and component library; it is not an application JS framework.

---

## Stack Quick Reference

| Concern | Choice |
|---------|--------|
| Presentation | Razor Views (`.cshtml`), Tag Helpers, Partial Views, View Components |
| Interactivity | Vanilla JavaScript (ES2022+), native ES modules (`<script type="module">`); **Bootstrap 5.3.8 JS bundle** for its documented components |
| Language | JavaScript only (JSDoc allowed for documentation) |
| HTTP from the browser | Fetch API |
| Styling | **Bootstrap 5.3.8** (static files in `wwwroot/lib/bootstrap/`; no CDN, no LibMan, no Sass) + minimal custom CSS: `base.css` (theme via `--bs-*` variables), `components/`, `pages/` |
| Build | None. Files are served from `wwwroot` (cache-busting via `asp-append-version`) |
| Validation | Server-side (DataAnnotations / FluentValidation) + HTML5 constraint validation; MVC client validation via jquery-validation-unobtrusive |
| jQuery | 3.7.1 + jquery-validation + jquery-validation-unobtrusive — **only** for MVC form validation and legacy plugins already in the project |
| Auth | ASP.NET Core cookie auth + antiforgery tokens |
| Testing | MSTest + Playwright for .NET |

**Forbidden:** Blazor, React, Next.js, Vue, Angular, Svelte, Vite, Webpack, TypeScript, any CSS framework other than Bootstrap, and any CSS preprocessor. Do not propose or add them — not even "just for one page".

---

## Core Principles

1. **Server-rendered first.** Razor renders the complete page. The server is the source of truth for data and validation.
2. **Progressive enhancement.** Every feature MUST work with JavaScript disabled (links navigate, forms POST). JavaScript only *enriches*: inline validation, partial updates, dialogs, filtering.
3. **No build step.** Write browser-native code. If a bundler ever becomes necessary, use only what ASP.NET Core provides natively, and record an ADR first.
4. **Small, isolated modules.** No global variables, no inline `<script>` blocks with logic, no `eval`.
5. **Accessible by default** (WCAG 2.1 AA — see §Accessibility).

---

## Razor Views

### Layers

| Piece | Use for |
|-------|---------|
| View (`Views/<Controller>/<Action>.cshtml`) | One page; strongly-typed `@model` (a ViewModel, never an EF entity) |
| Layout (`_Layout.cshtml`) | Shared shell: `<head>`, header, nav, footer, `@RenderSectionAsync("Scripts", required: false)` |
| Partial View | Reusable markup without logic |
| View Component | Reusable markup **with** its own data access/logic (cart summary, menu) |
| Tag Helper | Custom HTML behavior (`<my-alert>`) and built-ins: `asp-controller`, `asp-action`, `asp-for`, `asp-validation-for`, `asp-append-version` |

### Rules

- Use **Tag Helpers** instead of `Html.*` helpers and hardcoded URLs (`asp-action`, not `href="/products/index"`).
- Views contain **presentation only** — no queries, no business rules. Logic goes to the controller, service or View Component.
- Encode output by default (Razor does). `@Html.Raw` only for content sanitized on the server; never for user input.
- Pass server data to JS through `data-*` attributes or a `<script type="application/json">` block — never by string-building JavaScript.
- Every page sets a meaningful `<title>`, one `<h1>`, and `lang` on `<html>` (`pt-BR`).

### Naming

| Element | Convention | Example |
|---------|------------|---------|
| View / Partial files | PascalCase; partials prefixed `_` | `Index.cshtml`, `_ProductCard.cshtml` |
| ViewModels | `{Feature}ViewModel` | `ProductListViewModel` |
| View Components | `{Name}ViewComponent` | `CartSummaryViewComponent` |
| Tag Helpers | `{Name}TagHelper` | `AlertTagHelper` |

---

## JavaScript (ES2022+, native modules)

### Loading

```html
@section Scripts {
    <script type="module" src="~/js/pages/products-index.js" asp-append-version="true"></script>
}
```

- **One entry module per view** in `wwwroot/js/pages/`, loaded only by that view. Shared code lives in `wwwroot/js/modules/` and is imported with relative paths and the `.js` extension.
- `type="module"` is deferred and strict by default — no `defer` or `"use strict"` needed.
- Use `const`/`let` (never `var`), `async`/`await`, optional chaining, `structuredClone`, `Array.prototype.at`, etc. Do not use features without support in current evergreen browsers.

### Third-party JavaScript

**Bootstrap JS (`bootstrap.bundle.min.js`, includes Popper)** — permitted for Bootstrap's documented components: modal, tooltip, dropdown, offcanvas, collapse, alert, tab, toast, popover, carousel.

- Prefer the **declarative API** (`data-bs-toggle`, `data-bs-target`, …) — no JavaScript to write. When the API is needed, use `bootstrap.Modal.getOrCreateInstance(element)` and friends **inside a page module**; never patch or copy Bootstrap's source.
- Tooltips and popovers are opt-in: initialize them explicitly in the page module.
- **Progressive enhancement still applies:** the main navigation must stay reachable without JavaScript (e.g. `navbar-expand` at the needed breakpoint or a fallback link); every modal has an equivalent page or link (`<a href="...">` that Bootstrap enhances); tooltips never carry essential information.
- Where a native element fully solves the need (`<details>`, `<dialog>`), it may be preferred but is not mandatory.

**jQuery 3.7.1 + jquery-validation + jquery-validation-unobtrusive** — permitted **only** for (a) MVC client-side form validation generated by `asp-validation-for`, and (b) legacy plugins that already depend on it. **New application code uses vanilla JavaScript in ES modules.** Do not add jQuery plugins without an ADR. Bootstrap 5 does not need jQuery.

### Hooking into the DOM

Select by **`data-*` attributes**, not by CSS classes or ids used for styling (Bootstrap's own `data-bs-*` attributes belong to Bootstrap; use `data-action`, `data-product-id`, … for your own hooks):

```html
<button type="button" data-action="add-to-cart" data-product-id="@Model.Id">Add</button>
```

```js
// wwwroot/js/pages/products-index.js
import { addToCart } from "../modules/cart.js";

document.addEventListener("click", async (event) => {
  const button = event.target.closest("[data-action='add-to-cart']");
  if (!button) return;
  await addToCart(button.dataset.productId);
});
```

Prefer **event delegation** on a stable container. Always feature-detect (`if ("IntersectionObserver" in window)`) and fail silently to the non-JS behavior.

### Module structure

```js
// wwwroot/js/modules/cart.js — one responsibility, named exports, no globals
import { apiFetch } from "./api.js";

/**
 * Adds a product to the cart.
 * @param {string} productId
 * @returns {Promise<{ itemCount: number }>}
 */
export function addToCart(productId) {
  return apiFetch("/api/v1/cart/items", { method: "POST", body: { productId } });
}
```

- Named exports; avoid default exports.
- No inline event handlers (`onclick="..."`) and no inline scripts → keeps a strict Content-Security-Policy possible.
- Document public functions with **JSDoc** (`@param`, `@returns`); this is documentation only — no TypeScript checking.

### Naming

| Element | Convention | Example |
|---------|------------|---------|
| JS files | kebab-case | `products-index.js`, `cart.js` |
| Functions / variables | camelCase, verb first for functions | `addToCart`, `updateBadge` |
| Constants | UPPER_SNAKE_CASE | `MAX_QUANTITY` |
| Booleans | `is/has/can` prefix | `isLoading`, `hasError` |
| `data-*` hooks | kebab-case | `data-action="add-to-cart"` |
| Classes (rare) | PascalCase | `Toast` |

---

## Fetch API

One shared helper handles JSON, the antiforgery token, cancellation and the backend `ProblemDetails` contract.

```js
// wwwroot/js/modules/api.js
export class ApiError extends Error {
  constructor(problem) {
    super(problem.detail ?? problem.title ?? "Request failed");
    this.status = problem.status;
    this.code = problem.code;
    this.errors = problem.errors ?? {};
    this.traceId = problem.traceId;
  }
}

const antiforgeryToken = () =>
  document.querySelector("meta[name='request-verification-token']")?.content;

/**
 * @param {string} url
 * @param {{ method?: string, body?: object, signal?: AbortSignal }} [options]
 */
export async function apiFetch(url, { method = "GET", body, signal } = {}) {
  const headers = { Accept: "application/json" };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (method !== "GET") headers["RequestVerificationToken"] = antiforgeryToken();

  const response = await fetch(url, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    credentials: "same-origin",
    signal,
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ status: response.status }));
    throw new ApiError(problem);
  }
  return response.status === 204 ? null : response.json();
}
```

Rules:

- **Never** put secrets or tokens in client code. Calls needing server-only credentials go through a controller action.
- Non-GET requests MUST send the antiforgery token (`RequestVerificationToken`).
- Use `AbortController` for searches/typeahead and when navigating away; ignore `AbortError`.
- Always handle failure: show a message in an `aria-live` region and leave the page usable.
- Prefer requesting **HTML fragments** (partial views) for UI updates when the markup is already in Razor; use JSON for data.

---

## Forms & Validation

- Build forms with `<form asp-action="..." method="post">`, `asp-for`, `asp-validation-for` and `@Html.AntiForgeryToken()` (automatic with `asp-action`).
- **Server validation is authoritative** (DataAnnotations or FluentValidation + `ModelState`). Client-side checks are a convenience: the MVC unobtrusive validation (jquery-validation) stays for `asp-validation-for`; custom checks use the HTML5 Constraint Validation API in vanilla JS.
- Style forms with Bootstrap: `form-label`, `form-control`, `form-select`, `form-check`, `is-invalid`, `invalid-feedback`.
- Progressive enhancement: the form works with a plain POST/redirect/GET. JS may intercept `submit` to send it with `fetch` and show inline errors — but must fall back to normal submission if JS fails.
- Use the HTML5 Constraint Validation API (`required`, `type`, `pattern`, `setCustomValidity`) before writing custom code.
- Every input has a `<label>`; errors are linked with `aria-describedby` and announced (`role="alert"`).

```js
form.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!form.reportValidity()) return;
  try {
    await apiFetch(form.action, { method: "POST", body: Object.fromEntries(new FormData(form)) });
    form.dispatchEvent(new CustomEvent("saved", { bubbles: true }));
  } catch (error) {
    if (error instanceof ApiError) showFieldErrors(form, error.errors);
    else form.submit(); // fall back to the server-rendered flow
  }
});
```

---

## Styling — Bootstrap 5.3.8 + minimal custom CSS

**Bootstrap 5.3.8 is the official CSS framework** (CSS + JS bundle), delivered as **static files in `wwwroot/lib/bootstrap/`** — version pinned, **no CDN, no LibMan, no bundler, no Sass**, compatible with a strict CSP. Do not edit vendor files.

```
wwwroot/
├── lib/
│   ├── bootstrap/dist/{css,js}/   # Bootstrap 5.3.8 (vendor)
│   └── jquery*/                   # jQuery 3.7.1 + validation (see §JavaScript → Third-party JavaScript)
└── css/
    ├── base.css          # theme: --bs-* overrides, app tokens (--app-*), typography tweaks
    ├── components/       # reusable custom pieces Bootstrap does not provide: rating.css …
    └── pages/            # one file per view, only when Bootstrap is not enough: products-index.css
```

- **Prefer Bootstrap's own classes** for everything it covers: grid (`container`, `row`, `col-*`), **utilities** (spacing `m-*`/`p-*`/`gap-*`, typography `fs-*`/`fw-*`/`text-*`, flex, display, colors, borders), forms, buttons, cards, alerts, navbar, tables, badges, pagination, breadcrumbs.
- **Custom CSS only when Bootstrap does not solve it.** Check the Bootstrap docs first; one-off spacing/alignment is a utility class, not a new rule.
- **Theme with CSS variables, no rebuild.** Override the `--bs-*` variables in `:root` (or `[data-bs-theme]`) in `base.css`: `--bs-primary`, `--bs-primary-rgb`, `--bs-body-font-family`, `--bs-border-radius`, `--bs-link-color`, … Component classes such as `.btn-primary` define their **own `--bs-btn-*` variables** (they do not read `--bs-primary`), so override those too when re-coloring a component. Define `--app-*` custom properties only for values Bootstrap has no variable for.
- **One custom CSS file per view** (`pages/<controller>-<action>.css`), loaded from the view's `Styles` section; `bootstrap.min.css` and `base.css` (plus needed `components/*.css`) come from the layout, in that order.
- **Naming — BEM only for custom CSS** (`.product-card`, `.product-card__title`, `.product-card--featured`). Never rename or BEM-ify Bootstrap classes, and do not redefine Bootstrap classes globally (`.btn { … }`): use variables or a custom modifier class. Scope page styles under a page class on `<main>` (e.g. `.products-index`).
- Responsive layout uses Bootstrap's mobile-first breakpoints (`sm` 576 · `md` 768 · `lg` 992 · `xl` 1200 · `xxl` 1400) through grid/utility classes; custom media queries reuse the same breakpoints.
- Do not style by `id`; avoid `!important` (Bootstrap's own utilities are the exception).
- Respect `prefers-reduced-motion`; use Bootstrap 5.3 color modes (`data-bs-theme`) if a dark theme is needed.
- Accessibility with Bootstrap: keep the documented markup and ARIA attributes of each component, use `.visually-hidden` / `.visually-hidden-focusable` for screen-reader text and the skip link, and **re-check contrast (≥ 4.5:1) whenever a theme color is overridden** — Bootstrap's defaults do not guarantee it for custom palettes.
- Do not add any other CSS framework or preprocessor.

---

## State Management

| State | Where it lives |
|-------|----------------|
| Server data (products, orders, user) | Server — rendered by Razor or fetched via `apiFetch`. **Never** duplicate it in long-lived client state |
| Ephemeral UI (open/closed, selected tab) | The DOM: `hidden`, `aria-expanded`, `data-state`, `<details>`, `<dialog>` |
| Shared client state (cart badge count) | A small module with getter/setter + `CustomEvent`s; persist to `localStorage` only for non-sensitive preferences |

Never store tokens, personal data or anything sensitive in `localStorage`/`sessionStorage`.

---

## Accessibility (WCAG 2.1 AA)

Mandatory requirement, verified at `/test` and `/verify`.

- **Semantic HTML first** — `<button>`, `<a>`, `<nav>`, `<main>`, `<dialog>`, `<details>`; never `<div onclick>`. Use landmarks and a logical heading order.
- All form inputs have an associated `<label>`; group related inputs with `<fieldset>`/`<legend>`.
- Everything is keyboard-operable with a **visible focus style** (never `outline: none` without a replacement). Provide a "skip to content" link.
- Icon-only buttons need `aria-label`; decorative images use `alt=""`.
- Bootstrap components (modal, dropdown, offcanvas, collapse, tabs…) keep their documented ARIA attributes and focus behavior — never strip `aria-*`/`data-bs-*` attributes or replace them with `div`-based imitations.
- Color contrast ≥ 4.5:1 (normal text), ≥ 3:1 (large text and UI components). Never convey information by color alone.
- Dynamic updates: `aria-live="polite"` for status, `role="alert"` for errors. After JS navigation or opening a dialog, **move focus** deliberately and restore it on close.
- Touch targets ≥ 24×24 CSS px; content reflows at 320 px width and 200 % zoom.
- Test with keyboard only and a screen reader (NVDA). Automated: axe-core via Playwright.

See [`accessibility-checklist.md`](../references/accessibility-checklist.md) for the full WCAG checklist.

---

## Performance

### Core Web Vitals targets

| Metric | Good |
|--------|------|
| LCP (Largest Contentful Paint) | < 2.5s |
| INP (Interaction to Next Paint) | < 200ms |
| CLS (Cumulative Layout Shift) | < 0.1 |

### Required practices

- Images: explicit `width`/`height`, `loading="lazy"` below the fold, modern formats (WebP/AVIF), `srcset`/`sizes` for responsive images.
- Scripts are modules (deferred); load page-specific code only on that page. Use dynamic `import()` for heavy code needed on demand.
- Enable response compression and static-file caching (`asp-append-version` + long `Cache-Control`).
- Avoid layout thrash (batch DOM reads/writes) and request waterfalls (`Promise.all` for independent calls).
- Debounce input-driven requests; cancel stale ones with `AbortController`.
- Bootstrap CSS/JS are loaded once, from the layout; page-specific CSS/JS are loaded only by the view that needs them.

---

## Error Handling

- Server pages: friendly `Error.cshtml` and status-code pages (`UseStatusCodePagesWithReExecute`); never expose stack traces outside Development.
- Client: catch failures from `apiFetch`, show the message in an `aria-live` region, and keep the page usable. Log unexpected errors to the console in Development only.
- Consume the backend `ProblemDetails` contract (RFC 7807): `status`, `title`, `detail`, `code`, `errors`, `traceId`. Show `traceId` to the user for support when present. See [`error-handling.md`](error-handling.md).
- Add a global `window.addEventListener("unhandledrejection", …)` in the layout module to avoid silent failures.

---

## Security (frontend)

- No inline scripts/handlers → compatible with a strict **CSP** (`script-src 'self'`).
- Never build HTML from untrusted strings with `innerHTML`; use `textContent`, `createElement` or `<template>` cloning. If HTML from the server is inserted, it must be a Razor-rendered partial.
- Antiforgery on every state-changing request. Cookies: `HttpOnly`, `Secure`, `SameSite`.
- Follow [`security.md`](security.md) for headers, CORS and rate limiting.

---

## Testing

| Layer | Tool | What to test |
|-------|------|--------------|
| Controllers / ViewModels / services | MSTest | Logic, validation, model binding |
| Views + JavaScript behavior | Playwright for .NET (MSTest) | User-visible behavior in a real browser |
| No-JS fallback | Playwright with `JavaScriptEnabled = false` | Core journeys still work |
| Accessibility | Playwright + axe-core | No serious/critical violations |

- Query elements by **role > label > text > test id** (`GetByRole`, `GetByLabel`, `GetByText`, `GetByTestId`) — never by CSS class.
- Cover critical journeys end-to-end, with and without JavaScript. See [`testing.md`](testing.md).

---

## File & Folder Conventions

```
src/<Project>.Web/
├── Controllers/
├── Models/                      # ViewModels
├── ViewComponents/
├── TagHelpers/
├── Views/
│   ├── Shared/                  # _Layout.cshtml, _ValidationScriptsPartial.cshtml, partials
│   └── <Controller>/            # <Action>.cshtml
└── wwwroot/
    ├── lib/
    │   ├── bootstrap/           # Bootstrap 5.3.8 (static, vendor — do not edit)
    │   └── jquery*/             # jQuery 3.7.1 + validation (MVC validation / legacy plugins only)
    ├── css/
    │   ├── base.css             # --bs-* theme overrides + --app-* tokens
    │   ├── components/          # custom pieces Bootstrap does not provide
    │   └── pages/               # per-view CSS, only when Bootstrap is not enough
    ├── js/
    │   ├── modules/             # shared, reusable ES modules (api.js, cart.js …)
    │   └── pages/               # one entry module per view
    └── images/
```

---

## Checklist

- [ ] No JS application framework, no TypeScript, no bundler, no CSS framework other than Bootstrap 5.3.8 introduced
- [ ] Page works with JavaScript disabled (progressive enhancement)
- [ ] Strongly-typed ViewModel; no logic or queries in the View
- [ ] Tag Helpers used; no hardcoded URLs; `@Html.Raw` never on user input
- [ ] Scripts are ES modules, one entry per view, no globals, no inline handlers/scripts
- [ ] Server-side validation is authoritative; antiforgery on every non-GET request
- [ ] `apiFetch` handles `ProblemDetails`; failures announced via `aria-live`
- [ ] Bootstrap classes/utilities used before custom CSS; theme only via `--bs-*` overrides in `base.css`; BEM only for custom CSS; no `!important`
- [ ] Bootstrap JS only for documented components (prefer `data-bs-*`); navbar, modals and tooltips leave no essential content or action unreachable without JS
- [ ] jQuery only for MVC validation / legacy plugins; new code is vanilla ES modules
- [ ] Keyboard navigation and visible focus on every interactive element
- [ ] Contrast ≥ 4.5:1; labels, alt text and landmarks present; axe-core passes
- [ ] Images sized and lazy-loaded; Core Web Vitals targets met
- [ ] No sensitive data in `localStorage`/`sessionStorage`; no untrusted `innerHTML`
- [ ] Playwright journeys pass with and without JavaScript
- [ ] Responsive verified — no horizontal overflow at each declared breakpoint (measured at `/verify` Phase 4)
