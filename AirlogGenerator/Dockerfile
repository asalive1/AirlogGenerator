FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Install required native dependencies
RUN apt-get update && apt-get install -y \
    libicu-dev \
    libssl-dev \
    libkrb5-3 \
    && rm -rf /var/lib/apt/lists/*

COPY . .

RUN mkdir -p /app/CONFIG /app/LOG

RUN chmod -R 777 /app/CONFIG /app/LOG

EXPOSE 8030

ENTRYPOINT ["dotnet", "AirlogGenerator.dll"]