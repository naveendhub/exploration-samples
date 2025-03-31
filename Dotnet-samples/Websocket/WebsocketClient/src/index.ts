var W3CWebSocket = require("websocket").w3cwebsocket;

var client = new W3CWebSocket("ws://localhost:8000/chat");

client.onerror = function () {
  console.log("Connection Error");
};

client.onopen = function () {
  console.log("WebSocket Client Connected");

  function sendNumber() {
    if (client.readyState === client.OPEN) {
      var number = Math.round(Math.random() * 0xffffff);
      client.send(number.toString());
      setTimeout(sendNumber, 1000);
    }
  }
  sendNumber();
};

client.onclose = function () {
  console.log("Client Closed");
};

client.onmessage = function (e: any) {
  if (typeof e.data === "string") {
    console.log("Received: '" + e.data + "'");
  } else {
    const jsonString = JSON.parse(
      new TextDecoder().decode(e.data as ArrayBuffer)
    );
    console.log(jsonString);
  }
};
