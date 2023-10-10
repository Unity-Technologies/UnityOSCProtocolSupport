using System;
using System.Net;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Media.Osc.Editor
{
    [CustomEditor(typeof(OscNetworkSender))]
    class OscNetworkSenderEditor : UnityEditor.Editor
    {
        [SerializeField]
        VisualTreeAsset m_InspectorXML;

        ListView m_AddressesList;

        public override VisualElement CreateInspectorGUI()
        {
            var sender = target as OscNetworkSender;

            var root = new VisualElement();
            m_InspectorXML.CloneTree(root);

            // let the user know if the component is supported on the target build platform
            root.Q<HelpBox>("not-supported-message").SetDisplay(!OscNetworkSender.IsSupported());

            // show a message when the IP address is invalid
            var invalidIpAddressMessage = root.Q<HelpBox>("invalid-ip-address-message");

            root.Q<TextField>("ip-address").OnValueChanged(serializedObject, ipAddress =>
            {
                invalidIpAddressMessage.SetDisplay(!IPAddress.TryParse(ipAddress, out _));
            });

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

            // update the registered addresses list as needed
            sender.AddressesChanged += OnAddressesChanged;

            m_AddressesList = root.Q<ListView>("addresses");
            m_AddressesList.itemsSource = sender.Addresses;
            m_AddressesList.Q<Foldout>().value = false;

            root.Bind(serializedObject);
            return root;
        }

        void OnDisable()
        {
            var sender = target as OscNetworkSender;

            sender.AddressesChanged -= OnAddressesChanged;
        }

        void OnAddressesChanged()
        {
            m_AddressesList.Rebuild();
        }
    }
}
