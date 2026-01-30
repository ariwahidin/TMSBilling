# Step 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj dan restore
COPY *.csproj ./
RUN dotnet restore

# Copy semua source code dan publish
COPY . ./
RUN dotnet publish -c Release -o out

# Step 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Expose port
EXPOSE 80

# Jalankan .dll hasil publish
ENTRYPOINT ["dotnet", "TMSBilling.dll"]
