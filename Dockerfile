# StressLab Performance Testing Tool - Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set working directory
WORKDIR /src

# Copy project files
COPY StressLab.sln ./
COPY src/StressLab.Core/StressLab.Core.csproj src/StressLab.Core/
COPY src/StressLab.Application/StressLab.Application.csproj src/StressLab.Application/
COPY src/StressLab.Infrastructure/StressLab.Infrastructure.csproj src/StressLab.Infrastructure/
COPY src/StressLab.Api/StressLab.Api.csproj src/StressLab.Api/
COPY tests/StressLab.PerformanceTests/StressLab.PerformanceTests.csproj tests/StressLab.PerformanceTests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build the application
RUN dotnet build -c Release --no-restore

# Publish the performance test project
RUN dotnet publish tests/StressLab.PerformanceTests/StressLab.PerformanceTests.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime

# Install additional tools for monitoring
RUN apt-get update && apt-get install -y \
    procps \
    htop \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r stresslab && useradd -r -g stresslab stresslab

# Create directories
RUN mkdir -p /app/logs /app/reports /app/scenarios && \
    chown -R stresslab:stresslab /app

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Copy scenarios and configuration
COPY tests/StressLab.PerformanceTests/scenarios*.json ./scenarios/
COPY tests/StressLab.PerformanceTests/appsettings*.json ./

# Set ownership
RUN chown -R stresslab:stresslab /app

# Switch to non-root user
USER stresslab

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Expose port for potential web interface
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Default command
ENTRYPOINT ["dotnet", "StressLab.PerformanceTests.dll"]

# Default arguments
CMD ["--help"]

