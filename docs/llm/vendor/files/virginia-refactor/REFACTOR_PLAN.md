# Virginia — Architectural Refactor & Accessibility Plan

> Audience: future Claude or any LLM picking this up. This is the **execution
> blueprint**. The follow-up artifacts are the actual files. Nothing here is
> aspirational; everything is concrete and copy-pasteable.

---

## 1. Honest diagnostic — what's actually wrong (and what isn't)

The `ContactDetail.razor` file is roughly 585 lines. That is **on the edge**,
not catastrophic. Many production Blazor pages run 800–1200 lines. The reason
to refactor is not that it crossed an arbitrary line count, but that:

1. **Six unrelated concerns share one component**: data load, edit form, photo
   upload, notes, toasts, real-time change handling. Each concern has its own
   lifecycle, error states, and UI shape. Mixing them inflates cognitive load
   when you're only trying to fix one.
2. **Two of the loops have a real bug**: the email/phone/address loops use
   index-based iteration without a `@key` on the underlying entity. When a
   user removes the middle item from a 3-item list, Blazor's diffing
   re-uses the wrong DOM nodes for the remaining inputs, causing validation
   state and focus to attach to the wrong element. This is silent today
   because items rarely get removed mid-edit, but it's wrong.
3. **CSS isolation is wasted**: `ContactDetail.razor.css` (7.8 KB) contains
   styles for buttons, banners, toasts, notes, avatars, validation messages,
   responsive breakpoints, and form rows — most of which would apply to any
   page. Scoping it all to the `ContactDetail` component means the styles
   are duplicated when you build a similar page elsewhere.
4. **Toast logic is local**: the toast stack, severity enum, auto-dismiss
   timer, and message record live inside the page. They will be needed on
   the contact list page, on the admin user management page, and on the
   change-password page. Each of those will either duplicate the code or
   reach into a sibling component.
5. **The `originId` per-circuit Guid trick is correct but undocumented**:
   it's a clever defence against the page reacting to its own writes, but
   it's invisible if you don't already know the pattern.

What is **not** wrong and should not be "fixed":

- **The service layer**. `IContactService` / `ContactService` are clean,
  testable, and instrumented. Leave them alone.
- **The EF/SQLite schema**. Confirmed off-limits by the user. No changes.
- **The toast UX**. The actual interaction (auto-dismiss info, sticky warn
  with reload button) is good. We're just relocating the code.
- **The optimistic concurrency model** (`originId` filtering of self-echoes
  + warn-with-reload on remote edit). This is well-designed. Keep it.

---

## 2. What Gemini got right, what it got wrong

Gemini's report is broadly directionally correct but has three concrete
problems I want to flag before we execute:

### Wrong: PInvoke into `shlwapi.dll` for natural sort

Gemini suggests:

```csharp
[DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
public static extern int StrCmpLogicalW(string x, string y);
```

This is a bad idea for this project:

- **Hosting is `runasp.net` on Windows IIS**, which makes the PInvoke work in
  production today. But the platform check that Gemini proposes silently
  falls back to ordinal sort on Linux — meaning Linux CI runs (GitHub
  Actions ubuntu-latest, which is your current pipeline) will produce
  **different ordering than production**. Tests that depend on order would
  pass locally and fail in CI, or vice-versa.
- **Marshalling cost** per comparison is non-trivial; on a list of 500+
  contacts that's already 500+ kernel transitions just to sort by last name.
- **The platform fork breaks determinism**. Sorting is a place where you
  want one answer, not two.

The fix is a **fully managed** natural comparer — twenty lines of code,
zero dependencies, identical results everywhere. It's included in this
plan as `Virginia/Extensions/NaturalComparer.cs`.

### Overkill: generic `ContactCollectionSection<T>` with `RenderFragment`

Gemini proposes a single generic collection component that templates the
item editor as a `RenderFragment<T>`. This is the kind of abstraction that
looks clever and adds 30% complexity for 0% benefit when you have exactly
three (3) lists. Three concrete components — `EmailsCard`, `PhonesCard`,
`AddressesCard` — are easier to navigate, easier to test, easier to give
distinct `data-testid` attributes for Playwright, and easier for the next
developer to read. We're not building a CMS.

### Premature: full state container (Fluxor, etc.)

Gemini briefly mentions state containers and stops short of recommending
one, which is correct. Don't introduce Fluxor or any other Redux-pattern
library. Blazor Server with scoped DI services is already a state container
— it's called the DI container. The `ContactChangeNotifier` you already
have is the right scope and pattern.

### Right: split into child components, code-behind for the page

This is the heart of the work and Gemini has it right. Execute on this.

### Right: accessibility is a first-class concern

Gemini is right that this matters for both humans and agents. The
accessibility checklist below is more concrete than Gemini's.

---

## 3. Target architecture

### Folder layout

```
Virginia/
├── Components/
│   ├── Pages/
│   │   ├── Contacts/                          [NEW folder]
│   │   │   ├── ContactDetail.razor             (markup only, ~120 lines)
│   │   │   ├── ContactDetail.razor.cs          (orchestration, ~180 lines)
│   │   │   ├── ContactDetail.razor.css         (page-level layout only)
│   │   │   └── Parts/
│   │   │       ├── ProfilePictureCard.razor
│   │   │       ├── ProfilePictureCard.razor.cs
│   │   │       ├── ProfilePictureCard.razor.css
│   │   │       ├── BasicInfoCard.razor
│   │   │       ├── BasicInfoCard.razor.css
│   │   │       ├── EmailsCard.razor
│   │   │       ├── EmailsCard.razor.css
│   │   │       ├── PhonesCard.razor
│   │   │       ├── PhonesCard.razor.css
│   │   │       ├── AddressesCard.razor
│   │   │       ├── AddressesCard.razor.css
│   │   │       ├── NotesCard.razor
│   │   │       ├── NotesCard.razor.cs
│   │   │       ├── NotesCard.razor.css
│   │   │       ├── ContactActions.razor
│   │   │       └── ContactActions.razor.css
│   │   └── ContactList.razor                   (unchanged for now)
│   └── Shared/                                 [NEW folder]
│       ├── ToastStack.razor
│       ├── ToastStack.razor.cs
│       └── ToastStack.razor.css
├── Extensions/                                 [NEW folder]
│   └── NaturalComparer.cs
└── Services/
    ├── IToastService.cs                        [NEW]
    └── ToastService.cs                         [NEW]
```

The old `Virginia/Components/Pages/ContactDetail.razor` and its `.razor.css`
are deleted. Routes are preserved because the new file declares the same
`@page` directives.

### Component responsibilities

| Component               | Owns                                                  | Receives                                  | Emits                                |
|------------------------|-------------------------------------------------------|-------------------------------------------|--------------------------------------|
| `ContactDetail` (page) | Routing, data load, save/delete orchestration, change subscription | route param `Id`                          | navigates on save                    |
| `ProfilePictureCard`   | Upload/remove UI, file validation, preview            | `ContactDetailDto detail`, `int photoVer` | `OnPhotoChanged` callback            |
| `BasicInfoCard`        | First/Last name inputs                                 | `ContactFormModel` (cascading)            | binds directly                       |
| `EmailsCard`           | Email list rendering, add/remove                      | `List<EmailFormModel>`                    | mutates list                         |
| `PhonesCard`           | Phone list rendering, add/remove                      | `List<PhoneFormModel>`                    | mutates list                         |
| `AddressesCard`        | Address list rendering, add/remove                    | `List<AddressFormModel>`                  | mutates list                         |
| `NotesCard`            | Notes display, add note form                          | `ContactDetailDto detail`, `int contactId` | `OnNoteAdded` callback              |
| `ContactActions`       | Save / Delete (with confirm) / Cancel buttons         | `bool saving`, `bool isNew`               | `OnSave`, `OnDelete`, `OnCancel`     |
| `ToastStack` (shared)  | Renders + manages live toast list                     | nothing — pulls from `IToastService`      | dismisses through service            |

### State and communication rules

1. **Form model passed by reference**, not by `EventCallback`. The
   `ContactFormModel` is a mutable class. Child components bind directly
   to it. This is fine because:
   - We have one `EditForm` at the page level. Child inputs use
     `<InputText @bind-Value="...">` which participates in the page's
     `EditContext` automatically.
   - The page is the only thing that decides when to call save. Children
     just mutate their slice of the model.
2. **Cascading `EditContext`** for validation. The page provides it; the
   child cards' `InputText` components pick it up automatically through
   `[CascadingParameter]` inside Blazor's built-in input components.
3. **Toasts via scoped `IToastService`**. Any component on the page can
   call `ToastService.Show(...)`. The `ToastStack` component subscribes
   to a `Changed` event and re-renders. The service is registered
   `AddScoped<IToastService, ToastService>()` — in Blazor Server, scoped
   = per circuit, which is the right lifetime.
4. **No EventCallback chain for trivial mutations**. Adding an email is
   `model.Emails.Add(new())`. Don't wrap it in a callback that goes up
   one level and comes back down. That's ceremony.

---

## 4. Accessibility — humans + agents

This is two related but distinct concerns:

- **Human accessibility (WCAG 2.2 AA)**: screen readers, keyboard
  navigation, colour contrast, focus visibility, reduced motion.
- **Agent reliability (Playwright / Claude in Chrome)**: stable selectors,
  semantic roles, labels that match accessible names, predictable focus
  order.

The good news is **they push in the same direction**. Almost everything
that makes a page reliable for Playwright also makes it usable with a
screen reader, because Playwright's recommended locator strategy is
`getByRole`, `getByLabel`, `getByText` — i.e. exactly the accessibility
tree.

### Concrete rules

#### A. Every interactive element has an accessible name

- Inputs: `<label for="...">` matching `id`. We already do this in most
  places. Fix: in the email/phone/address loops, the `<label>` elements
  have **no `for`** attribute. They need `for="email-addr-{id}"` etc.
- Icon-only buttons: `aria-label`. The `✕` remove buttons need
  `aria-label="Remove email"` (already present), but the variant the page
  uses is sometimes missing it.
- The "Back to list" button is fine (text content). The dismiss `✕` on
  toasts is fine (`aria-label="Dismiss"`).

#### B. Stable, semantic selectors for Playwright

The rule:

> **Prefer `getByRole` + accessible name. Use `data-testid` only as a
> last resort for elements where neither role nor stable text exists.**

This means: instead of slapping `data-testid` on everything (which Gemini
implies), we make the accessible name correct and let Playwright use that.
Example, save button:

```html
<button type="submit" class="btn btn-primary">Save</button>
```

Playwright: `page.getByRole('button', { name: 'Save' })`. No testid needed.

`data-testid` is reserved for cases where:
- The text changes (`Save` → `Saving...`) and you need to find the same
  button across states. Use `data-testid="save-button"`.
- There are multiple elements with the same role+name on the page.
- The element has no text and only a generic icon.

#### C. Landmark structure

Every page must have a single `<main>` element wrapping the page content.
The `ContactDetail` page should look like:

```html
<main aria-labelledby="page-title">
  <h1 id="page-title">Edit Contact</h1>
  <nav aria-label="Detail navigation"> ... </nav>
  <form> ... </form>
</main>
```

This is a small change to `MainLayout.razor` (wrap children in `<main>`)
plus adding `<h1>` elements to each page. Right now `ContactDetail.razor`
uses `<PageTitle>` (browser tab) but has no in-page `<h1>`. That's a
WCAG failure and a Playwright pain (no obvious anchor for the page).

#### D. Focus management

- After a form save, focus should move to the success banner so screen
  readers announce it. Use `ElementReference` + JS `focus()` or just rely
  on `role="status"` + `aria-live="polite"` (we already do this).
- After clicking "Delete" → "Are you sure?", focus must move to the
  "Yes, delete" button so a keyboard user doesn't have to tab to it.
- Modal-style confirmations (delete confirmation) should be wrapped in
  a `role="group"` with `aria-label="Confirm deletion"` so it's a single
  navigable unit.

#### E. Live regions

- `aria-live="polite"` for status: loading, saved successfully, toast info.
- `role="alert"` (implicit `aria-live="assertive"`) for errors only —
  errors interrupt; success messages don't.
- We already do this correctly in most places. Audit and fix outliers.

#### F. Form validation

- `<ValidationMessage>` outputs a `<div>` with `class="validation-message"`.
  This is not announced by screen readers because it's not associated with
  the input. Fix: each input that has a validation message should reference
  it via `aria-describedby` pointing to an `id` on the message element.
  Blazor doesn't do this automatically. We add it manually in the cards.

#### G. Reduced motion

The toast animation uses `@keyframes toast-in`. Add a `@media (prefers-reduced-motion: reduce)` rule that disables it.

#### H. Colour contrast audit

The existing palette uses:
- `#c62828` on `#ffeaea` (error text/bg) — 5.95:1 ✓ (passes AA)
- `#1565c0` on `#e7f3fb` (info toast) — 6.42:1 ✓
- `#b26a00` on `#fff7e6` (warn toast) — 4.78:1 ✓ (just over AA 4.5:1)
- `#999` on `#f8f9fa` (note date) — 2.85:1 ✗ **fails AA**
- `#aaa` on `#fff` (hint text) — 2.32:1 ✗ **fails AA**

Fix: change `#999` → `#595959` (7.0:1) and `#aaa` → `#595959` (7.0:1)
in the new card-level CSS.

#### I. Keyboard navigation on the contact list rows

`ContactList.razor` uses `<tr tabindex="0">` with click + keydown handlers.
This works for keyboard but is not great for screen readers, because a
`<tr>` doesn't have a button role. Better: keep the row but place an
actual `<a>` link inside one cell as the primary action, and remove the
tabindex/onclick from the row. The row visual highlight on hover stays.

Defer this to a separate pass — it's a refactor of `ContactList`, not
`ContactDetail`. Mentioned here for completeness.

---

## 5. Natural sort — answer and scope

The question was: "implement natural sort everywhere?"

**Answer: no, not everywhere.** Natural sort makes sense for human-readable
lists where strings contain embedded numbers — house numbers, file names,
"Building 2" / "Building 10". For this address book it matters in exactly
one place today:

- The contact list's primary sort (last name, then first name). A user
  named "Smith 2" should appear before "Smith 10" if such a thing exists.
  Realistically, last names don't have numbers in them. **Natural sort is
  not actually needed right now.**

We add the `NaturalComparer` as a reusable utility for future use (street
addresses on a same-city filter, for instance) but we **do not change
`ContactService.ListAsync`** to use it. That ordering is done in SQL via
`OrderBy(c => c.LastName)` and pulling it client-side just to sort would
require fetching everything, which breaks pagination.

If natural sort becomes important for some future view, the comparer is
there.

---

## 6. Execution checklist

Files to **create** (full contents provided as separate artifacts):

- [ ] `Virginia/Extensions/NaturalComparer.cs`
- [ ] `Virginia/Services/IToastService.cs`
- [ ] `Virginia/Services/ToastService.cs`
- [ ] `Virginia/Components/Shared/ToastStack.razor`
- [ ] `Virginia/Components/Shared/ToastStack.razor.cs`
- [ ] `Virginia/Components/Shared/ToastStack.razor.css`
- [ ] `Virginia/Components/Pages/Contacts/ContactDetail.razor`
- [ ] `Virginia/Components/Pages/Contacts/ContactDetail.razor.cs`
- [ ] `Virginia/Components/Pages/Contacts/ContactDetail.razor.css`
- [ ] `Virginia/Components/Pages/Contacts/Parts/ProfilePictureCard.razor`
- [ ] `Virginia/Components/Pages/Contacts/Parts/ProfilePictureCard.razor.cs`
- [ ] `Virginia/Components/Pages/Contacts/Parts/ProfilePictureCard.razor.css`
- [ ] `Virginia/Components/Pages/Contacts/Parts/BasicInfoCard.razor`
- [ ] `Virginia/Components/Pages/Contacts/Parts/BasicInfoCard.razor.css`
- [ ] `Virginia/Components/Pages/Contacts/Parts/EmailsCard.razor`
- [ ] `Virginia/Components/Pages/Contacts/Parts/EmailsCard.razor.css`
- [ ] `Virginia/Components/Pages/Contacts/Parts/PhonesCard.razor`
- [ ] `Virginia/Components/Pages/Contacts/Parts/PhonesCard.razor.css`
- [ ] `Virginia/Components/Pages/Contacts/Parts/AddressesCard.razor`
- [ ] `Virginia/Components/Pages/Contacts/Parts/AddressesCard.razor.css`
- [ ] `Virginia/Components/Pages/Contacts/Parts/NotesCard.razor`
- [ ] `Virginia/Components/Pages/Contacts/Parts/NotesCard.razor.cs`
- [ ] `Virginia/Components/Pages/Contacts/Parts/NotesCard.razor.css`
- [ ] `Virginia/Components/Pages/Contacts/Parts/ContactActions.razor`
- [ ] `Virginia/Components/Pages/Contacts/Parts/ContactActions.razor.css`
- [ ] `Virginia.Tests/NaturalComparerTests.cs`
- [ ] `Virginia.Tests/ToastServiceTests.cs`

Files to **modify** (full contents provided):

- [ ] `Virginia/Components/_Imports.razor` (add new namespaces)
- [ ] `Virginia/Components/Layout/MainLayout.razor` (wrap in `<main>`)
- [ ] `Virginia/Program.cs` (register `IToastService`)
- [ ] `Virginia/wwwroot/app.css` (shared focus + reduced motion)

Files to **delete**:

- [ ] `Virginia/Components/Pages/ContactDetail.razor`
- [ ] `Virginia/Components/Pages/ContactDetail.razor.css`

After deployment:

1. Build with warnings-as-errors. Should pass.
2. Run xUnit suite. Existing tests unchanged; two new test classes added.
3. Manual smoke: open a contact, edit a name, save. Open a second tab on
   the same contact, edit from there, watch toast in tab one. Add a note,
   delete the contact.
4. Run an axe-core scan in the browser dev tools on `/contacts/1`. Should
   show zero violations.
5. Optional: Playwright probe with `page.getByRole('button', { name: 'Save' })`
   and confirm it resolves.

---

## 7. Things deliberately not done

To keep the change reviewable:

- **No new packages.** Per user instruction.
- **No schema changes.** Per user instruction.
- **`ContactList.razor` is unchanged.** A separate, equivalent pass on the
  list page is recommended later (move bulk actions into a card, replace
  row-as-button with proper anchor links, add the same `<main>` and `<h1>`
  treatment).
- **No new auth changes.** The `[Authorize(Policy = "Approved")]` attribute
  stays on the page.
- **`FakeContactGenerator.cs` is unchanged.** It's already in good shape.
- **No client-side validation library swap.** `DataAnnotations` + EditForm
  is the right tool here.
- **No virtualisation.** Page size is capped at 100, virtualisation is
  not needed at this scale.

---

## 8. After this lands

Future work, in priority order:

1. **`ContactList.razor` refactor** — same pattern. Split bulk-actions
   into its own component. Replace `<tr tabindex>` with proper anchors.
2. **Server-side natural sort** — only if a real use case appears.
3. **An end-to-end Playwright suite** — `/tests/e2e/` with three or four
   smoke scenarios (login, create contact, edit contact, delete contact).
4. **An `aria-busy` toggle on the form during save** — small a11y nicety.
5. **A focus-trap helper** for confirm-delete and any future modals.
