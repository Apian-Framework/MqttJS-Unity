using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

#if UNITY_WEBGL
using System.Runtime.InteropServices;
using AOT;
#endif

namespace MqttJsUnity
{
    public interface IMqttClientOwner
    {
        void OnError(string errJson);
        void OnConnect(bool success, string conAckJson = null);
        void OnEnd(); // intentional shutdown complete
        void OnMessage(string topic, string message, string jsonPayload = null);
        void OnDisconnect(); // either requested or not
        void OnOffline(); // local client is actually offline? Not sure.
    }

    public interface IMqttClient
    {
        void Connect (string url, string optsJson);
        void Subscribe(string topic, string optsJson=null);
        void Unsubscribe(string topic, string optJson=null);
        void Publish(string topic, string msg, string optsJson=null);
        void End(); // shut it down
    }

    public class WebGLMqttJSLib : IMqttClient
    {

#if UNITY_WEBGL
        //
        // Static stuff
        //

       public static Dictionary<string, WebGLMqttJSLib> MqttInstances;

         //
        // Emscripten-compiled Javascript functions
        [DllImport("__Internal")]
        private static extern void MqttJS_InitCallbacks(
            Action<string,string> connectCb,
            Action<string, string, string, string> msgCb,
            Action<string, bool> closeCb,
            Action<string> offlineCb,
            Action<string,string, bool> onErrorCb,
            Action<string> endCb);

        [DllImport("__Internal")]
        private static extern bool MqttJS_Connect( string instId, string url, string optsJson);

        [DllImport("__Internal")]
        private static extern bool MqttJS_Delete( string instId);

        [DllImport("__Internal")]
        private static extern void MqttJS_Subscribe(string c_clientId, string c_topic, string c_optsJson);

         [DllImport("__Internal")]
        private static extern void MqttJS_Unsubscribe(string c_clientId, string c_topic, string optsJson);

        [DllImport("__Internal")]
        private static extern void MqttJS_Publish(string c_clientId, string c_topic, string c_msg, string c_optsJson);

        [DllImport("__Internal")]
        private static extern void MqttJS_End(string c_clientId);



        // Static ctor sets up JS callback pointers
        static WebGLMqttJSLib()
        {
            MqttInstances = new Dictionary<string, WebGLMqttJSLib>();
            MqttJS_InitCallbacks( MqttJS_OnConnect, MqttJS_OnMessage, MqttJS_OnClose, MqttJS_OnOffline, MqttJS_OnError, MqttJS_OnEnd);
        }


        // Static callbacks from JSLib
        [MonoPInvokeCallback(typeof(Action<string, string>))]
        public static void MqttJS_OnConnect( string clientId, string connAckJson)
        {
            Debug.Log($"MqttJS_OnConnect() - clientId: {clientId}, connAckJson: {connAckJson}");
            if (MqttInstances.ContainsKey(clientId))
                MqttInstances[clientId].OnConnect(connAckJson);
            else
                Debug.LogError($"MqttJS_OnConnect() - clientId: {clientId} not found in MqttInstances");
        }

        [MonoPInvokeCallback(typeof(Action<string, string, string, string>))]
        public static void MqttJS_OnMessage( string clientId, string topic, string message, string msgJson)
        {
            // Debug.Log($"MqttJS_OnMessage() - clientId: {clientId}, topic: {topic}, message: {message}");
            if (MqttInstances.ContainsKey(clientId))
                MqttInstances[clientId].OnMessage(topic, message, msgJson);
            else
                Debug.LogError($"MqttJS_OnMessage() - clientId: {clientId} not found in MqttInstances");
        }

        [MonoPInvokeCallback(typeof(Action<string, bool>))]
        public static void MqttJS_OnClose( string clientId, bool isReconnecting)
        {
            Debug.Log($"MqttJS_OnClose() - clientId: {clientId}");
            if (MqttInstances.ContainsKey(clientId))
                MqttInstances[clientId].OnClose(isReconnecting);
            else
                Debug.LogError( $"MqttJS_OnClose() - clientId: {clientId} not found in MqttInstances");
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void MqttJS_OnOffline( string clientId)
        {
            Debug.Log($"MqttJS_OnOffline() - clientId: {clientId}");
            if (MqttInstances.ContainsKey(clientId))
                MqttInstances[clientId].OnOffline();
            else
                Debug.LogError($"MqttJS_OnOffline() - clientId: {clientId} not found in MqttInstance");
        }

        [MonoPInvokeCallback(typeof(Action<string, string, bool>))]
        public static void MqttJS_OnError( string clientId, string errorJson, bool isReconnecting)
        {
            Debug.Log($"MqttJS_OnError() - clientId: {clientId}, errorJson: {errorJson}, isReconnecting: {isReconnecting}");
            if (MqttInstances.ContainsKey(clientId))
                MqttInstances[clientId].OnError(errorJson, isReconnecting);
            else
                Debug.LogError($"MqttJS_OnError() - clientId: {clientId} not found in MqttInstance");
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void MqttJS_OnEnd( string clientId)
        {
            Debug.Log($"MqttJS_OnEnd() - clientId: {clientId}");
            if (MqttInstances.ContainsKey(clientId))
                MqttInstances[clientId].OnEnd();
            else
                Debug.LogError( $"MqttJS_OnEnd() - clientId: {clientId} not found in MqttInstances");
        }

        //
        // Factory
        //
        public static IMqttClient Create(IMqttClientOwner owner)
        {
            string instanceId = Guid.NewGuid().ToString();
            WebGLMqttJSLib inst = new WebGLMqttJSLib(instanceId, owner);
            MqttInstances[instanceId] = inst;
            return inst;
        }

        //
        // Instance stuff
        //
        public bool Connected { get; private set;}
        public bool HasEverConnected { get; private set;}
        public string InstanceId { get; private set; }
        public IMqttClientOwner Owner { get; private set; }

        protected WebGLMqttJSLib(string instanceId, IMqttClientOwner owner)
        {
            InstanceId = instanceId;
            Owner = owner;
            Connected = false;
        }

        // Instance IMQTTClient interface

        public void Connect(string url, string optsJson)
        {
            MqttJS_Connect(InstanceId, url, optsJson);
        }

        public void Subscribe(string topic, string optsJson = null)
        {
            MqttJS_Subscribe(InstanceId, topic, optsJson);
        }

        public void Unsubscribe(string topic, string optsJson = null)
        {
            MqttJS_Unsubscribe(InstanceId, topic, optsJson);
        }
        public void Publish(string topic, string msg, string optsJson = null)
        {
            MqttJS_Publish(InstanceId, topic, msg, optsJson);
        }

        public void End()
        {
            MqttJS_End(InstanceId);
        }

        // Callbacks from JSlib

        private void OnError(string errorJson, bool isReconnecting)
        {
            Debug.Log($"OnError(): errorJson: {errorJson}, isReconnecting: {isReconnecting}");
            Owner.OnError(errorJson);
            MqttJS_End(InstanceId); // shut it down
        }

        private void OnConnect(string connAckJson)
        {
            Connected = true;
            HasEverConnected = true;
            Debug.Log($"OnConnect(): connAckJson: {connAckJson}");
            Owner.OnConnect(true, connAckJson);
        }

        private void OnMessage(string topic, string msgTxt, string packetJson)
        {
            //Debug.Log($"OnMessage(): clientId: {InstanceId} topic: {topic}, mesg: {msgTxt} packet: {packetJson}");
            Owner.OnMessage(topic, msgTxt, packetJson);
        }

        // Close just kinda happens sometimes...
        // Let reconnect help with it unless we requested it.
        private void OnClose(bool isReconnecting)
        {
            Debug.Log($"OnClose() - clientId: {InstanceId}, isReconnecting: {isReconnecting}");

            if (HasEverConnected)
            {
                if (Connected)
                    Owner.OnError("Connection closed unexpectedly.");
                else
                    Owner.OnError("OnClose called when not connected.");
            } else {
                Owner.OnError("Connection failed");
                // It has never connected - but has a client instance. shut it down do it doesn;t try to reconnect.
                MqttJS_End(InstanceId);
            }
            Connected = false;

        }

        // Offline just kinda happens sometimes too...
        // Unless we asked to end let reconnect try to do it...
        private void OnOffline()
        {
            Debug.Log($"OnOffline() - clientId: {InstanceId}");
        }

        private void OnEnd()
        {
            _CleanupLibInstance(InstanceId);
        }

        private void _CleanupLibInstance(string instanceId)
        {
            MqttJS_Delete(instanceId); // JS checks for null
            if (MqttInstances.ContainsKey(instanceId))
                MqttInstances.Remove(instanceId);
        }

#else // Not WebGL - can;t really do anything, but shouldn;t explode

        public static IMqttClient Create(IMqttClientOwner owner)
        {
            throw new NotImplementedException("Only works in WebGL builds");
            return null;
        }


#endif // UNITY_WEBGL

    }



}