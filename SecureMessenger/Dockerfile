
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish --no-restore -c Debug -o out

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app
COPY --from=build /src/out .
ENV ASPNETCORE_URLS=http://0.0.0.0:11000
ENTRYPOINT ["dotnet", "SecureMessenger.dll"]