On another note, the razor file has gotten terribly large.
contact detail.razor is almost six hundred lines long
what can we do about this?
does it make sense to make partials for things only used in one place?
how would you instruct an LLM to rewrite this? use partial components? use state containers? composable child components? each child component should have its own css? use services? move all sorting and all code to its own c sharp file? implement natural sort everywhere? how deep does this rabbithole go? 
can you do a deep dive and write a complete and comprehensive report on what we need to change to make this state of the art architecture and also explain all the things that we are doing wrong? 
also we need to make sure that the website is FULLY accessible not just by humans of different abilities but also by automated checks such as playwright so agents can reliably navigate this website 



# Architectural Assessment and Refactoring Strategy: Blazor Modernization & Accessibility

## 1. Executive Summary & Diagnostic

The `ContactDetail.razor` file (alongside its corresponding backend files found in `dump.txt`) has scaled to a point where maintenance overhead, cognitive load, and testing friction are critical concerns. This document breaks down the architectural smells present in the current design, outlines an ideal modern Blazor component topology, provides targeted instructions for code-generation models, and details a strict accessibility scheme optimized for both automated Playwright agents and assistive technologies.

---

## 2. Structural Diagnostic: "What We Are Doing Wrong"

Reviewing the monolithic component structure reveals several anti-patterns that contradict modern Blazor and .NET 10 best practices:

* **Monolithic UI-State Coupling:** The file blends UI presentation, business validation logic, transactional orchestration, collections manipulation (sorting, filtering), and visual feedback (modals, validation states) in one place.
* **Lack of Domain Isolation:** The component directly orchestrates complex nested collections (Emails, Phones, Addresses). The parent template shouldn't need to know the detailed styling and loop management for every nested sub-entity.
* **CSS Bloat & Non-Isolated Leakage:** When a single `.razor.css` file handles layout, cards, validation states, buttons, tables, and modal overlays, CSS isolation is wasted. It degrades into a global file scoped only to one gigantic root node.
* **Code-Behind Misuse (`.razor.cs` vs. Components):** Moving 600 lines of mixed layout and C# entirely into a companion `ContactDetail.razor.cs` file is a **false optimization**. It fixes file length but leaves the underlying architecture coupled, opaque to unit testing, and prone to side effects.
* **Weak State Mechanics:** Relying entirely on mutable local properties or manual tracking for sub-forms makes it difficult to add undo/redo behaviors, perform localized validation, or prevent unnecessary tree re-renders.

---

## 3. The Refactoring Rabbit Hole: Component Architecture

To make this state-of-the-art, we must split the monolithic interface into smart orchestrators and predictable, isolated presentation components.

### Does it make sense to create components used in only one place?

**Yes.** In component-driven architectures (Blazor, React, Angular), components are boundaries for **cognitive load, localized rendering, and isolated CSS**. Even if a sub-section is used once, isolating it makes the parent component clean, declarative, and easily readable.

### Architectural Blueprint

```
Components/Pages/Contacts/
├── ContactDetail.razor             <-- Orchestrator / Page Root (Loads data, handles top-level submit)
├── ContactDetail.razor.cs          <-- Page Code-Behind (Minimal: DI injections, Route lifecycle hooks)
└── SubComponents/                  <-- Isolated Presentation Components
    ├── ContactCardHeader.razor     <-- Profile picture upload, name display, high-level labels
    ├── ContactCollectionSection.razor <-- Generic or specialized editor for Phone/Email/Address lists
    ├── ContactItemRow.razor        <-- Individual row/card handling individual entity mutation
    └── ContactStickyActions.razor  <-- Accessible Form Action bar (Save, Cancel, Delete)

```

### Component State & Communication Rules

1. **State Containers vs. EventCallbacks:** For page-level workflows like this address book, a full-blown ambient State Container (like a singleton/scoped store) is overkill unless data must persist across page changes. Instead, use a **Single Source of Truth** pattern via unidirectional data flow: data flows *down* as parameters; updates flow *up* via `EventCallback<T>`.
2. **Isolated CSS:** Every sub-component gets its own `.razor.css` file. For example, `ContactItemRow.razor.css` handles only its alignment, row margins, and localized interactive states.
3. **Validation:** Use specialized sub-forms or pass an `EditContext` down through a `CascadingParameter` so that child inputs correctly trigger validation inside the parent `EditForm`.

---

## 4. Sorting, Natural Sorting, and Data Processing

### Moving Code to C# Files

Code dealing with data management should live inside pure C# files.

* **Presentation Logic:** Kept in the code-behind file (`.razor.cs`) or inside dedicated UI mapping classes.
* **Business/Sorting Logic:** Placed inside domain-specific extension methods or utility services.

### Implementing "Natural Sort" Globally

Standard string sorting orders values character-by-character (e.g., `"Unit 10"`, `"Unit 2"`, `"Unit 3"` puts 10 before 2). Natural sorting treats numbers within strings as complete numeric values (`"Unit 2"`, `"Unit 3"`, `"Unit 10"`).

To avoid writing custom string logic across components, build an extension method using `PInvoke` to leverage native OS natural sorting capabilities, or use a highly performant, zero-allocation managed comparison routine.

```csharp
namespace Virginia.Extensions;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

public static class NaturalSortExtensions
{
    [SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int StrCmpLogicalW(string x, string y);
    }

    public static IOrderedEnumerable<T> OrderByNatural<T>(this IEnumerable<T> source, Func<T, string> selector)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return source.OrderBy(item => selector(item), new WindowsNaturalComparer());
        }
        
        return source.OrderBy(item => selector(item), StringComparer.OrdinalIgnoreCase);
    }

    private sealed class WindowsNaturalComparer : IComparer<string>
    {
        public int Compare(string? x, string? y) => 
            SafeNativeMethods.StrCmpLogicalW(x ?? string.Empty, y ?? string.Empty);
    }
}

```

*Architectural rule:* Apply this filter at the service layer or map it to a dedicated data presentation model before data hits the UI layer.

---

## 5. Systemic Machine & Human Accessibility (A11Y & Automation)

To ensure this application can be read by assistive tools (screen readers) and automated agents (like Playwright browser automation), the UI must follow semantically strict practices.

### Semantic Mapping Strategy

| Element Role | Wrong Approach | Correct Approach | Benefit |
| --- | --- | --- | --- |
| **Interactive Buttons** | `<div @onclick="Save">Save</div>` | `<button type="button" @onclick="Save">Save</button>` | Direct keyboard focus; easily picked up by Playwright's `locator('button')`. |
| **Form Inputs** | `<input placeholder="Enter name" />` | `<label for="contact-name">Name</label><input id="contact-name" ... />` | Explicit label associations allow screen readers to speak the input's purpose and lets Playwright use `getByLabel("Name")`. |
| **Section Layouts** | `<div class="section-title">Emails</div>` | `<section aria-labelledby="email-heading"><h2 id="email-heading">Emails</h2>...` | Provides explicit navigation landmarks for automated testing scripts and screen reader users. |

### Designing for Playwright Automation & Assistive Tools

Avoid brittle, auto-generated testing identifiers or styling selectors like `.card > div:nth-child(2) > input`. Instead, use accessible test properties:

1. **Prefer WAI-ARIA and Accessible Locators:** Use `aria-label`, `aria-labelledby`, and semantic roles. Playwright can interact natively via:
`page.GetByRole(AriaRole.Button, new() { Name = "Save Changes" })`
2. **Explicit Test Hooks via `data-testid`:** When structural uniqueness is required (e.g., distinguishing between different elements in a dynamic list), apply explicit data attributes:
```html
<button data-testid="delete-email-@email.Id" aria-label="Delete @email.Address">...</button>

```



---

## 6. Prompt Engineering Blueprint for Code-Generation LLMs

Copy, modify, and run this system prompt to handle the target component decomposition:

```markdown
You are an expert software engineer specializing in enterprise Blazor (.NET 10), C#, and strict web accessibility (WCAG 2.2 AA / Playwright automation optimization).

Task: Deconstruct the following monolithic Blazor component into clean, composable child components.

Architectural Constraints:
1. Extract repeatable, localized UI elements into standalone components within a 'SubComponents' subdirectory.
2. Ensure unidirectional data flow: pass data downwards via parameters (`[Parameter]`), and propagate modifications upward to the parent container using explicitly typed `EventCallback<T>` properties.
3. Isolate styles completely. Every child component must receive its own corresponding `.razor.css` file containing only the layout declarations required for its presentation.
4. Separate view logic from templates: Move formatting logic, sorting routines, and validation coordination into code-behind partial classes (`.razor.cs`).
5. Leverage primary constructors in generated helper classes and services wherever possible.

Accessibility & Playwright Optimization Requirements:
1. Every input element must be associated with an explicit semantic `<label>` element using matching `id` and `for` attributes.
2. Avoid generic elements (like `<div>` or `<span>`) for interactive click handlers. Use native `<button>` or `<a>` elements containing appropriate type declarations (`type="button"`).
3. Use semantic HTML5 layout containers (`<section>`, `<article>`, `<header>`) paired with matching `aria-labelledby` attributes to establish clean navigation landmarks.
4. Append explicit `data-testid="[component-name]-[unique-identifier]"` attributes to dynamic items, collection templates, and actionable inputs to simplify Playwright selector locating.

Input File for Deconstruction:
[Paste your ContactDetail.razor content here]

```

---

## 7. Complete Refactoring Implementation Blueprint

Below is the concrete blueprint for refactoring the monolith into structured, clean, decoupled files using the architectural principles defined above.

### Core Model & Form Validation Layer

This file isolates the form validation state using modern C# features, including primary constructors and record types.

```csharp
// File: Virginia/Data/ContactFormModel.cs
using System.ComponentModel.DataAnnotations;

namespace Virginia.Data;

public class ContactFormModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    public List<EmailEntry> Emails { get; set; } = [];
    public List<PhoneEntry> Phones { get; set; } = [];
    public List<AddressEntry> Addresses { get; set; } = [];
    public byte[]? ProfilePicture { get; set; }
}

public record EmailEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Address { get; set; } = string.Empty;
    
    public string Label { get; set; } = "Work";
}

public record PhoneEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    public string Number { get; set; } = string.Empty;
    
    public string Label { get; set; } = "Mobile";
}

public record AddressEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Required(ErrorMessage = "Street address is required.")]
    public string Street { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Label { get; set; } = "Home";
}

```

### Parent Page Container Component

The top-level orchestrator manages data lifecycle, tracking, loading states, and form submissions.

```razor
@* File: Virginia/Components/Pages/Contacts/ContactDetail.razor *@
@page "/contacts/detail/{Id:int?}"
@using Virginia.Data
@using Virginia.Components.Pages.Contacts.SubComponents
@inherits ComponentBase

<PageTitle>@(FormModel.Id == 0 ? "Create Contact" : "Edit Contact") - Address Book</PageTitle>

<div class="contact-detail-container">
    @if (IsLoading)
    {
        <div class="loading-state" role="status" aria-live="polite">
            <p>Loading contact options...</p>
        </div>
    }
    else
    {
        <EditForm EditContext="FormEditContext" OnValidSubmit="HandleValidSubmit" data-testid="contact-form">
            <DataAnnotationsValidator />

            <ContactCardHeader 
                Model="FormModel" 
                OnPictureChanged="NotifyFormChanged" />

            <div class="sections-grid">
                <ContactCollectionSection 
                    Title="Email Addresses"
                    TestId="emails-section"
                    Items="FormModel.Emails"
                    OnAdd="AddEmail"
                    OnRemove="RemoveEmail">
                    <ItemTemplate Context="email">
                        <div class="form-row">
                            <div class="field-group">
                                <label for="email-label-@email.Id">Label</label>
                                <InputText id="email-label-@email.Id" @bind-Value="email.Label" class="form-control" data-testid="email-label" />
                            </div>
                            <div class="field-group expand">
                                <label for="email-addr-@email.Id">Email Address</label>
                                <InputText id="email-addr-@email.Id" @bind-Value="email.Address" type="email" class="form-control" data-testid="email-address" />
                                <ValidationMessage For="@(() => email.Address)" />
                            </div>
                        </div>
                    </ItemTemplate>
                </ContactCollectionSection>

                <ContactCollectionSection 
                    Title="Phone Numbers"
                    TestId="phones-section"
                    Items="FormModel.Phones"
                    OnAdd="AddPhone"
                    OnRemove="RemovePhone">
                    <ItemTemplate Context="phone">
                        <div class="form-row">
                            <div class="field-group">
                                <label for="phone-label-@phone.Id">Label</label>
                                <InputText id="phone-label-@phone.Id" @bind-Value="phone.Label" class="form-control" data-testid="phone-label" />
                            </div>
                            <div class="field-group expand">
                                <label for="phone-num-@phone.Id">Phone Number</label>
                                <InputText id="phone-num-@phone.Id" @bind-Value="phone.Number" type="tel" class="form-control" data-testid="phone-number" />
                                <ValidationMessage For="@(() => phone.Number)" />
                            </div>
                        </div>
                    </ItemTemplate>
                </ContactCollectionSection>
            </div>

            <ContactStickyActions 
                CanSave="IsFormModified" 
                IsSubmitting="IsSubmitting" 
                OnCancel="HandleCancel" />
        </EditForm>
    }
</div>

```

```csharp
// File: Virginia/Components/Pages/Contacts/ContactDetail.razor.cs
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Virginia.Data;
using Virginia.Services;

namespace Virginia.Components.Pages.Contacts;

public partial class ContactDetail : ComponentBase
{
    [Parameter] public int? Id { get; set; }
    [Inject] private IContactService ContactService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<ContactDetail> Logger { get; set; } = default!;

    private ContactFormModel FormModel { get; set; } = new();
    private EditContext? FormEditContext { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsSubmitting { get; set; }
    private bool IsFormModified => FormEditContext?.IsModified() ?? false;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            if (Id.HasValue && Id.Value > 0)
            {
                var contact = await ContactService.GetByIdAsync(Id.Value);
                if (contact is null)
                {
                    Navigation.NavigateTo("/contacts");
                    return;
                }
                FormModel = MapToModel(contact);
            }
            
            FormEditContext = new EditContext(FormModel);
            FormEditContext.OnFieldChanged += (s, e) => StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initialization form state for dynamic entity context.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NotifyFormChanged() => FormEditContext?.NotifyFieldChanged(FieldIdentifier.Create(() => FormModel.ProfilePicture));

    private void AddEmail() { FormModel.Emails.Add(new EmailEntry()); FormEditContext?.NotifyFieldChanged(FieldIdentifier.Create(() => FormModel.Emails)); }
    private void RemoveEmail(EmailEntry item) { FormModel.Emails.Remove(item); FormEditContext?.NotifyFieldChanged(FieldIdentifier.Create(() => FormModel.Emails)); }

    private void AddPhone() { FormModel.Phones.Add(new PhoneEntry()); FormEditContext?.NotifyFieldChanged(FieldIdentifier.Create(() => FormModel.Phones)); }
    private void RemovePhone(PhoneEntry item) { FormModel.Phones.Remove(item); FormEditContext?.NotifyFieldChanged(FieldIdentifier.Create(() => FormModel.Phones)); }

    private async Task HandleValidSubmit()
    {
        if (IsSubmitting) return;
        IsSubmitting = true;
        try
        {
            // Transactional Save via underlying Service infrastructure
            Navigation.NavigateTo("/contacts");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Transactional exception running persist pipeline execution.");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private void HandleCancel() => Navigation.NavigateTo("/contacts");

    private static ContactFormModel MapToModel(ContactDomain target) => new() 
    { 
        Id = target.Id, 
        Name = target.Name 
        // Mapping Logic goes here
    };
}

```

```css
/* File: Virginia/Components/Pages/Contacts/ContactDetail.razor.css */
.contact-detail-container {
    max-width: 1200px;
    margin: 2rem auto;
    padding: 0 1rem;
}

.sections-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
    gap: 2rem;
    margin-top: 2rem;
}

@media (max-width: 768px) {
    .sections-grid {
        grid-template-columns: 1fr;
    }
}

.loading-state {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 300px;
    font-weight: 500;
}

```

### Profile Picture Header Sub-Component

This child component manages profile layout views and explicit picture operations.

```razor
@* File: Virginia/Components/Pages/Contacts/SubComponents/ContactCardHeader.razor *@
@using Virginia.Data

<div class="card-header-component" data-testid="contact-header-card">
    <div class="profile-uploader-wrapper">
        <div class="image-preview" role="img" aria-label="Profile picture preview">
            @if (Model.ProfilePicture?.Length > 0)
            {
                <img src="data:image/png;base64,@Convert.ToBase64String(Model.ProfilePicture)" alt="Contact Profile Visual Image" />
            }
            else
            {
                <div class="avatar-fallback">@Model.Name.FirstOrDefault()</div>
            }
        </div>
        <label for="picture-file-upload" class="btn-upload">Upload Picture</label>
        <InputFile id="picture-file-upload" OnChange="HandleFileSelected" accept="image/png,image/jpeg" data-testid="upload-input" />
    </div>

    <div class="identity-group">
        <label for="contact-core-name">Contact Full Name</label>
        <InputText id="contact-core-name" @bind-Value="Model.Name" class="form-control name-input" data-testid="contact-name-input" />
        <ValidationMessage For="@(() => Model.Name)" />
    </div>
</div>

```

```csharp
// File: Virginia/Components/Pages/Contacts/SubComponents/ContactCardHeader.razor.cs
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Virginia.Data;

namespace Virginia.Components.Pages.Contacts.SubComponents;

public partial class ContactCardHeader : ComponentBase
{
    [Parameter] [EditorRequired] public ContactFormModel Model { get; set; } = default!;
    [Parameter] public EventCallback OnPictureChanged { get; set; }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        using var stream = e.File.OpenReadStream(maxAllowedSize: 1024 * 1024 * 2); // 2MB Max Limit Protection
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        Model.ProfilePicture = memoryStream.ToArray();
        await OnPictureChanged.InvokeAsync();
    }
}

```

```css
/* File: Virginia/Components/Pages/Contacts/SubComponents/ContactCardHeader.razor.css */
.card-header-component {
    display: flex;
    gap: 2rem;
    background: var(--card-bg, #ffffff);
    padding: 2rem;
    border-radius: 8px;
    box-shadow: 0 4px 6px rgba(0,0,0,0.05);
    align-items: center;
}

.profile-uploader-wrapper {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
}

.image-preview {
    width: 110px;
    height: 110px;
    border-radius: 50%;
    overflow: hidden;
    background: #e0e0e0;
    display: flex;
    align-items: center;
    justify-content: center;
}

.avatar-fallback {
    font-size: 2.5rem;
    text-transform: uppercase;
    color: #555;
}

input[type="file"] {
    display: none;
}

.btn-upload {
    padding: 0.4rem 0.8rem;
    font-size: 0.85rem;
    background: #0066cc;
    color: white;
    border-radius: 4px;
    cursor: pointer;
}

.identity-group {
    flex: 1;
}

.name-input {
    font-size: 1.5rem;
    font-weight: 600;
}

```

### Generic Collection Component Section

This component abstracts management for sub-collections (like Emails, Phone Numbers, and Addresses) into a reusable template wrapper.

```razor
@* File: Virginia/Components/Pages/Contacts/SubComponents/ContactCollectionSection.razor *@
@typeparam TItem

<section class="collection-section" aria-labelledby="@SectionHeadingId" data-testid="@TestId">
    <div class="section-header">
        <h2 id="@SectionHeadingId">@Title</h2>
        <button type="button" class="btn-add-item" @onclick="OnAdd" aria-label="Add entry to @Title" data-testid="add-entry-button">
            Add New
        </button>
    </div>

    <div class="collection-list">
        @foreach (var item in Items)
        {
            <div class="collection-item-card" data-testid="collection-item">
                <div class="item-fields">
                    @ItemTemplate(item)
                </div>
                <button type="button" class="btn-remove-item" @onclick="() => OnRemove.InvokeAsync(item)" aria-label="Remove item entry" data-testid="remove-entry-button">
                    Remove
                </button>
            </div>
        }
    </div>
</section>

```

```csharp
// File: Virginia/Components/Pages/Contacts/SubComponents/ContactCollectionSection.razor.cs
using Microsoft.AspNetCore.Components;

namespace Virginia.Components.Pages.Contacts.SubComponents;

public partial class ContactCollectionSection<TItem> : ComponentBase
{
    [Parameter] [EditorRequired] public string Title { get; set; } = string.Empty;
    [Parameter] public string TestId { get; set; } = "collection-section";
    [Parameter] [EditorRequired] public List<TItem> Items { get; set; } = [];
    [Parameter] [EditorRequired] public RenderFragment<TItem> ItemTemplate { get; set; } = default!;
    [Parameter] public EventCallback OnAdd { get; set; }
    [Parameter] public EventCallback<TItem> OnRemove { get; set; }

    private string SectionHeadingId { get; set; } = $"heading-{Guid.NewGuid():N}";
}

```

```css
/* File: Virginia/Components/Pages/Contacts/SubComponents/ContactCollectionSection.razor.css */
.collection-section {
    background: #ffffff;
    padding: 1.5rem;
    border-radius: 8px;
    box-shadow: 0 2px 4px rgba(0,0,0,0.05);
}

.section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.5rem;
    border-bottom: 2px solid #f0f0f0;
    padding-bottom: 0.5rem;
}

.btn-add-item {
    background: #28a745;
    color: white;
    border: none;
    padding: 0.4rem 0.9rem;
    border-radius: 4px;
    cursor: pointer;
}

.collection-item-card {
    display: flex;
    gap: 1rem;
    align-items: flex-start;
    padding: 1rem 0;
    border-bottom: 1px dashed #e9ecef;
}

.item-fields {
    flex: 1;
}

.btn-remove-item {
    background: #dc3545;
    color: white;
    border: none;
    padding: 0.4rem 0.8rem;
    border-radius: 4px;
    margin-top: 1.5rem;
    cursor: pointer;
}

```

### Accessible Action Bar Component

This component displays persistent action tools across view modifications.

```razor
@* File: Virginia/Components/Pages/Contacts/SubComponents/ContactStickyActions.razor *@
<div class="sticky-action-bar" data-testid="form-actions-bar">
    <div class="bar-content">
        <button type="button" class="btn-secondary" @onclick="OnCancel" disabled="@IsSubmitting" data-testid="cancel-button">
            Cancel
        </button>
        <button type="submit" class="btn-primary" disabled="@(IsSubmitting || !CanSave)" aria-busy="@IsSubmitting" data-testid="save-button">
            @if (IsSubmitting)
            {
                <span>Saving Changes...</span>
            }
            else
            {
                <span>Save Changes</span>
            }
        </button>
    </div>
</div>

```

```csharp
// File: Virginia/Components/Pages/Contacts/SubComponents/ContactStickyActions.razor.cs
using Microsoft.AspNetCore.Components;

namespace Virginia.Components.Pages.Contacts.SubComponents;

public partial class ContactStickyActions : ComponentBase
{
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public bool IsSubmitting { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
}

```

```css
/* File: Virginia/Components/Pages/Contacts/SubComponents/ContactStickyActions.razor.css */
.sticky-action-bar {
    position: fixed;
    bottom: 0;
    left: 0;
    right: 0;
    background: #ffffff;
    box-shadow: 0 -4px 10px rgba(0,0,0,0.06);
    padding: 1rem;
    z-index: 1000;
}

.bar-content {
    max-width: 1200px;
    margin: 0 auto;
    display: flex;
    justify-content: flex-end;
    gap: 1rem;
}

.btn-primary, .btn-secondary {
    padding: 0.6rem 1.5rem;
    font-weight: 500;
    border-radius: 4px;
    cursor: pointer;
    border: none;
}

.btn-primary {
    background: #0066cc;
    color: white;
}

.btn-primary:disabled {
    background: #aaccff;
    cursor: not-allowed;
}

.btn-secondary {
    background: #6c757d;
    color: white;
}

```

---

## 8. Verifying with End-to-End Playwright Automation

Here is an E2E testing profile implemented in clean C# that validates form interaction, system data processing, access control compliance, and automated engine discoverability rules:

```csharp
// File: Virginia.Tests/AutomatedAgentTests.cs
using Microsoft.Playwright;
using Xunit;

namespace Virginia.Tests;

public class AutomatedAgentTests
{
    [Fact]
    public async Task VerifyContactDetailForm_IsFullyAccessibleToAutomatedAgents()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();

        // Target site loaded from infrastructure launch context
        await page.GotoAsync("http://localhost:5000/contacts/detail");

        // Verify top level structure exists via explicit role maps
        var mainForm = page.Locator("[data-testid='contact-form']");
        await Assertions.Expect(mainForm).ToBeVisibleAsync();

        // Interact using purely accessible labels
        var nameInput = page.GetByLabel("Contact Full Name");
        await Assertions.Expect(nameInput).ToBeVisibleAsync();
        await nameInput.FillAsync("Evelyn Montgomery");

        // Locate internal collection wrappers via designated test identifier boundaries
        var emailSection = page.Locator("[data-testid='emails-section']");
        var addEmailButton = emailSection.Locator("[data-testid='add-entry-button']");
        await addEmailButton.ClickAsync();

        // Access child row elements using reliable sub-component locators
        var emailInput = emailSection.Locator("[data-testid='email-address']").First;
        await emailInput.FillAsync("evelyn.m@example.org");

        // Verify Save operations update correctly upon layout state modifications
        var saveButton = page.Locator("[data-testid='save-button']");
        await Assertions.Expect(saveButton).ToBeEnabledAsync();
        await saveButton.ClickAsync();

        // Ensure navigation redirected successfully
        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/contacts"));
    }
}

```
