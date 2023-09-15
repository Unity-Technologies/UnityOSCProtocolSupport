# Controlling a GameObject from a remote OSC app

Example configuration to remotely control the position of a GameObject in your Unity Scene over the network by using a touchpad on an external OSC app.

1. In your external OSC app, configure the network settings to send OSC messages to your Unity Editor workstation: IP Address and Port.


2. In Unity, create an empty GameObject to hold the components required for receiving OSC messages:

  a. In the Unity Editor’s menu, select **GameObject \> Create Empty**.

  b. Name this GameObject “OSC In”, for example.


3. In the “OSC In” GameObject, add and configure an [OSC Receiver](ui-ref.md#osc-receiver) component:

  a. In the Inspector, select **Add Component \> OSC \> OSC Receiver**.

  b. Set **Port** to the network port you specified your external OSC app to send messages to.


4. In the “OSC In” GameObject, add and configure an [OSC Message Handler](ui-ref.md#osc-message-handler) component:

  a. In the Inspector, select **Add Component \> OSC \> OSC Message Handler**.

  b. Set **OSC Address** to the address of the touchpad you want to use to control your GameObject position, according to the specifications of your external OSC app. For example: /myapp/screen1/touchpad1.

  c. Select **Add New OSC Argument**.

  d. In the created Argument 0, set the dropdown value to “Vector3”.

  e. In the **Event(Vector3)** list, select **+** (plus) to add an event.

  f. In the added event, in the left dropdown, select **Editor And Runtime**.

  g. Set the target field to the GameObject you want to control.

  h. In the right dropdown, select **Transform \> localPosition**.


5. In your external OSC app, move your finger on the touchpad.  
   In your Unity Scene, you should see the targeted GameObject move on the X and Y axes.


## Troubleshooting

- Make sure you correctly mapped the IP Address and Port in your external OSC app and in the OSC Receiver component in Unity. Change the Port value on both sides if needed.

- Use the [OSC Monitor](ui-ref.md#osc-monitor) to verify if Unity correctly receives OSC messages from your external OSC app and to analyze the message details. The OSC messages sent to Unity should use the tag string “fff” and contain 3 float values for this example to work.
