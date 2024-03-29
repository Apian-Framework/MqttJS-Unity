
var MqttJS_API = {
  $dependencies:{},

  MqttJS_InitCallbacks__postset: 'var ClientInstances = { }; var cs_callbacks = { };',
  MqttJS_InitCallbacks: function( onConnectCb_ptr, onMsgCb_ptr, onCloseCb_ptr, onOfflineCb_ptr, onErrorCb_ptr, onEndCb_ptr)
  {
    console.log(`MqttJS_InitCallbacks()`)
    cs_callbacks["connect"] = onConnectCb_ptr;
    cs_callbacks['message'] = onMsgCb_ptr;
    cs_callbacks['close'] = onCloseCb_ptr;
    cs_callbacks['offline'] = onOfflineCb_ptr;
    cs_callbacks['error'] = onErrorCb_ptr;
    cs_callbacks['end'] = onEndCb_ptr;
  },

  MqttJS_Connect: function(c_clientId, c_url, c_optsJson)
  {
    // Create client instance
    console.log(`MqttJS_Connect() - entry`)
    var clientId = UTF8ToString(c_clientId)
    var url = UTF8ToString(c_url)
    var opts = JSON.parse(UTF8ToString(c_optsJson))

    console.log(`MqttJS_Connect() clientId: ${clientId}`)
    console.log(`MqttJS_Connect() opts: ${UTF8ToString(c_optsJson)}`)

    try {
      var client = mqtt.connect(url, opts)
    } catch (error) {
      console.log(`connect exception - entry`)
      var errorJson = JSON.stringify(error)
      console.log(`error: ${errorJson}`)
      var c_errorJson = allocate(intArrayFromString(errorJson), ALLOC_NORMAL)
      dynCall_viii( cs_callbacks["error"], c_clientId, c_errorJson, false) // no client yet so can't be reconnecting
      _free(c_errorJson)
      console.log(`connect exception - exit`)
      console.log(`MqttJS_Connect() - exit(false) after exception `)
      return false;
    }

    if (!client)
    {
      console.log(`MqttJS_Connect() - exit (false)`)
      return false;
    }

    ClientInstances[clientId] = client;

    client.on('error', function (error) {
      console.log(`on:error - entry`)
      var cs_clientId = allocate(intArrayFromString(clientId))
      var errorJson = JSON.stringify(error)
      console.log(`error:${errorJson}`)
      var c_errorJson = allocate(intArrayFromString(errorJson), ALLOC_NORMAL)
      var reconnecting = client.reconnecting;
      dynCall_viii( cs_callbacks["error"], cs_clientId, c_errorJson, reconnecting)
      _free(cs_clientId)
      _free(c_errorJson)
      console.log(`on:error - exit`)

    })

    client.stream.on('error', function (error) {
      console.log(`on:stream.error - entry`)
      var cs_clientId = allocate(intArrayFromString(clientId))
      var errorJson = JSON.stringify(error)
      console.log(`error:${errorJson}`)
      var c_errorJson = allocate(intArrayFromString(errorJson), ALLOC_NORMAL)
      var reconnecting = client.reconnecting;
      dynCall_viii( cs_callbacks["error"], cs_clientId, c_errorJson, reconnecting)
      _free(cs_clientId)
      _free(c_errorJson)
      console.log(`on:stream.error - exit`)
    })

    // TODO: I think this is not ever called? Or not useful if it is?
    client.stream.on('close', function (done) {
      console.log(`on:stream.close - entry`)
      console.log(`on:stream.close - done: ${done}`)
      console.log(`on:stream.close - exit`)
    })

    client.on('connect', function (connAck) {
      console.log(`on:connect - entry`)
      var cs_clientId = allocate(intArrayFromString(clientId))
      var connAckJson = JSON.stringify(connAck)
      var c_connAckJson = allocate(intArrayFromString(connAckJson), ALLOC_NORMAL)
      dynCall_vii( cs_callbacks["connect"], cs_clientId, c_connAckJson)
      _free(cs_clientId)
      _free(c_connAckJson)
      console.log(`on:connect - exit`)
    })

    client.on('message', function (topic, payload, packet) {
      // console.log(`on:message - entry: topic: ${topic} payload: ${payload} packet: ${packet}`)
      var cs_clientId = allocate(intArrayFromString(clientId)) // can't just use c_clientId from containing scope because it's a c#-land ref
      var c_topic = allocate(intArrayFromString(topic), ALLOC_NORMAL)
      var c_msgStr = allocate(intArrayFromString(payload.toString()), ALLOC_NORMAL)
      var packetJson = JSON.stringify(packet)
      var c_packetJson = allocate(intArrayFromString(packetJson), ALLOC_NORMAL)

      dynCall_viiii( cs_callbacks['message'], cs_clientId, c_topic, c_msgStr, c_packetJson)

      _free(cs_clientId)
      _free(c_topic)
      _free(c_msgStr)
      _free(c_packetJson)
      // console.log(`on:message - exit`)
      return true;
    })

    client.on('close', function () {
      // Emitted after a disconnection.
      // ...or apparently a failed connection, too?
      console.log(`client.onClose()`)
      var cs_clientId = allocate(intArrayFromString(clientId))
      var reconnecting = client.reconnecting;
      dynCall_vii( cs_callbacks['close'], cs_clientId, reconnecting)
       _free(cs_clientId)
    })

    client.on('end', function () {
      // client.end() has been called and everything closed
      console.log(`client.onEnd()`)
      var cs_clientId = allocate(intArrayFromString(clientId))
       dynCall_vi( cs_callbacks['end'], cs_clientId)
       _free(cs_clientId)
    })

    client.on('disconnect', function () {
      // v5 disconnect packet rcvd
      console.log(`client.onDisconnect()`)
    })


    client.on('offline', function () {
      // client is offline (like network is down?)
      console.log(`client.onOffline()`)
      var cs_clientId = allocate(intArrayFromString(clientId))
      dynCall_vi( cs_callbacks['offline'], cs_clientId)
      _free(cs_clientId)
    })

    console.log(`MqttJS_Connect() - exit (true)`)

  },

  MqttJS_Delete: function (c_clientId)
  {
    // client instance is only removed when asked by C#
    var clientId = UTF8ToString(c_clientId)
    client = ClientInstances[clientId]
    if (client)
      delete ClientInstances[clientId]
  },

  MqttJS_Subscribe: function (c_clientId, c_topic, c_optsJson )
  {
    console.log(`MqttJS_Subscribe() - entry`)
    var clientId = UTF8ToString(c_clientId)
    client = ClientInstances[clientId]
    var topic = UTF8ToString(c_topic)
    console.log(`MqttJS_Subscribe() topic: ${topic}`)
    if (c_optsJson === 0) // passed-in strings are addreses. null is 0
      client.subscribe(topic)
    else {
      var opts = JSON.parse(UTF8ToString(c_optsJson))
      client.subscribe(topic, opts);
    }
    console.log(`MqttJS_Subscribe() - exit`)
  },

  MqttJS_Unsubscribe: function (c_clientId, c_topic, c_optsJson)
  {
    console.log(`MqttJS_Unsubscribe() - entry`)
    var clientId = UTF8ToString(c_clientId)
    client = ClientInstances[clientId]
    var topic = UTF8ToString(c_topic)
    console.log(`MqttJS_Unsubscribe() topic: ${topic}`)
    if (c_optsJson === 0 )
      client.unsubscribe(topic);
    else {
      var opts = JSON.parse(UTF8ToString(c_optsJson))
      client.unsubscribe(topic, opts);
    }
    console.log(`MqttJS_Unsubscribe() - exit`)
  },

  MqttJS_Publish: function (c_clientId,  c_topic, c_msg, c_optsJson)
  {
    // console.log(`MqttJS_Publish() - entry`)
    var clientId = UTF8ToString(c_clientId)
    client = ClientInstances[clientId]
    var topic = UTF8ToString(c_topic)
    var msg = UTF8ToString(c_msg);
    if (c_optsJson === 0)
      client.publish(topic, msg);
    else {
      var opts = JSON.parse(UTF8ToString(c_optsJson))
      client.publish(topic, msg, opts);
    }
    // console.log(`MqttJS_Publish() - exit`)
  },

  MqttJS_End: function (c_clientId)
  {
    var clientId = UTF8ToString(c_clientId)
    console.log(`MqttJS_End() - clientId: ${clientId}`)
    client = ClientInstances[clientId]
    if (client)
      client.end();
    else
      console.log(`MqttJS_End() - No client found.`)
  },

 }
 autoAddDeps(MqttJS_API,'$dependencies')
 mergeInto(LibraryManager.library,MqttJS_API)
