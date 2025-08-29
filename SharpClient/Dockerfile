#OllamaTest
FROM mcr.microsoft.com/dotnet/sdk AS build
WORKDIR /App

COPY . .

WORKDIR /App/

RUN dotnet restore 
RUN dotnet publish -o out

FROM mcr.microsoft.com/dotnet/runtime
WORKDIR /App
COPY --from=build /App/out .
ENTRYPOINT [ "dotnet", "OllamaTest.dll" ]