# Stage 1: Build the React app
FROM node:25-alpine@sha256:f4769ca6eeb6ebbd15eb9c8233afed856e437b75f486f7fccaa81d7c8ad56007 AS build

WORKDIR /app

COPY package.json ./
COPY package-lock.json ./
RUN npm install

COPY . ./
RUN npm run build

# Stage 2: Serve the app with Nginx
FROM nginx:alpine@sha256:8491795299c8e739b7fcc6285d531d9812ce2666e07bd3dd8db00020ad132295

COPY --from=build /app/build /usr/share/nginx/html

COPY nginx.conf /etc/nginx/conf.d/default.conf

# When the container starts, Nginx will serve the files from /usr/share/nginx/html
