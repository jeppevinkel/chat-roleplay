FROM node:22 AS build

# Create app directory
WORKDIR /usr/src/app

# Install app dependencies
COPY package*.json .
RUN npm install

# Bundle app source
COPY . .

# Build typescript
RUN npx tsc

FROM node:22 AS production
LABEL authors="Jeppevinkel"

# Create app directory
WORKDIR /usr/src/app

# Install app dependencies
COPY package*.json ./
RUN npm ci --omit=dev

# Bundle app dist
COPY --from=build /usr/src/app/dist ./dist

CMD [ "npm", "run", "start" ]