# Features

## OSC messaging

-   OSC message writing and parsing from buffers and streams.

-   OSC message sending and receiving.

-   OSC address space management: message dispatching and pattern matching.

-   OSC message bundle support.

-   [OSC 1.0 specification](https://opensoundcontrol.stanford.edu/spec-1_0.html) full support.

-   [OSC 1.1 specification](https://opensoundcontrol.stanford.edu/spec-1_1.html) partial support (mostly to use SLIP for stream-based protocols and standardize various metadata formats).

## Message transport

-   Support for UDP and TCP over the network.

-   Support for UDP multicast.

-   API available to add support for other transport media such as serial connection.

## Unity Editor components and windows

-   Components to send and receive OSC messages over the network.

-   Component to apply OSC message data to a Unity Scene.

-   Component to generate OSC messages when the Unity Scene changes.

-   Window to monitor all OSC messages sent and received by the Unity Editor.

## Functionality context

-   Works in the Unity Editor in Edit mode and Play mode.

-   Works in Unity built runtime apps. UDP and TCP transport are limited to platforms that support the System.Net.Sockets library (i.e. most platforms except WebGL and some consoles).
