
    # Stage 1: Build the application
    FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
    WORKDIR /source

    # Copy project files and restore dependencies
    COPY *.sln .
    COPY LibraryManagement.API/*.csproj ./LibraryManagement.API/
    COPY LibraryManagement.Web/*.csproj ./LibraryManagement.Web/
    RUN dotnet restore

    # Copy the rest of the source code
    COPY . .

    # Publish ONLY the API project
    WORKDIR /source/LibraryManagement.API
    RUN dotnet publish -c Release -o /app/publish

    # Stage 2: Create the final runtime image
    FROM mcr.microsoft.com/dotnet/aspnet:8.0
    WORKDIR /app
    COPY --from=build /app/publish .

    # Configure the server port
    EXPOSE 8080
    ENV ASPNETCORE_URLS=http://+:8080

    # Command to run the application
    ENTRYPOINT ["dotnet", "LibraryManagement.API.dll"]
  