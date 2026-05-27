# Blazor WebAssembly Project Files Summary

## Complete File List

### Root Configuration Files
- `App.razor` - Root application component with routing
- `_Imports.razor` - Global using statements
- `Program.cs` - Application entry point with DI configuration

### Models (CollabsKus.BlazorWebAssembly/Models/)
1. **CalendarResponse.cs** - Models for calendar API response
2. **TimeResponse.cs** - Models for time API response
3. **MoonPhase.cs** - Moon phase data model

### Services (CollabsKus.BlazorWebAssembly/Services/)
1. **KathmanduCalendarService.cs** - Calendar/time API with caching
2. **MoonPhaseService.cs** - Client-side moon phase calculation
3. **ApiLoggerService.cs** - Non-blocking API request logging

### Components (CollabsKus.BlazorWebAssembly/Components/)
1. **TimeDisplay.razor** + CSS - Current time display
2. **DateCards.razor** + CSS - Bikram Sambat and Gregorian dates
3. **MoonDisplay.razor** + CSS - Moon phase visualization
4. **CalendarGrid.razor** + CSS - Full month calendar

### Layout (CollabsKus.BlazorWebAssembly/Layout/)
- **MainLayout.razor** + CSS - Main page layout wrapper

### Pages (CollabsKus.BlazorWebAssembly/Pages/)
- **Home.razor** + CSS - Main home page with state management

### WWW Root (CollabsKus.BlazorWebAssembly/wwwroot/)
- **css/app.css** - Global styles
- **index.html** - HTML entry point

### GitHub Actions (.github/workflows/)
- **deploy.yml** - Automated deployment to GitHub Pages

## Key Features

✅ **No JavaScript** - Everything in C#
✅ **No NuGet Packages** - Uses only built-in libraries
✅ **No External CSS** - All styles written from scratch
✅ **Component-Based** - Each UI element is a separate component
✅ **Scoped CSS** - Automatic style isolation per component
✅ **Service Layer** - Clean separation of concerns
✅ **Intelligent Caching** - Reduces API calls
✅ **API Logging** - Tracks all requests non-blocking
✅ **Responsive Design** - Mobile-first approach
✅ **Real-time Updates** - Multiple timer strategies

## File Sizes
- Total C# files: 10
- Total Razor components: 6
- Total CSS files: 7
- Total project files: ~25

## How to Use

1. Copy all files to your project directory
2. Ensure .github/workflows/deploy.yml is in the repository root
3. Push to GitHub
4. GitHub Actions will automatically build and deploy
5. Access at https://collabskus.github.io

## Architecture Highlights

### Component Hierarchy
```
App.razor
└── MainLayout.razor
    └── Home.razor (Page)
        ├── TimeDisplay
        ├── DateCards
        ├── MoonDisplay
        └── CalendarGrid
```

### Service Dependencies
```
Home.razor
├── KathmanduCalendarService
│   └── ApiLoggerService
└── MoonPhaseService
```

### Data Flow
1. User visits page
2. Home.razor initializes
3. Services fetch initial data
4. Components render with data
5. Timers manage updates:
   - Clock: 1s interval
   - Time API: 1h interval
   - Calendar API: 24h interval

## Customization Points

- **Colors**: Modify gradient in wwwroot/css/app.css
- **Timings**: Adjust intervals in Home.razor
- **Cache Duration**: Change in KathmanduCalendarService.cs
- **API Endpoints**: Update constants in services

## Learning Outcomes

By studying this project, you'll learn:
1. Blazor component lifecycle
2. Dependency injection patterns
3. State management techniques
4. Timer-based updates
5. API integration with caching
6. Scoped CSS isolation
7. Responsive design in Blazor
8. GitHub Pages deployment
9. Service-oriented architecture
10. Error handling strategies
