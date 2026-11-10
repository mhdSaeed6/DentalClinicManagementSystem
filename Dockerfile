# ----- Build Stage -----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build

# Copy project files for restore
COPY ["src/DentalClinic.Api/DentalClinic.Api.csproj", "src/DentalClinic.Api/"]
COPY ["src/DentalClinic.Application/DentalClinic.Application.csproj", "src/DentalClinic.Application/"]
COPY ["src/DentalClinic.Domain/DentalClinic.Domain.csproj", "src/DentalClinic.Domain/"]
COPY ["src/DentalClinic.Contracts/DentalClinic.Contracts.csproj", "src/DentalClinic.Contracts/"]
COPY ["src/DentalClinic.Infrastructure/DentalClinic.Infrastructure.csproj", "src/DentalClinic.Infrastructure/"]
COPY ["src/DentalClinic.Web/DentalClinic.Web.csproj", "src/DentalClinic.Web/"]
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]

# Restore dependencies (only once)
RUN dotnet restore "src/DentalClinic.Api/DentalClinic.Api.csproj"
RUN dotnet restore "src/DentalClinic.Web/DentalClinic.Web.csproj"

# Copy all source code
COPY . .

# Publish both apps
RUN dotnet publish "src/DentalClinic.Api/DentalClinic.Api.csproj" -c Release -o /app
RUN dotnet publish "src/DentalClinic.Web/DentalClinic.Web.csproj" -c Release -o /web

# ----- Final Stage (API) -----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# tzdata: TimeZoneInfo support for the clinic timezone. curl: container
# healthchecks (the runtime image ships neither).
# The timezone is applied at runtime via the TZ environment variable rather
# than baked into the image, so the same image can be deployed to a clinic in
# any region. The default matches the clinic's operating zone.
RUN apt-get update && apt-get install -y --no-install-recommends tzdata curl && \
    dpkg-reconfigure -f noninteractive tzdata && \
    rm -rf /var/lib/apt/lists/*

ENV TZ=Asia/Damascus
ENV ASPNETCORE_URLS=http://+:80

WORKDIR /app
COPY --from=build /app .

# Clinical images live outside wwwroot (Images__StoragePath) and the Data
# Protection key ring is persisted on a volume so antiforgery tokens survive a
# container restart (DataProtection:KeysPath). Create both directories and hand
# them to the image's non-root "app" user (UID 1654) so the volumes work
# unchanged when the container is switched to USER app.
RUN mkdir -p /app/App_Data/images /app/App_Data/keys && chown -R app:app /app/App_Data

EXPOSE 80
ENTRYPOINT ["dotnet", "DentalClinic.Api.dll"]

# ----- Web Stage -----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS web

RUN apt-get update && apt-get install -y --no-install-recommends tzdata curl && \
    dpkg-reconfigure -f noninteractive tzdata && \
    rm -rf /var/lib/apt/lists/*

ENV TZ=Asia/Damascus
ENV ASPNETCORE_URLS=http://+:80

WORKDIR /app
COPY --from=build /web .

# Same volume layout as the API stage: the Data Protection key ring must be
# writable by the app user so antiforgery tokens survive container recreation.
RUN mkdir -p /app/App_Data/keys && chown -R app:app /app/App_Data

EXPOSE 80
ENTRYPOINT ["dotnet", "DentalClinic.Web.dll"]