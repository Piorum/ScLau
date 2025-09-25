#ChatBackend
FROM mcr.microsoft.com/dotnet/sdk@sha256:bb42ae2c058609d1746baf24fe6864ecab0686711dfca1f4b7a99e367ab17162 AS build
WORKDIR /App

COPY . .

WORKDIR /App/

RUN dotnet restore 
RUN dotnet publish -o out

FROM mcr.microsoft.com/dotnet/aspnet@sha256:1af4114db9ba87542a3f23dbb5cd9072cad7fcc8505f6e9131d1feb580286a6f
WORKDIR /App
COPY --from=build /App/out .
ENTRYPOINT [ "dotnet", "ChatBackend.dll" ]