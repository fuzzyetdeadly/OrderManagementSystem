@echo off

echo Discarding existing migrations...
if exist "OrderManagement.Infrastructure\Migrations" (
    rd /s /q "OrderManagement.Infrastructure\Migrations"
    echo Migrations folder deleted.
) else (
    echo No migrations folder found, skipping.
)

echo Navigating to API project...
cd OrderManagement.API

echo Dropping database...
dotnet ef database drop

echo Adding initial migration...
dotnet ef migrations add InitialCreate --project ../OrderManagement.Infrastructure/

echo Applying migration...
dotnet ef database update

echo Done.
pause