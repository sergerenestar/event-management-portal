# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/EventPortal.Api/EventPortal.Api.csproj", "src/EventPortal.Api/"]
RUN dotnet restore "src/EventPortal.Api/EventPortal.Api.csproj"

COPY . .
WORKDIR "/src/src/EventPortal.Api"
RUN dotnet publish "EventPortal.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventPortal.Api.dll"]
