FROM node:18
LABEL authors="Jeppevinkel"

RUN apt-get -y update && apt-get -y upgrade && apt-get install -y --no-install-recommends ffmpeg

# Create app directory
WORKDIR /usr/src/app

# Install app dependencies
# A wildcard is used to ensure both package.json AND package-lock.json are copied
# where available (npm@5+)
COPY package*.json ./

RUN npm ci --omit=dev

# Bundle app source
COPY . .

RUN tsc

CMD [ "npm", "run", "start" ]