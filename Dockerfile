FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AirlogGenerator.csproj ./
RUN dotnet restore AirlogGenerator.csproj

COPY . .
RUN dotnet publish AirlogGenerator.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update && apt-get install -y \
    libicu-dev \
    libssl-dev \
    libkrb5-3 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN mkdir -p /app/CONFIG /app/LOG \
    && chmod -R 775 /app/CONFIG /app/LOG

EXPOSE 8030

ENTRYPOINT ["dotnet", "AirlogGenerator.dll"]