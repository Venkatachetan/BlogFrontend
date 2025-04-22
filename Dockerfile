#–– Stage 1: Build the Blazor WASM app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy the solution and project files
COPY BlogFrontend.sln ./
COPY BlogFrontend/BlogFrontend.csproj BlogFrontend/

# Restore all NuGet packages
RUN dotnet restore

# Copy the full source tree and publish the frontend project
COPY . ./
WORKDIR /src/BlogFrontend
RUN dotnet publish -c Release -o /app/publish --no-restore

#–– Stage 2: Serve the published files with Nginx
FROM nginx:alpine

# Remove default static assets and copy in ours
WORKDIR /usr/share/nginx/html
RUN rm -rf ./*
COPY --from=build /app/publish/wwwroot .

# (Optional) If you need SPA‑style fallback routing, uncomment:
# COPY BlogFrontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
