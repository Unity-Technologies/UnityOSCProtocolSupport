using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Media.Osc.Editor
{
    [CustomEditor(typeof(OscNetworkReceiver))]
    class OscNetworkReceiverEditor : UnityEditor.Editor
    {
        [SerializeField]
        VisualTreeAsset m_InspectorXML;

        ListView m_AddressesList;
        HelpBox m_CannotConfigureMessage;

        public override VisualElement CreateInspectorGUI()
        {
            var receiver = target as OscNetworkReceiver;

            var root = new VisualElement();
            m_InspectorXML.CloneTree(root);

            // let the user know if the component is supported on the target build platform
            root.Q<HelpBox>("not-supported-message").SetDisplay(!OscNetworkReceiver.IsSupported());

            // show a message when the port is invalid
            var invalidPortMessage = root.Q<HelpBox>("invalid-port-message");

            root.Q<IntegerField>("port").OnValueChanged(serializedObject, port =>
            {
                var isValid = NetworkingUtils.IsPortValid(port, out var message);

                invalidPortMessage.text = message;
                invalidPortMessage.messageType = isValid ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
                invalidPortMessage.SetDisplay(!string.IsNullOrEmpty(message));
            });

            // only show options relevant to the current protocol
            var udpOptions = root.Q("udp-options");
            var tcpOptions = root.Q("tcp-options");

            root.Q<PropertyField>("protocol").OnValueChanged(serializedObject, prop => (OscNetworkProtocol)prop.intValue, protocol =>
            {
                udpOptions.SetDisplay(protocol == OscNetworkProtocol.Udp);
                tcpOptions.SetDisplay(protocol == OscNetworkProtocol.Tcp);
            });

            // show a message warning about configuration limitations when multiple TCP servers share the same port
            m_CannotConfigureMessage = root.Q<HelpBox>("cannot-modify-stream-type-message");
            m_CannotConfigureMessage.schedule.Execute(UpdateCannotConfigureMessage).Every(100);
            UpdateCannotConfigureMessage();

            // only show all of the multicast options if receive multicast is enabled
            var multicastFoldout = root.Q<Foldout>("multicast-foldout");
            multicastFoldout.Q<Toggle>().SetDisplay(false);

            root.Q<Toggle>("receive-multicast").OnValueChanged(serializedObject, receiveMulticast =>
            {
                multicastFoldout.value = receiveMulticast;
            });

            // update the registered addresses list as needed
            receiver.AddressesChanged += OnAddressesChanged;

            m_AddressesList = root.Q<ListView>("addresses");
            m_AddressesList.itemsSource = receiver.Addresses;
            m_AddressesList.Q<Foldout>().value = false;

            root.Bind(serializedObject);
            return root;
        }

        void OnDisable()
        {
            var receiver = target as OscNetworkReceiver;

            receiver.AddressesChanged -= OnAddressesChanged;
        }

        void OnAddressesChanged()
        {
            m_AddressesList.Rebuild();
        }

        void UpdateCannotConfigureMessage()
        {
            var showMessage = false;

            if (target is OscNetworkReceiver receiver && receiver.TryGetServer<OscTcpServer>(out var tcpServer))
            {
                showMessage = tcpServer.StreamType != receiver.StreamType;
                m_CannotConfigureMessage.text =
                    $"Another OSC receiver is already running on TCP port {tcpServer.Port} using {tcpServer.StreamType.GetDisplayName()}. " +
                    $"This receiver will use the same configuration.";
            }

            m_CannotConfigureMessage.SetDisplay(showMessage);
        }
    }
}
