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
        scroll_to_latest(li);
    });

    // <snippet_ReceiveMessage>
    connection.on("Error", (message) => {
        write_message("alert-danger", message);
    });

    connection.on("Info", (message) => {
        write_message("alert-info", message);
    });
    // </snippet_ReceiveMessage>

        // Trace telemetry on click handler
    if (document.getElementById("trace")) {
        document.getElementById("trace").addEventListener("click", async () => {
            var form = document.getElementById("iot-core-trace-form");
            if (form.checkValidity() === false) {
                event.preventDefault();
                event.stopPropagation();
                form.classList.add('was-validated');
                return false;
            }

            const device_id = document.getElementById("device-id").value;
            const registry_id = document.getElementById("registry-id").value;
            const password = document.getElementById("registry-pwd").value;
            const trace_type_radio = $('input[name="trace-type-radio"]:checked').val();

            const registry_cert = document.getElementById("registry-cert").value;

            // disable button
            $("#trace").prop("disabled", true);
            // add spinner to button
            $("#trace").html(
                `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Waiting for telemetry messages...`
            );

            // <snippet_Invoke>
            try {
                await connection.invoke("TraceDeviceMessages", trace_type_radio, device_id, registry_id, password, registry_cert);
            } catch (err) {
                console.error(err);
                write_message("alert-danger", err);
            }
            // </snippet_Invoke>

        });
    }
        // Send telemetry on click handler
    if (document.getElementById("device_send")) {
        document.getElementById("device_send").addEventListener("click", async () => {
            var form = document.getElementById("iot-core-device-send-form");
            if (form.checkValidity() === false) {
                event.preventDefault();
                event.stopPropagation();
                form.classList.add('was-validated');
                return false;
            }
            const uri = 'api/message/send'

            const message = {
                "deviceId": document.getElementById("device-id").value,
                "registryId": document.getElementById("registry-id").value,
                "password": document.getElementById("device-pwd").value,
                "registryCert": document.getElementById("registry-cert").value,
                "topicType": $('input[name="trace-type-radio"]:checked').val(),
                "message": document.getElementById("message").value
            };

            // disable button
            $("#device_send").prop("disabled", true);
            // add spinner to button
            $("#device_send").html(
                `<span id="iot-core-device-send-form-spinner" class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Sending message...`
            );

            fetch(uri, {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(message)
            })
                .then(response => response.json())
                .then(data => displayMessageSendResults(data))
                .catch(error => {
                    write_message("alert-danger", error);
                    console.error('Unable send message.', error)
                });
            // enable button
            $("#device_send").prop("disabled", false);
            // remove spinner from button
            $("#device_send").html("Send message");
        });

    }

    function displayMessageSendResults(result) {

        if (result) {
            if (result.info && Array.isArray(result.info)) {
                result.info.forEach(function (message) {
                    write_message("alert-info", message);
                });
            }

            if (result.error && Array.isArray(result.error)) {
                result.error.forEach(function (message) {
                    write_message("alert-danger", message);
                });
            }
        }
       }


        function write_message(alert_class, err) {
            $('#messageList').append(`<li><div class="alert ${alert_class}" role="alert">${err}</div></li>`);
            if ($('#messageList').children().last()) {
                scroll_to_latest($('#messageList').children().last()[0]);
            }
        }

        function scroll_to_latest(message_elmt) {
            if (message_elmt.offsetTop > $(document).height() - $(window).height()) {
                dest = $(document).height() - $(window).height();
            } else {
                dest = message_elmt.offsetTop;
            }
            //go to destination
            $('html,body').animate({ scrollTop: dest }, 1000, 'swing');
        }

        async function start() {
            try {
                // connection.hub.logging = true;
                await connection.start();
                console.log("SignalR Connected.");
            } catch (err) {              
                console.log(err);
                write_message("alert-danger", err);
                setTimeout(start, 5000);
            }
        };

        connection.onclose(start);

        // Start the connection.
        start();
});