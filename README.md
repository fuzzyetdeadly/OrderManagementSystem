# Demo Order Management System
---

This repository contains a demo order management system.
It was built to showcase my coding practice in full-stack development with .NET/ASP.NET Core, EF Core technologies, and React with a PostgreSQL database.

SQLite is used as an in memory database for testing, prioritizing speed and simplicity. Docker support may be considered in future.

## Features

### Back-end

* A simple RESTful Order Web API built on ASP.NET Core
* Swagger UI for testing the API
* Contains built in validation logic for existing scenarios
* In developer mode, will seed a test customer if there is none on startup

### Front-end

* A simple React web app, to add a basic order and manage orders

## How to run

Required installs:

* PostgreSQL
* Visual Studio (for .NET) and Visual Studio Code (for React)
* .NET SDK for .NET 10.0
* `dotnet ef` with `dotnet tool install --global dotnet-ef`
* Node.js for React

Run steps:

1\. Clone the project and open it locally  
2\. Open `OrderManagement.API/appsettings.json` and set your PostgreSQL password  
3\. (Optional) Run `dotnet restore` to install dependencies  
3\. `cd OrderManagement.API/appsettings.json` to give Visual Studio startup context  
4\. Run `dotnet ef database update` to create the initial database and it's tables.   

Note: If you see the logs creating tables, it should have succeeded, but you can double check with `dotnet ef migrations list`, which will connect to the DB and list them from `__EFMigrationsHistory`.

5\. (Optional) Connect to the DB with `pgAdmin`  
(run as admin, because Windows Smart control sometimes blocks `libpq.dll`,  
which requires a re-install to fix.)  
6\. (Optional) Inspect the DB to verify that tables were created correctly.  
7\. Click the `run` button in `Visual Studio` to start the back-end  
8\. `cd Frontend.React`, then `npm run dev` to run in development mode

## Accessing the API and Web app

* Localhost only for now
* SwaggerUI (development mode only): `https://localhost:7000/swagger/index.html`
* POC React web app: `http://localhost:5173/`

## Resetting migrations 

If there are changes to any of the following:

* `OrderManagement/Domain/Entities/*.cs`
* `OrderManagement/Infrastructure/AppDbContext.cs`
* `OrderManagement/Infrastructure/Migrations/Persistence/*` configurations

Then it will be necessary to reset migrations for fresh clones.
This can be done by running `ResetMigrations.bat` from the solution folder.

Warning: Must be done from the solution folder, or the script will fail!

## Future work

Planned additions, updated as time and priorities allow.

### Features

* Better order submission form (add/remove items)

### Tests

* Front end tests for web app
* End-to-end controller tests (halfway done, needed to re-prioritize React learning)

## Author's professional summary

Timothy Guan is a Software Engineering Manager with 14 years of experience across the full SDLC, delivering enterprise-grade applications and tools in a global multinational environment. 

His experience covers working within distributed international teams spanning multiple continents and time zones, with a strong background in Agile/Scrum including the Scrum Master role.

For the past few years, he has been growing into a senior C#/.NET/ASP.NET Core and React engineering role, with a passion for specializing his career towards full stack development.

In his free time, he develops and maintains a C# application for a niche community. Open to remote, hybrid, or on-site roles in Australia, or fully remote roles globally.
