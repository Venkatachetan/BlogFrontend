#–– Stage 1: Build the Blazor WebAssembly app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /opt/render/project

# Copy solution and project file into the container
COPY BlogFrontend.sln ./
COPY BlogFrontend/BlogFrontend.csproj BlogFrontend/

# Restore NuGet packages
WORKDIR /opt/render/project/BlogFrontend
RUN dotnet restore

# Copy the rest of your source code and publish
COPY BlogFrontend/. ./
RUN dotnet publish -c Release -o /app/publish --no-restore

#–– Stage 2: Serve the published files with Nginx
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Clear default content, then bring in your Blazor static files
RUN rm -rf ./*
COPY --from=build /app/publish/wwwroot .

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
