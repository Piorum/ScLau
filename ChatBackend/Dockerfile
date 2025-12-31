#ChatBackend
FROM mcr.microsoft.com/dotnet/sdk@sha256:d1823fecac3689a2eb959e02ee3bfe1c2142392808240039097ad70644566190 AS build
WORKDIR /App

COPY . .

WORKDIR /App/

RUN dotnet restore 
RUN dotnet publish -o out

FROM mcr.microsoft.com/dotnet/aspnet@sha256:eaa79205c3ade4792a7f7bf310a3aac51fe0e1d91c44e40f70b7c6423d475fe0
WORKDIR /App
COPY --from=build /App/out .
ENTRYPOINT [ "dotnet", "ChatBackend.dll" ]