# HomeFix – AI-Enhanced Local Services Marketplace

ASP.NET Core 10 Web API + EF Core + SQL Server + JWT  |  React (Vite) frontend

## 1. Requirements
- Visual Studio 2026 (or 2022 17.14+) with the **ASP.NET and web development** workload
- **.NET 10 SDK**
- **Node.js 18+** (includes npm)
- SQL Server Express / LocalDB (default: `.\SQLEXPRESS`)
- Internet access once, to restore NuGet and npm packages

## 2. Open the project
Extract the ZIP, then open `HomeFix.sln` in Visual Studio.

## 3. Restore backend packages
Visual Studio restores NuGet automatically. Or run:
```
cd HomeFix.API
dotnet restore
```

## 4. Database configuration
Edit `HomeFix.API/appsettings.json` -> `ConnectionStrings:DefaultConnection`.

| Your SQL Server | Connection string |
|---|---|
| SQL Server Express (default) | `Server=.\\SQLEXPRESS;Database=HomeFixDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true` |
| Named instance | `Server=YOURPC\\SQLEXPRESS;Database=HomeFixDb;Trusted_Connection=True;TrustServerCertificate=True` |
| LocalDB | `Server=(localdb)\\MSSQLLocalDB;Database=HomeFixDb;Trusted_Connection=True;TrustServerCertificate=True` |

On first run the API **creates the database and seeds demo data automatically** (seeding is skipped if users already exist).

### Migrations (optional, recommended for your report)
The app uses EF Core migrations automatically if a `Migrations` folder exists; otherwise it creates the schema directly. To generate migrations:
```
dotnet tool install --global dotnet-ef
cd HomeFix.API
dotnet ef migrations add InitialCreate
dotnet ef database update
```
If you switch to migrations after the DB already exists, drop it first: `dotnet ef database drop` (or delete `HomeFixDb` in SSMS).

## 5. Run the backend
Visual Studio: select the **http** profile and press **F5**, or:
```
cd HomeFix.API
dotnet run --launch-profile http
```
- Backend URL: http://localhost:5080
- Swagger: http://localhost:5080/swagger

## 6. Install and run the frontend
```
cd HomeFix.Client
npm install
npm run dev
```
(or double-click `run-frontend.bat`). Frontend URL: **http://localhost:5173**
The API address is set in `HomeFix.Client/.env` (`VITE_API_URL`).

## 7. Demo accounts
| Role | Email | Password |
|---|---|---|
| Admin | admin@homefix.com | Admin@123 |
| Customer | customer@homefix.com | Customer@123 |
| Customer | sara@homefix.com | Customer@123 |
| Provider (Electrician) | electrician@homefix.com | Provider@123 |
| Provider (Plumber) | plumber@homefix.com | Provider@123 |
| Provider (Painter) | painter@homefix.com | Provider@123 |
| Provider (Carpenter / Cleaner / Mechanic) | carpenter@ / cleaner@ / mechanic@homefix.com | Provider@123 |
| **Pending** provider (approve in Admin) | tariq.electrician@homefix.com, hamid.plumber@homefix.com | Provider@123 |

Swagger: call `POST /api/auth/login`, copy the `token`, click **Authorize**, paste the token (without "Bearer").

## 8. AI search configuration
Works with **no key** (built-in rule-based parser). To use the real AI, set a key (never commit it):
```
cd HomeFix.API
dotnet user-secrets init
dotnet user-secrets set "AI:ApiKey" "YOUR_ANTHROPIC_API_KEY"
```
or set the environment variable `AI__ApiKey`. Model is `AI:Model` in appsettings.json. If the AI call fails, the app falls back to the built-in parser automatically.

## 9. Common problems
- **Cannot connect to SQL Server** – fix the connection string (section 4) and make sure the SQL Server service is running.
- **CORS error / "Cannot reach the HomeFix server"** – start the API first; frontend must be on port 5173 (`Cors:AllowedOrigins`).
- **Jwt:Key missing** – run in Development (the launch profiles do), or set env var `Jwt__Key` (32+ characters).
- **Port 5080 busy** – change `applicationUrl` in `Properties/launchSettings.json` and `VITE_API_URL` in `HomeFix.Client/.env`.
- **Provider not visible in search** – providers must be approved (Admin -> Provider Approvals).
- **Booking says provider does not work that day** – pick a working day (see provider profile).
- **Seed data missing** – drop the database and run the API again.
