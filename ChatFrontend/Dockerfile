#ChatFrontend
FROM mcr.microsoft.com/dotnet/sdk AS build

#Reqiured for wasm build
RUN dotnet workload install wasm-tools
RUN apt-get update && apt-get install -y python3

WORKDIR /App

COPY . .

WORKDIR "/App/ChatFrontend.Browser"

RUN dotnet restore
RUN dotnet publish -c Release -o /App/out

FROM nginx:alpine AS final

COPY --from=build /App/out/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80