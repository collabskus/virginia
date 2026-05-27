# Virginia Refactor — Apply Instructions

## TL;DR

1. **Delete** these two files from your repo:
   - `Virginia/Components/Pages/ContactDetail.razor`
   - `Virginia/Components/Pages/ContactDetail.razor.css`

2. **Copy** every file from this bundle into your repo at the same
   relative path (under your `Virginia/` solution root). The new
   `ContactDetail.razor` lives at
   `Virginia/Components/Pages/Contacts/ContactDetail.razor` — the
   `@page` directives in it preserve the existing routes
   (`/contacts/new` and `/contacts/{Id:int}`).

3. **Build**. Warnings-as-errors should pass.

4. **Run tests**. The two new test classes (`NaturalComparerTests`,
   `ToastServiceTests`) should pass alongside the existing suite.
   Existing test files (`ContactServiceTests`, `DtoMappingTests`,
   `FormValidationTests`) are unchanged.

## File map

### Created (33 files)

```
Virginia/
├── Extensions/
│   └── NaturalComparer.cs                     [NEW]
├── Services/
│   ├── IToastService.cs                       [NEW]
│   └── ToastService.cs                        [NEW]
├── Components/
│   ├── Shared/
│   │   ├── ToastStack.razor                   [NEW]
│   │   ├── ToastStack.razor.cs                [NEW]
│   │   └── ToastStack.razor.css               [NEW]
│   └── Pages/
│       └── Contacts/
│           ├── ContactDetail.razor            [NEW — replaces old]
│           ├── ContactDetail.razor.cs         [NEW]
│           ├── ContactDetail.razor.css        [NEW]
│           └── Parts/
│               ├── ProfilePictureCard.razor   [NEW]
│               ├── ProfilePictureCard.razor.cs[NEW]
│               ├── ProfilePictureCard.razor.css[NEW]
│               ├── BasicInfoCard.razor        [NEW]
│               ├── BasicInfoCard.razor.css    [NEW]
│               ├── EmailsCard.razor           [NEW]
│               ├── EmailsCard.razor.css       [NEW]
│               ├── PhonesCard.razor           [NEW]
│               ├── PhonesCard.razor.css       [NEW]
│               ├── AddressesCard.razor        [NEW]
│               ├── AddressesCard.razor.css    [NEW]
│               ├── NotesCard.razor            [NEW]
│               ├── NotesCard.razor.cs         [NEW]
│               ├── NotesCard.razor.css        [NEW]
│               ├── ContactActions.razor       [NEW]
│               └── ContactActions.razor.css   [NEW]

Virginia.Tests/
├── NaturalComparerTests.cs                    [NEW]
└── ToastServiceTests.cs                       [NEW]
```

### Modified (4 files — full contents in this bundle)

```
Virginia/
├── Components/
│   ├── _Imports.razor                          [adds 4 namespaces]
│   └── Layout/
│       ├── MainLayout.razor                    [adds skip-link, nav landmark, focus]
│       └── MainLayout.razor.css                [a11y improvements]
├── wwwroot/
│   └── app.css                                 [shared primitives + a11y, big rewrite]
└── Program.cs                                  [adds IToastService registration]
```

### Deleted (2 files)

```
Virginia/Components/Pages/ContactDetail.razor      [REPLACED by Contacts/ version]
Virginia/Components/Pages/ContactDetail.razor.css  [REPLACED]
```

### Untouched

All other files in the repo are unchanged:
- All other pages (`ContactList`, `Login`, `Register`, `ChangePassword`,
  `UserManagement`, `Error`, `NotFound`)
- All service-layer code (`ContactService`, `ContactTelemetry`,
  `ContactChangeNotifier`, `FakeContactGenerator`, `AppClaimsPrincipalFactory`)
- All data layer (`AppDbContext`, `AppUser`, `Dtos`, `Entities`, `FormModels`)
- All existing tests (`ContactServiceTests`, `DtoMappingTests`,
  `FormValidationTests`, `TestInfrastructure`)
- CI workflow, package management, project files

## Smoke test checklist (after deploy)

- [ ] Visit `/contacts/new`. Verify page renders with `<h1>` "New Contact".
- [ ] Add basic info + one email + one phone + one address. Save.
       Should redirect to `/contacts/{newId}`.
- [ ] Edit the contact, upload a photo (any JPEG under 2 MB). The photo
       appears next to the contact's name in the avatar area.
- [ ] Add a note. It appears in the notes list with your username and
       a UTC timestamp.
- [ ] Click Delete → confirm button appears, focus moves to it
       automatically. Press Tab → focus goes to Cancel.
- [ ] Open the same contact in a second browser tab/session. Edit
       in tab one and save. Tab two should show a toast.
- [ ] Press Tab from a fresh page-load. A "Skip to main content" link
       should appear at the top-left.
- [ ] Run an axe-core scan in browser dev tools on the edit page.
       Should report zero violations.

## Playwright reference

The page exposes stable selectors via `getByRole` + accessible names
first, `data-testid` as fallback. Examples:

```ts
// Preferred — role + accessible name
await page.getByRole('button', { name: 'Save' }).click();
await page.getByLabel('First Name *').fill('Ada');
await page.getByRole('heading', { name: 'Edit Contact' });

// Fallback — testid (for state-dependent or generic elements)
await page.getByTestId('toast').first();
await page.getByTestId('confirm-delete').click();
await page.getByTestId('email-address').first().fill('ada@example.com');
```

Stable testids defined in this refactor:

- Page-level: `back-to-list`, `loading`, `not-found`, `deleted-by-other`,
  `form-error`, `form-saved`, `validation-summary`
- Toast: `toast-stack`, `toast`, `toast-reload`, `toast-dismiss`
- Profile picture: `profile-photo`, `remove-photo`, `upload-photo`,
  `photo-error`
- Basic info: `first-name`, `last-name`
- Emails: `add-email`, `emails-list`, `emails-empty`, `email-item`,
  `email-label`, `email-address`, `remove-email`
- Phones: `add-phone`, `phones-list`, `phones-empty`, `phone-item`,
  `phone-label`, `phone-number`, `remove-phone`
- Addresses: `add-address`, `addresses-list`, `addresses-empty`,
  `address-item`, `address-label`, `address-street`, `address-city`,
  `address-state`, `address-postal`, `address-country`, `remove-address`
- Notes: `new-note-input`, `save-note`, `notes-list`, `notes-empty`,
  `note-item`, `note-author`, `note-date`, `note-content`
- Actions: `save-contact`, `delete-contact`, `confirm-delete`,
  `cancel-delete`, `cancel-contact`
- Layout: `app-logo`, `current-user`, `nav-users`, `nav-password`,
  `logout-button`
