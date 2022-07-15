
## Building `browserMqtt.js` for use by Unity WebGL.

This is taken pretty much straight from [the Mqtt.js GitHub page](https://github.com/mqttjs/MQTT.js#browser)

```
cd [this dir]
npm install
cd node_modules/mqtt/
npm install .
npx browserify mqtt.js -s mqtt >browserMqtt.js // use script tag
```

Note that it's built in  `node_modules/mqtt/`. I thought about changing that, but decided it was better to keep things as close as possible to the original docs.

