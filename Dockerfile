# Stage 1: Build React client
FROM node:20-alpine AS client-build
WORKDIR /app/Client
COPY Client/package.json Client/package-lock.json* ./
RUN npm install
COPY Client/ ./
RUN npm run build

# Stage 2: Build .NET API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /src
COPY ClubPlaytime.Api/ClubPlaytime.Api.csproj ClubPlaytime.Api/
RUN dotnet restore ClubPlaytime.Api/ClubPlaytime.Api.csproj
COPY ClubPlaytime.Api/ ClubPlaytime.Api/
# Copy built client into wwwroot before publishing
COPY --from=client-build /app/Client/dist/ ClubPlaytime.Api/wwwroot/
RUN dotnet publish ClubPlaytime.Api/ClubPlaytime.Api.csproj -c Release -o /app/publish --no-restore

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=api-build /app/publish .
ENV ASPNETCORE_URLS=http://+
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "ClubPlaytime.Api.dll"]
