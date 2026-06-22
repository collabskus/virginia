these two packages are deprecated and have vulnerabilities but these are NOT direct dependencies. What is going on? Can you please review the dump.txt and come up with a plan to address this issue? What is the underlying issue? How can we fix it? 
Warning As Error: Package 'SQLitePCLRaw.lib.e_sqlite3' 2.1.11 has a known high severity vulnerability, https://github.com/advisories/GHSA-2m69-gcr7-jv3q
Warning As Error: Package 'SQLitePCLRaw.lib.e_sqlite3' 2.1.11 has a known high severity vulnerability, https://github.com/advisories/GHSA-2m69-gcr7-jv3q
Severity    Code    Description    Project    File    Line    Suppression State
Error (active)    NU1903    Warning As Error: Package 'SQLitePCLRaw.lib.e_sqlite3' 2.1.11 has a known high severity vulnerability, https://github.com/advisories/GHSA-2m69-gcr7-jv3q    Virginia.Tests    D:\DEV\personal\Virginia\Virginia.Tests\Virginia.Tests.csproj    1    
Error (active)    NU1903    Warning As Error: Package 'SQLitePCLRaw.lib.e_sqlite3' 2.1.11 has a known high severity vulnerability, https://github.com/advisories/GHSA-2m69-gcr7-jv3q    Virginia    D:\DEV\personal\Virginia\Virginia\Virginia.csproj    1
10:00 AM

### 1. What is Going On? (The Underlying Issue)

The build errors (`NU1903`) are triggered because you have **`TreatWarningsAsErrors` enabled** in your project configurations, and NuGet's security auditing system (`NuGetAudit`) found a high-severity vulnerability.

Even though you didn't explicitly add `SQLitePCLRaw.lib.e_sqlite3` to your project files, it is being introduced into your application as a **transitive dependency**. The dependency chain looks like this:
`Microsoft.EntityFrameworkCore.Sqlite (10.0.9)` $\rightarrow$ `Microsoft.Data.Sqlite` $\rightarrow$ `SQLitePCLRaw.bundle_e_sqlite3` $\rightarrow$ **`SQLitePCLRaw.lib.e_sqlite3 (2.1.11)`**.

#### The Vulnerability Details:

* **Advisory/CVE:** CVE-2025-6965 / GHSA-2m69-gcr7-jv3q.
* **The Problem:** The native SQLite library compiled into `SQLitePCLRaw.lib.e_sqlite3` versions `<= 2.1.11` is older than version `3.50.2`. It contains a memory corruption flaw related to processing query structures that exceed column thresholds.
* **The Complicated Part:** The package maintainer has **deprecated** the `2.x` line of `SQLitePCLRaw.lib.e_sqlite3` with `first_patched_version: null`. A patch will not be released for the `2.x` branch. Instead, the fix is implemented in the major `3.x` branch (`SQLitePCLRaw.bundle_e_sqlite3 >= 3.0.3`), which completely drops the vulnerable native wrapper package in favor of a newer module named `SourceGear.sqlite3`.

---

### 2. How Can We Fix It? (Action Plan)

Depending on your production environment constraints, here are the two most practical ways to fix this for your .NET 10 setup:

#### Option A: Promote and force the fixed 3.x Bundle (Recommended)

Since `Microsoft.EntityFrameworkCore.Sqlite 10.x` accepts versions of `SQLitePCLRaw.bundle_e_sqlite3 >= 2.0.4`, you can explicitly override the entire dependency graph to pull the non-vulnerable `3.x` version by specifying it centrally in your package configurations.

1. Open your **`Directory.Packages.props`** file.
2. Add a direct package version configuration to override the graph and bypass the `2.x` line entirely:

```xml
<ItemGroup>
  <PackageVersion Include="SQLitePCLRaw.bundle_e_sqlite3" Version="3.0.3" />
  
  <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.9" />
  <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.9" />
  </ItemGroup>

```

3. Open **`Virginia.csproj`** and **`Virginia.Tests.csproj`** and add a direct `PackageReference` to force NuGet to resolve to the pinned `3.0.3` bundle:

```xml
<ItemGroup>
  <PackageReference Include="SQLitePCLRaw.bundle_e_sqlite3" />
</ItemGroup>

```

#### Option B: Switch to the System SQLite Engine (Best for Container Security)

Since your application is packaged and deployed via a Linux container (`Containerfile` / `compose.yaml`), utilizing your operating system's native SQLite binary is highly secure because it offloads patching to the container base image package manager (`apt-get`).

1. In **`Directory.Packages.props`**, swap the "batteries-included" package out for the Core variation, and declare the native system bridge:

```xml
<PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite.Core" Version="10.0.9" />
<PackageVersion Include="SQLitePCLRaw.provider.sqlite3" Version="2.1.11" />

```

2. Update your **`.csproj`** files to consume `Microsoft.EntityFrameworkCore.Sqlite.Core` and `SQLitePCLRaw.provider.sqlite3` instead of the bundled version.
3. Ensure you initialize the system-level SQLite provider inside your application startup (`Program.cs`) before configuring Entity Framework:

```csharp
SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());

```

4. Ensure your `Containerfile` explicitly installs the native SQLite development or runtime libraries so the container environment can execute queries:

```dockerfile
RUN apt-get update && apt-get install -y libsqlite3-dev

```

How do I add this apt get update to my Containerfile? 

```yaml Containerfile
# syntax=docker/dockerfile:1

# ─────────────────────────────────────────────────────────────────────────────
# Build stage — full .NET 10 SDK. Nothing here ends up on the Fedora host.
# We restore/publish ONLY the Virginia web project, so Virginia.AppHost (Aspire
# orchestrator) is never built and the Aspire workload is never required.
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-wide MSBuild config first so layer caching works for restore.
COPY Directory.Build.props Directory.Packages.props ./

# Copy the projects the web app actually depends on.
COPY Virginia.ServiceDefaults/ Virginia.ServiceDefaults/
COPY Virginia/ Virginia/

# Restore + publish the web project only.
RUN dotnet restore Virginia/Virginia.csproj
RUN dotnet publish Virginia/Virginia.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ─────────────────────────────────────────────────────────────────────────────
# Runtime stage — ASP.NET runtime only. No SDK, no source.
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Kestrel listens on 8080 (plain HTTP); a proxy/host maps it outward.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish ./

# Persisted data lives here; mounted as a volume in compose.
RUN mkdir -p /data && chown -R app:app /data /app
VOLUME /data

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "Virginia.dll"]

```
There is a small catch with your current `Containerfile` base images. Microsoft's `.NET` images (like `mcr.microsoft.com/dotnet/aspnet:10.0`) are actually built on top of **Debian** by default, which does use `apt-get`.

However, you need to add `apt-get update` and install `libsqlite3-dev` (or `sqlite3` runtime) inside the **`final` runtime stage**, because that is what actually boots up and runs your application code. Furthermore, because your file drops privileges to a non-root user (`USER app`) near the bottom, you must run the installation command **while you are still the `root` user** (which is the default at the beginning of the stage).

Here is how you update your `Containerfile` to install the system-level SQLite library safely:

```dockerfile
# syntax=docker/dockerfile:1

# ─────────────────────────────────────────────────────────────────────────────
# Build stage — full .NET 10 SDK. Nothing here ends up on the Fedora host.
# We restore/publish ONLY the Virginia web project, so Virginia.AppHost (Aspire
# orchestrator) is never built and the Aspire workload is never required.
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-wide MSBuild config first so layer caching works for restore.
COPY Directory.Build.props Directory.Packages.props ./

# Copy the projects the web app actually depends on.
COPY Virginia.ServiceDefaults/ Virginia.ServiceDefaults/
COPY Virginia/ Virginia/

# Restore + publish the web project only.
RUN dotnet restore Virginia/Virginia.csproj
RUN dotnet publish Virginia/Virginia.csproj \
    -c Release \
    -o /app/publish \
    -no-restore \
    /p:UseAppHost=false

# ─────────────────────────────────────────────────────────────────────────────
# Runtime stage — ASP.NET runtime only. No SDK, no source.
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# 1. INSTALL SYSTEM SQLITE DEPENDENCY (Run as root, which is default here)
# We chain apt-get commands together and clean up the package cache immediately 
# to keep the container layer as small as possible.
RUN apt-get update && apt-get install -y --no-install-recommends \
    libsqlite3-dev \
    && rm -rf /var/lib/apt/lists/*

# Kestrel listens on 8080 (plain HTTP); a proxy/host maps it outward.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish ./

# Persisted data lives here; mounted as a volume in compose.
RUN mkdir -p /data && chown -R app:app /data /app
VOLUME /data

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "Virginia.dll"]

```

### Why it's written this way:

1. **`--no-install-recommends`**: This ensures `apt` doesn't pull in extra, unnecessary packages (like text editors or documentation files), keeping your production container lean.
2. **`&& rm -rf /var/lib/apt/lists/*`**: It's a Docker best practice to clean up the downloaded package indices in the *same* `RUN` layer they were retrieved. If you don't do this, the downloaded update logs stay baked into the final image size forever.
3. **Placement**: Placed right below the `FROM ... AS final` line ensures it executes with `root` privileges. If you placed it below `USER app`, the command would fail with a permission denied error.
