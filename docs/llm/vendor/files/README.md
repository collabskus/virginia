# Kathmandu Calendar & Time - Blazor WebAssembly

A beautiful, responsive Blazor WebAssembly application that displays the current time and calendar for Kathmandu, Nepal, including moon phase information.

## 🏗️ Project Structure

```
CollabsKus.BlazorWebAssembly/
├── Components/              # Reusable UI components
│   ├── CalendarGrid.razor   # Displays the Nepali calendar grid
│   ├── DateCards.razor      # Shows Bikram Sambat and Gregorian dates
│   ├── MoonDisplay.razor    # Displays current moon phase
│   └── TimeDisplay.razor    # Shows current time in English and Nepali
├── Layout/
│   └── MainLayout.razor     # Main layout wrapper
├── Models/                  # Data models
│   ├── CalendarResponse.cs  # Calendar API response model
│   ├── MoonPhase.cs        # Moon phase data model
│   └── TimeResponse.cs     # Time API response model
├── Pages/
│   └── Home.razor          # Main home page
├── Services/               # Business logic services
│   ├── ApiLoggerService.cs # Logs API requests to Cloudflare Workers
│   ├── KathmanduCalendarService.cs # Handles calendar/time API calls
│   └── MoonPhaseService.cs # Calculates moon phases
├── wwwroot/
│   ├── css/
│   │   └── app.css        # Global styles
│   └── index.html         # Main HTML entry point
├── App.razor              # Root component
├── Program.cs             # Application entry point
└── _Imports.razor         # Global using statements
```

## 🎨 Architecture Decisions

### Component-Based Design
Each UI element is a separate component with its own scoped CSS:
- **TimeDisplay**: Real-time clock with Nepali numerals
- **DateCards**: Bikram Sambat and Gregorian dates
- **MoonDisplay**: Current moon phase with icon
- **CalendarGrid**: Full month calendar with today highlighted

### Service Layer
Business logic is separated into services:
- **KathmanduCalendarService**: Manages API calls with intelligent caching
  - Calendar data cached for 1 hour
  - Time data cached for 5 minutes
  - Calculates server time offset for accurate local time
- **MoonPhaseService**: Client-side moon phase calculation using astronomical algorithms
- **ApiLoggerService**: Logs all API requests to Cloudflare Workers (non-blocking)

### State Management
- Component state managed in `Home.razor`
- Timers for:
  - Clock updates (every 1 second)
  - Time API refresh (every 1 hour)
  - Calendar API refresh (every 24 hours)

### CSS Strategy
- **Global styles** in `wwwroot/css/app.css` for body, page layout
- **Scoped CSS** for each component (automatic isolation)
- **No external dependencies** - all CSS written from scratch
- **Responsive design** with mobile-first breakpoints

## 🚀 How It Works

### Data Flow
1. **Initial Load**:
   - Home page fetches calendar and time data
   - Calculates initial moon phase
   - Starts three timers

2. **Real-time Updates**:
   - Clock timer updates every second (local calculation)
   - Time API refreshes hourly to prevent drift
   - Calendar API refreshes daily

3. **Caching Strategy**:
   - Services cache API responses in memory
   - Cached responses logged with `fromCache: true`
   - Fresh API calls logged with `fromCache: false`

4. **Error Handling**:
   - API failures don't crash the app
   - Logging failures are silent (non-critical)
   - User sees friendly error messages

### API Integration
- **Calendar API**: `https://calendar.bloggernepal.com/api/today`
- **Time API**: `https://calendar.bloggernepal.com/api/time`
- **Logger API**: `https://my-api.2w7sp317.workers.dev/ui/create`

### Moon Phase Calculation
Uses Julian Day Number algorithm:
- Calculates days since known new moon (Jan 6, 2000)
- Determines current lunar age
- Computes illumination percentage
- Maps to appropriate phase icon

## 🎓 Learning Points

### Blazor Concepts Demonstrated
1. **Component Composition**: Building complex UIs from small components
2. **Dependency Injection**: Services injected into components
3. **Lifecycle Methods**: `OnInitializedAsync` for data loading
4. **Scoped CSS**: Component-specific styling
5. **Parameter Binding**: Passing data between components
6. **Timer Management**: Background tasks with proper disposal
7. **Error Boundaries**: Graceful error handling

### Best Practices
- ✅ Separation of concerns (UI, logic, data)
- ✅ Single responsibility principle
- ✅ Async/await for API calls
- ✅ Proper resource disposal (IDisposable)
- ✅ Responsive design
- ✅ No JavaScript (pure C#)
- ✅ No external dependencies

## 🔧 Configuration

The app is configured for GitHub Pages deployment at `https://collabskus.github.io`.

### Base Path
The `<base href="/" />` in `index.html` is set for root domain deployment.

### Service Worker
Configured for offline support with automatic updates.

## 📦 Deployment

GitHub Actions automatically builds and deploys on push to main/master:
1. Builds the Blazor WASM project
2. Publishes to `release/wwwroot`
3. Uploads to GitHub Pages
4. Deploys to production

## 🎯 Features

- ✨ Real-time clock synchronized with Kathmandu time
- 📅 Bikram Sambat (Nepali) calendar
- 🌍 Gregorian calendar
- 🌙 Accurate moon phase calculation
- 📱 Fully responsive (desktop, tablet, mobile)
- 🎨 Beautiful gradient background with glassmorphism
- 📊 API request logging
- ⚡ Intelligent caching
- 🔄 Automatic refresh intervals
- 💪 No external dependencies

## 🧪 Testing Locally

```bash
dotnet run --project CollabsKus.BlazorWebAssembly
```

Navigate to `https://localhost:7212` or the port shown in console.

## 📚 Additional Resources

- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [Scoped CSS](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation)
- [Dependency Injection](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection)
