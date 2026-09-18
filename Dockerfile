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

# Railway bills memory by the second, and on this plan memory IS the bill: at
# ~$10 per GB-month, an idle footprint of 150 MB costs ~$1.50/month while 80 MB
# costs ~$0.80. These settings keep the runtime small:
#   gcServer=0        — Server GC keeps a heap per CPU core, which dominates a
#                       container this size; Workstation GC uses one heap.
#   GCConserveMemory  — biases the GC towards a smaller heap over throughput.
#   GCHeapHardLimit   — 96 MB ceiling (0x6000000) so the heap cannot creep up.
#   EnableDiagnostics — drops the diagnostics IPC socket and its buffers.
#   TieredPGO         — cheaper steady-state JIT for a long-running process.
# Container/cgroup limits are still applied by the platform; this only narrows them.
ENV DOTNET_gcServer=0 \
    DOTNET_GCConserveMemory=9 \
    DOTNET_GCHeapHardLimit=0x6000000 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_TieredPGO=1

ENV ASPNETCORE_URLS=http://+
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "ClubPlaytime.Api.dll"]
