# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies first (for layer caching)
# We copy it into a path structure that mirrors the source directory
COPY src/RestApiLayer/*.csproj src/RestApiLayer/
RUN dotnet restore src/RestApiLayer/RestApiLayer.csproj

# Copy the rest of the source code
COPY src/RestApiLayer/. src/RestApiLayer/

# Change directory to the folder containing the project file
WORKDIR /src/RestApiLayer

# FIX: Explicitly specify the project file to publish (RestApiLayer.csproj)
RUN dotnet publish RestApiLayer.csproj -c Release -o /app/publish

# Stage 2: Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The application runs on port 8080 inside the container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RestApiLayer.dll"]
