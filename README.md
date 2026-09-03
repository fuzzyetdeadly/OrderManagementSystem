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
* In-memory message queue for order created events in development mode
* RabbitMQ message queue for order created events in production mode
* An order created consumer that subscribes to the queue and logs created orders
* Near complete test coverage for repository, service, controllers, queue and consumer

### Front-end

* A simple React web app using Material UI
* Allows basic order creation and management
* Near complete test coverage for front-end API client and component tests

### Repository 

* CI configuration to automate tests for changes to pull requests, which includes package caching for NuGet and Node

## How to run

Run dependencies:

* PostgreSQL
* Visual Studio (for .NET) and Visual Studio Code (for React)
* .NET SDK for .NET 10.0
* `dotnet ef` with `dotnet tool install --global dotnet-ef`
* Windows subsystem for Linux (WSL) for Docker, version 4.x or greater
* Docker Desktop to run a RabbitMQ container in production mode
* Node.js for React

Run steps:

1\. Clone the project and open it locally  

2\. Open `OrderManagement.API/appsettings.json` and set your PostgreSQL password  

3\. (Optional) Run `dotnet restore` to install dependencies  

4\. `cd OrderManagement.API/appsettings.json` to give Visual Studio startup context  

5\. Run `dotnet ef database update` to create the initial database and it's tables.   

Note: If you see the logs creating tables, it should have succeeded, but you can double check with `dotnet ef migrations list`, which will connect to the DB and list them from `__EFMigrationsHistory`.

6\. (Optional) Connect to the DB with `pgAdmin`  
(Run as admin, because Windows Smart control sometimes blocks `libpq.dll`, which requires a re-install to fix.)  

7\. (Optional) Inspect the DB to verify that tables were created correctly.

8\. (Production mode only) Navigate to the `../OrderManagementSystem` solution folder, and run `docker compose up -d` to start a `RabbitMQ` service with a persistent `rabbitmq_data` volume.

**Warning: Before you run this step, consider whether you wish to [restrict WSL/Docker disk space usage](#restricting-wsl-docker-disk-space-usage)**

9\. Click the `run` button in `Visual Studio` to start the back-end. 
(Use `https` for Development, and `https-prod` for Production)

10\. `cd Web.React`, then `npm run dev` to run the front-end in development mode

## Accessing the API and Web app

* Localhost only for now
* SwaggerUI (development mode only): `https://localhost:7000/swagger/index.html`
* POC React web app: `http://localhost:5173/`

## Running tests

1\. Run tests with `npm run test`.
2\. Check test coverage with `npm run test:coverage` 

## Resetting migrations 

If there are changes to any of the following:

* `OrderManagement/Domain/Entities/*.cs`
* `OrderManagement/Infrastructure/AppDbContext.cs`
* `OrderManagement/Infrastructure/Migrations/Persistence/*` configurations

Then it will be necessary to reset migrations for fresh clones.
This can be done by running `ResetMigrations.bat` from the solution folder.

*Warning*: Must be done from the solution folder, or the script will fail!

## Restricting WSL/Docker disk space usage

This section is **optional**.

From my experience, WSL/Docker eat up a lot of disk space if left unchecked.
There are some additional configurations I attempted to set to prevent this.

Add a file `%USER_PROFILE%/.wslconfig` with the following contents:

```
[wsl2]
memory=4GB
processors=4
swap=2GB
defaultVhdSize=15GB

[experimental]
sparseVhd=true
```

* I chose to use a 15GB limit for `defaultVhdSize` to prevent it eating up my hard drive. Note that this did not seem to reflect for Docker Desktop, which shows that 1TB is still the cap. Verifying that this setting works as advertised is not a priority at time of writing.
* The use of `sparseVhd=true` requires a minimum WSL version 2.x at minimum. It is supposed to allow the virtual disk to shrink when space is freed, instead of only growing.

## Future work

Planned additions to be explored as time and priorities allow.

### Features

* Translations for user facing text (ongoing)
* Message queue with event driven architecture. In memory first, RabbitMQ later.
* Proper middleware (existing is minimum viable setup)
* Auth with a minimal JSON Web Token (JWT)
* Implement React-Router
* Implement state machine for order status
* Better order submission form (cart management style)
* Deploy to Render

### Tests

* No technical debt (for now)

## Author's professional summary

Timothy Guan is a hands-on technical Senior Software Engineer with 14 years of experience across the full SDLC, delivering enterprise-grade applications and tools in a global multinational environment. His experience covers working within distributed international teams spanning multiple continents and time zones, with a strong background in Agile/Scrum including the Scrum Master role. 

The technical stacks he worked with at his previous employment are React, Typescript, and Java Spring Boot, and also a Java and Quill for the maintenance of essential tooling for product and infrastructure teams.

For the past few years, he has been growing into a senior C#/.NET/ASP.NET Core and React engineering role, with a passion for specializing his career towards full stack development.

In his free time, he develops and maintains a C# application for a niche community. Open to remote, hybrid, or on-site roles in Australia, or fully remote roles globally.
