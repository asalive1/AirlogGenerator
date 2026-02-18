# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create expected directories inside container
RUN mkdir -p /app/CONFIG /app/LOG

COPY --from=build /app/publish .

# Expose web port (only used when EnableWebUi = true)
EXPOSE 8030

ENTRYPOINT ["dotnet", "AirlogGenerator.dll"]