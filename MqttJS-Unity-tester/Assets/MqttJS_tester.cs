using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

using UnityMqttJS;

public class MqttJS_tester : MonoBehaviour, IMqttClientOwner
{
    public IMqttClient mqttInst;

    // Start is called before the first frame update
    void Start()
    {
        // StartCoroutine(run_tests());
    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator run_tests()
    {
        JObject wsOptsObj =  new JObject {
            {"username", "apian_mqtt"},
            {"password", "apian_mqtt_pwd"},
            {"clean", true},
            {"connectTimeout", 2000},
        };

        Debug.Log($"MQTT options: {wsOptsObj.ToString()}");

        string url = "wss://newsweasel.com:15676/ws";

        mqttInst = WebGLMqttJSLib.Create(this);

        mqttInst.Connect( url, wsOptsObj.ToString());

        yield return new WaitForSeconds(1);

        mqttInst.Subscribe("foo");

        mqttInst.Publish("foo", "A Message");
        mqttInst.Publish("foo", "Another Message");

        mqttInst.Unsubscribe("foo");

        //yield return new WaitForSeconds(1);

        mqttInst.Publish("foo", "Shouldn;t get this one?");

        //yield return new WaitForSeconds(1);

        mqttInst.End();

    }

    public void OnError(string _) {}
    public void OnConnect(bool _, string _2 = null) {}
    public void OnMessage(string _, string _2, string _3) {}
    public void OnDisconnect()    { }
    public void OnOffline() {}
    public void OnEnd() {}
}
