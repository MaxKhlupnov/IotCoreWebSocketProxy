document.addEventListener("DOMContentLoaded", () => {
    // <snippet_Connection>
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/telemetryhub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // <snippet_ReceiveMessage>
    connection.on("Trace", (message) => {
        const li = document.createElement("li");
        li.textContent = `${message}`;
        document.getElementById("messageList").appendChild(li);
    });
    // </snippet_ReceiveMessage>

    document.getElementById("send").addEventListener("click", async () => {
        const device_id = document.getElementById("device-id").value;
        const device_pwd = document.getElementById("device-pwd").value;
        const device_cert = document.getElementById("device-cert").value;

        // <snippet_Invoke>
        try {
            await connection.invoke("TraceDeviceMessages", device_id, device_pwd, device_cert);
        } catch (err) {
            console.error(err);
        }
        // </snippet_Invoke>
    });

        async function start() {
            try {
                await connection.start();
                console.log("SignalR Connected.");
            } catch (err) {
                console.log(err);
                setTimeout(start, 5000);
            }
        };

        connection.onclose(start);

        // Start the connection.
        start();
});