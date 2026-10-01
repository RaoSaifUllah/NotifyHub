# NotifyHub

NotifyHub is a self-hosted notification gateway designed to bring application
notifications into one place. Applications submit events, and NotifyHub routes
them to destinations such as Email, Telegram, Discord, webhooks and Web Push.

The project combines an ASP.NET Core backend, PostgreSQL database and a React
frontend with responsive light and dark themes. Its planned workflow includes
workspace management, application API keys, routing rules, templates, quiet hours
and delivery history with retries.

NotifyHub is currently under development. You can run the dashboard preview and
API locally; notification delivery and the complete account workflow are not yet
available.

## UI Preview

### Light theme

![NotifyHub light theme](document/screenshots/baseline-light.png)

### Dark theme

![NotifyHub dark theme](document/screenshots/baseline-dark.png)

## How to use

### 1. Install prerequisites

- .NET SDK 10.0.401, selected by global.json.
- Node.js 24.11 or newer.
- A dedicated PostgreSQL database.

### 2. Download and install

    git clone https://github.com/RaoSaifUllah/NotifyHub.git
    cd NotifyHub
    dotnet tool restore
    dotnet restore NotifyHub.slnx --locked-mode
    npm ci --prefix Frontend

### 3. Configure PostgreSQL

Create backend/src/NotifyHub.Api/appsettings.Local.json with your database
connections:

    {
      "ConnectionStrings": {
        "Write": "Host=localhost;Database=notifyhub;Username=notifyhub_write;Password=YOUR_WRITE_PASSWORD",
        "Read": "Host=localhost;Database=notifyhub;Username=notifyhub_read;Password=YOUR_READ_PASSWORD",
        "Migration": "Host=localhost;Database=notifyhub;Username=YOUR_SCHEMA_OWNER;Password=YOUR_OWNER_PASSWORD"
      }
    }

Use separate database credentials for migrations, writes and read-only queries.
The local settings file is excluded from Git.

Follow the [database setup guide](document/Database-Development.md) to provision
roles and apply migrations before starting the API.

### 4. Start the backend

From the project root:

    dotnet run --project backend/src/NotifyHub.Api --no-launch-profile --urls http://127.0.0.1:5080

Check API health at http://127.0.0.1:5080/health and database readiness at
http://127.0.0.1:5080/ready.

### 5. Start the frontend

Open another terminal in the project root:

    cd Frontend
    npm run dev

Open the local address printed by Vite. The dashboard lets you view API status,
switch between light and dark themes, and explore the responsive interface.