FROM node:20-alpine AS build
WORKDIR /app

COPY ui/package.json ./package.json
COPY ui/tsconfig.json ./tsconfig.json
COPY ui/vite.config.ts ./vite.config.ts
COPY ui/index.html ./index.html
COPY ui/src ./src

RUN npm install

EXPOSE 5173
CMD ["npm", "run", "dev"]

