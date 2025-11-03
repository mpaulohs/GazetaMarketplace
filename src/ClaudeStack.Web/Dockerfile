# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["sln.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]
COPY ["src/ClaudeStack.Web/ClaudeStack.Web.csproj", "src/ClaudeStack.Web/"]

# Restore dependencies
RUN dotnet restore "src/ClaudeStack.Web/ClaudeStack.Web.csproj"

# Copy source code
COPY ["src/ClaudeStack.Web/", "src/ClaudeStack.Web/"]

# Build application
WORKDIR "/src/src/ClaudeStack.Web"
RUN dotnet build "ClaudeStack.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ClaudeStack.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos "" --uid 1000 appuser && \
    chown -R appuser:appuser /app
USER appuser

# Copy published application
COPY --from=publish --chown=appuser:appuser /app/publish .

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Start application
ENTRYPOINT ["dotnet", "ClaudeStack.Web.dll"]
