# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ChatRoleplay/ChatRoleplay.csproj", "ChatRoleplay/"]
RUN dotnet restore "ChatRoleplay/ChatRoleplay.csproj"

COPY . .
WORKDIR "/src/ChatRoleplay"
RUN dotnet build "ChatRoleplay.csproj" -c Release -o /app/build

# ── Publish stage ─────────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "ChatRoleplay.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
LABEL org.opencontainers.image.source="https://github.com/jeppevinkel/Chat-Roleplay"
LABEL org.opencontainers.image.description="Chat-Roleplay Discord bot (.NET edition)"

WORKDIR /app
COPY --from=publish /app/publish .

# Persistent data directory (mount a volume here)
RUN mkdir -p /app/data

ENTRYPOINT ["dotnet", "ChatRoleplay.dll"]
