[Contents](TableOfContents.md) | [Home](index.md) > [Getting started with examples](getting-started.md) > Controlling a remote OSC app from Unity

# Controlling a remote OSC app from Unity

Example configuration to remotely control a slider on your external OSC app over the network by manipulating a single value from the Unity Editor (here, through a Light GameObject).

1. In Unity, create an empty GameObject to hold the components required for sending OSC messages:

 a. In the Unity Editor’s menu, select **GameObject \> Create Empty**.

 b. Name this GameObject “OSC Out”, for example.


2. In the “OSC Out” GameObject, add and configure an [OSC Sender](ui-ref.md#osc-sender) component:

 a. In the Inspector, select **Add Component \> OSC \> OSC Sender**.

 b. Set **IP Address** to the address of the receiving device hosting your OSC app.

 c. Set **Port** to the network port the receiving device is listening on.


3. In the “OSC Out” GameObject, add and configure an [OSC Message Output](ui-ref.md#osc-message-output) component:

 a. In the Inspector, select **Add Component \> OSC \> OSC Message Output**.

 b. Set **OSC Address** to the address of the slider you want to control from Unity, according to the specifications of your external OSC app. For example: /myapp/screen1/slider1.

 c. Select **Add New OSC Argument.**

 d. In the created Argument 0, set **Object** to a Light GameObject of your Scene.

 e. Set **Component** to “Light”, and **Property** to “Single bounceIntensity”.


4. In the Hierarchy, select the Light GameObject you targeted in the previous step.


5. In the Inspector, in **Light \> Emission**, play with the **Indirect Multiplier** value between 0 and 1.  
   In your external OSC app, you should see the slider moving.


## Troubleshooting

- Make sure you correctly mapped the IP Address and Port in your external OSC app and in the OSC Sender component in Unity. Change the Port value on both sides if needed.

- Use the [OSC Monitor](ui-ref.md#osc-monitor) to verify if Unity correctly sends OSC messages and to analyze the message details.
