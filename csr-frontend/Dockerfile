FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
ARG VITE_LIFF_ID=
ARG VITE_USE_MOCK_LIFF=false
ENV VITE_LIFF_ID=$VITE_LIFF_ID
ENV VITE_USE_MOCK_LIFF=$VITE_USE_MOCK_LIFF
RUN npm run build

FROM nginx:1.27-alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=build /app/dist .
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
