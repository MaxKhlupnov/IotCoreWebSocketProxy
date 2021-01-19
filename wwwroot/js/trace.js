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
        const registry_id = document.getElementById("registry-id").value;
        const password = document.getElementById("registry-pwd").value;
        const trace_type_radio = $('input[name="trace-type-radio"]:checked').val();

        const registry_cert = document.getElementById("registry-cert").value;

        // <snippet_Invoke>
        try {
            await connection.invoke("TraceDeviceMessages", trace_type_radio, device_id, registry_id, password, registry_cert);
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