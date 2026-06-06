FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy everything and restore only the API project.
COPY . ./
RUN dotnet restore BaseOps.API/BaseOps.API.csproj

# Publish the API project.
RUN dotnet publish BaseOps.API/BaseOps.API.csproj -c Release -o /app/out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 3000
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-3000} dotnet BaseOps.API.dll"]
