# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy project file and restore dependencies
COPY InvestmentTracker/*.csproj InvestmentTracker/
RUN dotnet restore InvestmentTracker/InvestmentTracker.csproj

# Copy all source code
COPY . .

# Build and publish the project
RUN dotnet publish InvestmentTracker/InvestmentTracker.csproj -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=build /app .

# Create directory for SQLite database and logs
RUN mkdir -p /app/data /app/logs

# Set permissions for SQLite database
RUN chown -R app:app /app
USER app

# Expose port
EXPOSE 5000

# Environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000 || exit 1

# Start the application
ENTRYPOINT ["dotnet", "InvestmentTracker.dll"]
