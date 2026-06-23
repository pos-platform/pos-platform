# PosPlatform — backend + frontend bitta image ichida (multi-stage).
# Build konteksti loyiha ILDIZI bo'lishi kerak (frontend va backend ham kerak).
#   docker build -f deploy/Dockerfile -t posplatform:latest .
# yoki docker-compose.yml orqali (context: ..).

# --- 1-bosqich: Frontend (Vue) build ---
FROM node:20-alpine AS frontend
WORKDIR /app/frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
# Production'da API nisbiy (/) — .env.production allaqachon VITE_API_BASE=/ qo'ygan.
RUN npm run build
# Natija: /app/frontend/dist

# --- 2-bosqich: Backend (.NET) publish ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend
WORKDIR /src
COPY backend/ ./backend/
WORKDIR /src/backend
RUN dotnet restore src/PosPlatform.Api/PosPlatform.Api.csproj
RUN dotnet publish src/PosPlatform.Api/PosPlatform.Api.csproj -c Release -o /publish
# Vue build'ni wwwroot ichiga joylaymiz (SPA shu yerdan beriladi)
COPY --from=frontend /app/frontend/dist /publish/wwwroot

# --- 3-bosqich: Runtime (kichik image) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=backend /publish ./
# Yuklangan rasmlar/uploads uchun papka (volume bilan saqlash tavsiya etiladi)
RUN mkdir -p /app/wwwroot/uploads
EXPOSE 5000
ENV ASPNETCORE_URLS=http://0.0.0.0:5000 \
    ASPNETCORE_ENVIRONMENT=Production
# Sirlar (Jwt__Key, ConnectionStrings__*, Seed__*, Agent__ApiKey) — run paytida
# environment / docker-compose orqali beriladi (image ichiga yozilmaydi).
ENTRYPOINT ["dotnet", "PosPlatform.Api.dll"]
