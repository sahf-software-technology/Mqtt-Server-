# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# FIX 1: Copy the single project file into the expected build directory first
# This ensures caching works and the file is definitely present.
COPY src/RestApiLayer/RestApiLayer.csproj src/RestApiLayer/

# Restore dependencies - use the full path from /src
RUN dotnet restore src/RestApiLayer/RestApiLayer.csproj

# Copy the rest of the source code (this line is correctly copying from the repo to the container)
COPY src/RestApiLayer/. src/RestApiLayer/

# NOTE: We keep WORKDIR at /src to use consistent full paths below, removing ambiguity.

# FIX 2: Use the full path for the publish command
# This resolves path ambiguity by explicitly telling dotnet where the project is.
RUN dotnet publish src/RestApiLayer/RestApiLayer.csproj -c Release -o /app/publish

# Stage 2: Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The application runs on port 8080 inside the container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RestApiLayer.dll"]
