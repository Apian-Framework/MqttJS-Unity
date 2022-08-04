using System;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // for JObject
using UniLog;
using UnityEngine;
using MqttJsUnity;
using P2pNet;


    public class P2pNetMqttJs : IP2pNetCarrier, IMqttClientOwner
    {
       class JoinState
        {
            public SynchronizationContext mainSyncCtx; // might be null
            public IP2pNetBase p2pBase;
            public P2pNetChannelInfo mainChannel;
            public string localHelloData;
            public bool IsConnected;
            public bool HasPeers; // are we connected to anyone subscribed to the main channel?
        };

        private JoinState joinState;

        protected IMqttClient mqttInst;

        private readonly Dictionary<string,string> connectOpts;


        public UniLogger logger;

        public static JObject defMtqqOpts =  new JObject {
            {"username", "xxx"},
            {"password", "yyy"},
            {"clean", true},
            {"connectTimeout", 750},
            {"keepalive", 1000},
            {"reconnectPeriod", 500},
            {"queueQoSZero", true}  // will queue up outgoing Qos0 messages if disconnected and send on reconnect
        };


        public P2pNetMqttJs( string _connectionString)
        {
            logger = UniLogger.GetLogger("P2pNet");

            // ConnectOpts:
            // {
            //    url:
            //    user:
            //    pwd:
            // }
            //

            connectOpts = JsonConvert.DeserializeObject<Dictionary<string,string>>(_connectionString);

            mqttInst = WebGLMqttJSLib.Create(this);

            ResetJoinVars();
        }

        private void ResetJoinVars()
        {
            joinState = null;
        }

        public void Join(P2pNetChannelInfo mainChannel, IP2pNetBase p2pBase, string localHelloData)
        {
            ResetJoinVars();

            String url = connectOpts["url"];

            JObject mqttOptsObj = new JObject(defMtqqOpts);
            // replace these 2:
            mqttOptsObj["username"] = new JValue(connectOpts["user"]);
            mqttOptsObj["password"] = new JValue(connectOpts["pwd"]);

            string mqttOptsJson = mqttOptsObj.ToString();


            joinState = new JoinState()
            {
                p2pBase=p2pBase,
                mainChannel=mainChannel,
                localHelloData=localHelloData,
                mainSyncCtx = SynchronizationContext.Current,
                HasPeers = false,
                IsConnected = false
            };

            mqttInst.Connect( url, mqttOptsJson);
        }


        public void Leave()
        {
            mqttInst.End();
            ResetJoinVars();
        }

        public void Send(P2pNetMessage msg)
        {
            string msgJSON = JsonConvert.SerializeObject(msg);
            mqttInst.Publish(msg.dstChannel, msgJSON);
        }

        public void Listen(string channel)
        {
            mqttInst.Subscribe(channel);
        }

        public void StopListening(string channel)
        {
            mqttInst.Unsubscribe(channel);
        }

        public void AddReceiptTimestamp(P2pNetMessage msg)
        {
            msg.rcptTime = P2pNetDateTime.NowMs;
        }

        public void Poll() {}

        // IMqttClientOwner API

        public void OnError(string errJson)
        {
            logger.Error(errJson);
        }

        public void OnConnect(bool success, string conAckJson = null)
        {
            Listen(joinState.p2pBase.GetId()); // listen to our p2pId channel
            joinState.IsConnected = true;
            // OnNetworkJoined needs to be synchronized
            if (joinState.mainSyncCtx != null)
            {
                joinState.mainSyncCtx.Post( new SendOrPostCallback( (o) => {
                    joinState.p2pBase.OnNetworkJoined(joinState.mainChannel, joinState.localHelloData);
                } ), null);
            } else {
                joinState.p2pBase.OnNetworkJoined(joinState.mainChannel, joinState.localHelloData);
            }
        }


        public void OnEnd()
        {
            logger.Info($"MqttJs OnEnd()");
        }
        public void OnMessage(string topic, string message, string jsonPayload = null)
        {
            logger.Verbose($"P2pNetMqttJs OnMessage() thread: {Environment.CurrentManagedThreadId}");
            P2pNetMessage msg = JsonConvert.DeserializeObject<P2pNetMessage>(message); // "message" is the json-encoded message data

            AddReceiptTimestamp(msg);

            if (joinState.mainSyncCtx != null)
            {
                joinState.mainSyncCtx.Post( new SendOrPostCallback( (o) => {
                    joinState.p2pBase.OnReceivedNetMessage(msg.dstChannel, msg);
                } ), null);
            } else {
                joinState.p2pBase.OnReceivedNetMessage(msg.dstChannel, msg);
            }
        }


        public void OnDisconnect() { }// either requested or not
        public void OnOffline() { } // local client is actually offline? Not sure.


    }

