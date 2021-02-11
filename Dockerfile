# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source
COPY signer.sln ./
COPY .config/dotnet-tools.json ./.config/

COPY src/Signer.Core/*.fsproj ./src/Signer.Core/
COPY src/Signer.Core/paket.references ./src/Signer.Core/
COPY src/Signer.Service/*.fsproj ./src/Signer.Service/
COPY src/Signer.Service/paket.references ./src/Signer.Service/
COPY tests/Signer.Test/*.fsproj ./tests/Signer.Test/
COPY tests/Signer.Test/paket.references ./tests/Signer.Test/

COPY paket.dependencies ./
COPY paket.lock .

RUN dotnet tool restore
RUN dotnet paket restore -g main

COPY . .
WORKDIR /source/src/Signer.Service
RUN dotnet publish -c release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:5.0
RUN mkdir /Data
VOLUME /Data
ENV LiteDB__Path="/Data/state.db"
WORKDIR /App
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Signer.Service.dll"]
