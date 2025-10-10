# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies first (for layer caching)
COPY src/RestApiLayer/*.csproj src/RestApiLayer/
RUN dotnet restore src/RestApiLayer/RestApiLayer.csproj

# Copy the rest of the source code
COPY src/RestApiLayer/. src/RestApiLayer/
WORKDIR /src/RestApiLayer
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The application runs on port 8080 inside the container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RestApiLayer.dll"]
