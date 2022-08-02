using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // for JObject
using UnityMqttJS;

public class MqttJsWsConnectTest :  MonoBehaviour, IMqttClientOwner
{

    public static string wsDefBrokerUrl = "wss://newsweasel.com:15676/ws";

    public static JObject wsDefOptsObj =  new JObject {
        {"username", "apian_mqtt"},
        {"password", "apian_mqtt_pwd"},
        {"clean", true},
        {"connectTimeout", 750},
        {"keepalive", 1000},
        {"reconnectPeriod", 500},
        {"queueQoSZero", true}  // will queue up outgoing Qos0 messages if disconnected and send on reconnect
    };

    public TMP_InputField UrlFld;
    public TMP_InputField ConfigFld;
    public TMP_InputField TopicFld;
    public TMP_InputField MsgFld;
    public TMP_InputField OutputFld; // is set to non-interactive
    public TMP_Dropdown TopicSel;

    //public TMP_InputField PeerAddrFld;

    protected UnityEngine.UI.Scrollbar outScrollBar;

    protected string SelectedTopic => TopicSel.options[TopicSel.value].text;
    public IMqttClient mqttInst;

    // Start is called before the first frame update
    void Start()
    {
        ConfigFld.text = wsDefOptsObj.ToString();
        UrlFld.text = wsDefBrokerUrl;
        outScrollBar = OutputFld.transform.Find("Scrollbar").gameObject.GetComponent<UnityEngine.UI.Scrollbar>();
        TopicSel.ClearOptions();
    }

    // Update is called once per frame
    void Update()
    {

    }


    public void DoConnect()
    {
        Debug.Log("Connect pressed.");

        string url = UrlFld.text;
        string optsJson = ConfigFld.text;

        Debug.Log($"MQTT options: {optsJson}");

        mqttInst = WebGLMqttJSLib.Create(this);

        mqttInst.Connect( url, optsJson);
     }

    public void DoDisconnect()
    {
        Debug.Log("Disconnect pressed.");
        mqttInst.End();
    }

    public void DoSubscribe()
    {
        string topic = TopicFld.text;
        try {
            mqttInst.Subscribe(topic);
            List<string> topicList = new List<string>() {topic};
            TopicSel.AddOptions( topicList );
            Log($"Subscribed to {topic}");
        } catch (Exception e) {
            Log($"Failed to subscrib to {topic}: {e.Message}");
        }
    }

    public void DoUnsubscribe()
    {
        string topic = TopicFld.text;
        try {
            mqttInst.Unsubscribe(topic);
            List<string> newTopics = TopicSel.options.Where(t => t.text != topic).Select(t => t.text ).ToList();
            TopicSel.ClearOptions();
            TopicSel.AddOptions( newTopics );
            Log($"Unsubscribed from {topic}");
        } catch (Exception e) {
          Log($"Failed to unsubscribe from {topic}: {e.Message}");
        }
    }

    public void DoPublish()
    {
        string topic = SelectedTopic;
        string payload = MsgFld.text;
        Log($"Publishing to {topic}: {payload}");
        try {
            mqttInst.Publish(topic, payload);
        } catch (Exception e) {
          Log($"Failed to publish to {topic}: {e.Message}");
        }
    }

    public void Log(string msg)
    {
        OutputFld.text = $"{OutputFld.text}{msg}\n";
        outScrollBar.value = 1; // scroll to end on log()
    }

    public void OnError(string errMsg) {
        Log($"Error: {errMsg}");
        //mqttInst = null;
    }
    public void OnConnect(bool success, string connAck = null)
    {
        Log( $"Connected! ConnAck: {connAck}");
    }

    public void OnMessage(string topic, string msgText, string msgPayload)
    {
        Log($"[{topic}]: {msgText} - payload: {msgPayload}");
    }


    public void OnDisconnect()
    {
        Log( $"Disconnected!");
       // mqttInst = null;
    }

    public void OnOffline()
    {
        Log( $"Offline!");
        //mqttInst = null;
    }

    public void OnEnd()
    {
        Log( $"Shutdown complete");
        mqttInst = null;
    }

}
