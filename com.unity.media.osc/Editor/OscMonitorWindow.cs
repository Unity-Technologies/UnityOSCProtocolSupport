using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Media.Osc.Editor
{
    class OscMonitorWindow : EditorWindow
    {
        const string k_WindowName = "OSC Monitor";
        const string k_WindowPath = "Window/Analysis/" + k_WindowName;
        const string k_MessageValueNameLabelClass = "MessageValueName";
        const string k_StatusClass = "Status";
        const string k_StatusNoneClass = "StatusNone";
        const string k_StatusOkClass = "StatusOk";
        const string k_StatusWarningClass = "StatusWarning";
        const string k_StatusErrorClass = "StatusError";

        const int k_MessageLogUpdatePeriod = 100;

        static class Contents
        {
            public static readonly string ClearOnPlayOption = "Clear on Play";
        }

        static class Prefs
        {
            static readonly string k_Base = $"{typeof(OscMonitorWindow).FullName}-";
            static readonly string k_ShowClients = k_Base + "show-clients";
            static readonly string k_ShowServers = k_Base + "show-server";
            static readonly string k_ShowMessageLog = k_Base + "show-message-log";
            static readonly string k_EnableMonitor = k_Base + "monitor-enable";
            static readonly string k_ClearOnPlay = k_Base + "clear-on-play";
            static readonly string k_FilterSent = k_Base + "monitor-sent";
            static readonly string k_FilterReceived = k_Base + "monitor-received";

            public static bool ShowClients
            {
                get => EditorPrefs.GetBool(k_ShowClients, true);
                set => EditorPrefs.SetBool(k_ShowClients, value);
            }

            public static bool ShowServers
            {
                get => EditorPrefs.GetBool(k_ShowServers, true);
                set => EditorPrefs.SetBool(k_ShowServers, value);
            }

            public static bool ShowMessageLog
            {
                get => EditorPrefs.GetBool(k_ShowMessageLog, true);
                set => EditorPrefs.SetBool(k_ShowMessageLog, value);
            }

            public static bool EnableMonitor
            {
                get => EditorPrefs.GetBool(k_EnableMonitor, true);
                set => EditorPrefs.SetBool(k_EnableMonitor, value);
            }

            public static bool ClearOnPlay
            {
                get => EditorPrefs.GetBool(k_ClearOnPlay, true);
                set => EditorPrefs.SetBool(k_ClearOnPlay, value);
            }

            public static bool FilterSent
            {
                get => EditorPrefs.GetBool(k_FilterSent, true);
                set => EditorPrefs.SetBool(k_FilterSent, value);
            }

            public static bool FilterReceived
            {
                get => EditorPrefs.GetBool(k_FilterReceived, true);
                set => EditorPrefs.SetBool(k_FilterReceived, value);
            }
        }

        [SerializeField]
        VisualTreeAsset m_WindowUxml;
        [SerializeField]
        StyleSheet m_WindowCommonUss;
        [SerializeField]
        StyleSheet m_WindowLightUss;
        [SerializeField]
        StyleSheet m_WindowDarkUss;

        [Serializable]
        struct MessageData
        {
            public enum Type
            {
                Sent,
                Received,
            }

            [SerializeField]
            int m_SequenceNumber;
            [SerializeField]
            Type m_MessageType;
            [SerializeField]
            string m_Source;
            [SerializeField]
            string m_OscAddress;
            [SerializeField]
            TypeTag[] m_Tags;
            [SerializeField]
            string[] m_Values;

            public int SequenceNumber => m_SequenceNumber;
            public Type MessageType => m_MessageType;
            public string Source => m_Source;
            public string OscAddress => m_OscAddress;
            public TypeTag[] Tags => m_Tags;
            public string[] Values => m_Values;

            public MessageData(int sequenceNumber, Type type, string source, string oscAddress, TypeTag[] tags, string[] values)
            {
                m_SequenceNumber = sequenceNumber;
                m_MessageType = type;
                m_Source = source;
                m_OscAddress = oscAddress;
                m_Tags = tags;
                m_Values = values;
            }
        }

        Foldout m_ClientsFoldout;
        VisualElement m_ClientsList;
        VisualElement m_ClientsEmptyMessage;

        Foldout m_ServersFoldout;
        VisualElement m_ServersList;
        VisualElement m_ServersEmptyMessage;

        Foldout m_MessageLogFoldout;
        MultiColumnListView m_MessageListView;
        Scroller m_MessageListScroller;
        VisualElement m_MessageDetails;
        Label m_MessageSourceName;
        Label m_MessageSourceValue;
        Label m_MessageAddressValue;
        Label m_MessageTagsValue;
        VisualElement m_MessageValuesList;

        readonly ConcurrentQueue<MessageData> m_MessageQueue = new ConcurrentQueue<MessageData>();
        readonly List<MessageData> m_MessageListFiltered = new List<MessageData>();

        [SerializeField]
        List<MessageData> m_MessageList = new List<MessageData>();
        [SerializeField]
        int m_SequenceNumber;
        [SerializeField]
        int m_SelectedSequenceNumber;
        [SerializeField]
        bool m_Autoscroll;
        [SerializeField]
        string m_SearchTextRaw;
        [SerializeField]
        string m_SearchText;

        [MenuItem(k_WindowPath)]
        static void ShowWindow()
        {
            GetWindow<OscMonitorWindow>();
        }

        void Awake()
        {
            // When the window is initialized we should clear any serialized state. This is needed
            // since the serialized fields persist when the editor is closed and opened, but we want
            // to have a clear message log when the project is opened.
            m_MessageList.Clear();
            m_SequenceNumber = -1;
            m_SelectedSequenceNumber = -1;
            m_Autoscroll = true;
            m_SearchTextRaw = string.Empty;
            m_SearchText = string.Empty;
        }

        void OnEnable()
        {
            titleContent = new GUIContent(k_WindowName);

            OscClient.ClientsChanged += ClientsListChanged;
            OscServer.ServersChanged += ServersListChanged;

            SetCaptureEnabled(Prefs.EnableMonitor);

            SetClearOnPlay(Prefs.ClearOnPlay);
        }

        void OnDisable()
        {
            OscClient.ClientsChanged -= ClientsListChanged;
            OscServer.ServersChanged -= ServersListChanged;

            OscClient.MessageSent -= OnMessageSent;
            OscServer.MessageReceived -= OnMessageReceived;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        void CreateGUI()
        {
            m_WindowUxml.CloneTree(rootVisualElement);

            rootVisualElement.styleSheets.Add(m_WindowCommonUss);
            rootVisualElement.styleSheets.Add(EditorGUIUtility.isProSkin ? m_WindowDarkUss : m_WindowLightUss);

            m_ClientsFoldout = rootVisualElement.Q<Foldout>("clients-foldout");
            m_ServersFoldout = rootVisualElement.Q<Foldout>("servers-foldout");
            m_MessageLogFoldout = rootVisualElement.Q<Foldout>("message-log-foldout");

            m_ClientsFoldout.Q<Toggle>().focusable = false;
            m_ServersFoldout.Q<Toggle>().focusable = false;
            m_MessageLogFoldout.Q<Toggle>().focusable = false;

            m_ClientsFoldout.value = Prefs.ShowClients;
            m_ServersFoldout.value = Prefs.ShowServers;
            m_MessageLogFoldout.value = Prefs.ShowMessageLog;

            var clientsContent = rootVisualElement.Q("clients-content");
            var serversContent = rootVisualElement.Q("servers-content");
            var messageLogContent = rootVisualElement.Q("message-log-content");

            clientsContent.SetDisplay(m_ClientsFoldout.value);
            serversContent.SetDisplay(m_ServersFoldout.value);
            messageLogContent.visible = m_MessageLogFoldout.value;

            m_ClientsFoldout.RegisterValueChangedCallback(evt =>
            {
                Prefs.ShowClients = evt.newValue;
                clientsContent.SetDisplay(evt.newValue);
            });
            m_ServersFoldout.RegisterValueChangedCallback(evt =>
            {
                Prefs.ShowServers = evt.newValue;
                serversContent.SetDisplay(evt.newValue);
            });
            m_MessageLogFoldout.RegisterValueChangedCallback(evt =>
            {
                Prefs.ShowMessageLog = evt.newValue;
                messageLogContent.visible = evt.newValue;

                RefreshMessageList();
            });

            m_ClientsEmptyMessage = clientsContent.Q("clients-empty-message");
            m_ServersEmptyMessage = serversContent.Q("servers-empty-message");
            m_ClientsList = clientsContent.Q("clients-list");
            m_ServersList = serversContent.Q("servers-list");

            ClientsListChanged();
            ServersListChanged();

            var monitorEnableToggle = rootVisualElement.Q<Toggle>("monitor-enable");
            monitorEnableToggle.value = Prefs.EnableMonitor;
            monitorEnableToggle.RegisterValueChangedCallback(evt =>
            {
                SetCaptureEnabled(evt.newValue);
            });

            var filterSentToggle = rootVisualElement.Q<Toggle>("monitor-sent");
            filterSentToggle.value = Prefs.FilterSent;
            filterSentToggle.RegisterValueChangedCallback(evt =>
            {
                Prefs.FilterSent = evt.newValue;
                OnFiltersChanged();
            });

            var filterReceivedToggle = rootVisualElement.Q<Toggle>("monitor-received");
            filterReceivedToggle.value = Prefs.FilterReceived;
            filterReceivedToggle.RegisterValueChangedCallback(evt =>
            {
                Prefs.FilterReceived = evt.newValue;
                OnFiltersChanged();
            });

            rootVisualElement.Q<Button>("monitor-clear").RegisterCallback<PointerUpEvent>(_ => ClearMessageList());

            var clearOptionsMenu = rootVisualElement.Q<ToolbarMenu>("monitor-clear-options");
            clearOptionsMenu.menu.AppendAction(Contents.ClearOnPlayOption,
                _ => SetClearOnPlay(!Prefs.ClearOnPlay),
                _ => Prefs.ClearOnPlay ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
            );

            var searchField = rootVisualElement.Q<ToolbarSearchField>("monitor-search");
            searchField.value = m_SearchTextRaw;
            searchField.RegisterValueChangedCallback(evt =>
            {
                m_SearchTextRaw = evt.newValue;
                m_SearchText = m_SearchTextRaw.Trim();
                OnFiltersChanged();
            });

            m_MessageDetails = rootVisualElement.Q("message-details");
            m_MessageSourceName = rootVisualElement.Q<Label>("message-source-name");
            m_MessageSourceValue = rootVisualElement.Q<Label>("message-source-value");
            m_MessageAddressValue = rootVisualElement.Q<Label>("message-address-value");
            m_MessageTagsValue = rootVisualElement.Q<Label>("message-tags-value");
            m_MessageValuesList = rootVisualElement.Q("message-values");

            m_MessageDetails.SetDisplay(false);
            MakeSelectable(m_MessageSourceValue);
            MakeSelectable(m_MessageAddressValue);
            MakeSelectable(m_MessageTagsValue);

            m_MessageListView = rootVisualElement.Q<MultiColumnListView>("message-list");
            m_MessageListView.itemsSource = m_MessageListFiltered;
            m_MessageListView.columnSortingChanged += SortMessageList;
            m_MessageListView.onSelectionChange += _ => OnSelectMessage(false);

            m_MessageListView.columns["sequence"].bindCell = (element, index) =>
            {
                ((Label)element).text = m_MessageListFiltered[index].SequenceNumber.ToString();
            };
            m_MessageListView.columns["direction"].bindCell = (element, index) =>
            {
                ((Label)element).text = m_MessageListFiltered[index].MessageType switch
                {
                    MessageData.Type.Sent => "Sent",
                    MessageData.Type.Received => "Received",
                    _ => string.Empty,
                };
            };
            m_MessageListView.columns["source"].bindCell = (element, index) =>
            {
                ((Label)element).text = m_MessageListFiltered[index].Source;
            };
            m_MessageListView.columns["osc-address"].bindCell = (element, index) =>
            {
                ((Label)element).text = m_MessageListFiltered[index].OscAddress;
            };
            m_MessageListView.columns["contents"].bindCell = (element, index) =>
            {
                ((Label)element).text = string.Join(", ", m_MessageListFiltered[index].Values);
            };

            foreach (var col in m_MessageListView.columns)
            {
                col.unbindCell = (element, index) =>
                {
                    ((Label)element).text = null;
                };
            }

            m_MessageListScroller = m_MessageListView.Q<ScrollView>().verticalScroller;
            m_MessageListScroller.valueChanged += delegate (float value)
            {
                m_Autoscroll = Math.Abs(value - m_MessageListScroller.highValue) < 0.5f;
            };

            m_MessageListView.schedule.Execute(UpdateMessageList).Every(k_MessageLogUpdatePeriod);

            UpdateMessageList();
            OnFiltersChanged();
            RefreshMessageList();
            OnSelectMessage(true);
        }

        void Update()
        {
            UpdateClientsList();
            UpdateServersList();

            if (m_Autoscroll)
            {
                m_MessageListScroller.value = m_MessageListScroller.highValue;
            }
        }

        void ClientsListChanged()
        {
            var isEmpty = OscClient.Clients.Count == 0;

            if (m_ClientsEmptyMessage != null)
            {
                m_ClientsEmptyMessage.SetDisplay(isEmpty);
            }
            if (m_ClientsList != null)
            {
                m_ClientsList.SetDisplay(!isEmpty);
                m_ClientsList.Clear();

                foreach (var client in OscClient.Clients)
                {
                    var row = CreateStatusRow();
                    row.userData = client;
                    m_ClientsList.Add(row);
                }
            }
        }

        void ServersListChanged()
        {
            var isEmpty = OscServer.Servers.Count == 0;

            if (m_ServersEmptyMessage != null)
            {
                m_ServersEmptyMessage.SetDisplay(isEmpty);
            }
            if (m_ServersList != null)
            {
                m_ServersList.SetDisplay(!isEmpty);
                m_ServersList.Clear();

                foreach (var server in OscServer.Servers)
                {
                    var row = CreateStatusRow();
                    row.userData = server;
                    m_ServersList.Add(row);
                }
            }
        }

        VisualElement CreateStatusRow()
        {
            var row = new VisualElement();
            var statusBox = new VisualElement
            {
                name = "status",
            };
            var nameLabel = new Label
            {
                name = "name",
            };
            var messageContainer = new VisualElement();
            var messageLabel = new Label
            {
                name = "message",
            };

            row.Add(statusBox);
            row.Add(nameLabel);
            row.Add(messageContainer);
            messageContainer.Add(messageLabel);

            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 3f;
            row.style.marginBottom = 3f;
            nameLabel.style.minWidth = 275f;
            nameLabel.style.flexGrow = 0.1f;
            messageContainer.style.marginLeft = 10f;
            messageContainer.style.flexGrow = 0.9f;
            messageContainer.style.minWidth = 100f;
            messageLabel.style.whiteSpace = WhiteSpace.Normal;

            return row;
        }

        void UpdateClientsList()
        {
            if (!m_ClientsFoldout.value)
            {
                return;
            }

            foreach (var row in m_ClientsList.Children())
            {
                var client = row.userData as OscClient;
                var status = client.GetStatus(out var message);
                UpdateStatusRow(row, client.ToString(), status, message);
            }
        }

        void UpdateServersList()
        {
            if (!m_ServersFoldout.value)
            {
                return;
            }

            foreach (var row in m_ServersList.Children())
            {
                var server = row.userData as OscServer;
                var status = server.GetStatus(out var message);
                UpdateStatusRow(row, server.ToString(), status, message);
            }
        }

        void UpdateStatusRow(VisualElement row, string name, Status status, string message)
        {
            var statusBox = row.Q("status");
            statusBox.ClearClassList();
            statusBox.AddToClassList(k_StatusClass);
            statusBox.AddToClassList(status switch
            {
                Status.Ok => k_StatusOkClass,
                Status.Warning => k_StatusWarningClass,
                Status.Error => k_StatusErrorClass,
                _ => k_StatusNoneClass,
            });
            statusBox.tooltip = message;

            var nameLabel = row.Q<Label>("name");
            nameLabel.text = name;

            var messageLabel = row.Q<Label>("message");
            messageLabel.text = message;
        }

        void ClearMessageList()
        {
            Interlocked.Exchange(ref m_SequenceNumber, -1);
            m_MessageList.Clear();
            m_MessageListFiltered.Clear();

            RefreshMessageList();

            m_SelectedSequenceNumber = -1;
            m_Autoscroll = true;
        }

        void UpdateMessageList()
        {
            var filteredListDirty = false;

            while (m_MessageQueue.TryDequeue(out var message))
            {
                m_MessageList.Add(message);

                if (FilterMessage(message))
                {
                    m_MessageListFiltered.Add(message);
                    filteredListDirty = true;
                }
            }

            if (filteredListDirty && m_MessageLogFoldout.value)
            {
                // We could choose to sort here as well, but the performance cost could be considerable,
                // it is probably better to have users toggle the column sorting when they want to refresh the sorting.
                RefreshMessageList();
            }
        }

        void OnFiltersChanged()
        {
            m_MessageListFiltered.Clear();

            foreach (var message in m_MessageList)
            {
                if (FilterMessage(message))
                {
                    m_MessageListFiltered.Add(message);
                }
            }

            SortMessageList();
        }

        void SortMessageList()
        {
            foreach (var col in m_MessageListView.sortedColumns)
            {
                switch (col.columnName)
                {
                    case "sequence":
                    {
                        m_MessageListFiltered.Sort((a, b) => col.direction == SortDirection.Ascending
                            ? a.SequenceNumber.CompareTo(b.SequenceNumber)
                            : b.SequenceNumber.CompareTo(a.SequenceNumber)
                        );
                        break;
                    }
                    case "source":
                    {
                        m_MessageListFiltered.Sort((a, b) => col.direction == SortDirection.Ascending
                            ? a.Source.CompareTo(b.Source)
                            : b.Source.CompareTo(a.Source)
                        );
                        break;
                    }
                    case "osc-address":
                    {
                        m_MessageListFiltered.Sort((a, b) => col.direction == SortDirection.Ascending
                            ? a.OscAddress.CompareTo(b.OscAddress)
                            : b.OscAddress.CompareTo(a.OscAddress)
                        );
                        break;
                    }
                }
            }

            // We want to preserve the selected item though sorting, so we should rely on the sequence number to
            // reacquire the selection when we refresh.
            m_MessageListView.selectedIndex = -1;

            RefreshMessageList();
        }

        void RefreshMessageList()
        {
            if (m_MessageLogFoldout == null || !m_MessageLogFoldout.value)
            {
                return;
            }
            if (m_MessageListView == null)
            {
                return;
            }

            m_MessageListView.Rebuild();
            OnSelectMessage(false);
        }

        void OnSelectMessage(bool forceUpdate)
        {
            // determine what message, if any, is selected
            var lastSequenceNumber = m_SelectedSequenceNumber;
            var selectedIndex = Mathf.Min(m_MessageListView.selectedIndex, m_MessageListFiltered.Count - 1);

            if (selectedIndex >= 0)
            {
                m_SelectedSequenceNumber = m_MessageListFiltered[selectedIndex].SequenceNumber;
            }
            else if (m_SelectedSequenceNumber >= 0)
            {
                // if we previously had a message selected, try to find it and select it
                selectedIndex = m_MessageListFiltered.FindIndex(m => m.SequenceNumber == m_SelectedSequenceNumber);
                m_MessageListView.selectedIndex = selectedIndex;

                // if the message no longer exists or is filtered away, clear the selection
                if (selectedIndex < 0)
                {
                    m_SelectedSequenceNumber = -1;
                }
            }

            // if a message is selected we show the details in the split panel
            if (forceUpdate || m_SelectedSequenceNumber != lastSequenceNumber)
            {
                SetMessageDetails(selectedIndex >= 0 ? m_MessageListFiltered[selectedIndex] : null);
            }
        }

        void SetMessageDetails(MessageData? message)
        {
            m_MessageDetails.SetDisplay(message.HasValue);

            if (!message.HasValue)
            {
                return;
            }

            var m = message.Value;

            m_MessageSourceName.text = m.MessageType == MessageData.Type.Sent ? "Destination" : "Source";
            m_MessageSourceValue.text = m.Source;
            m_MessageAddressValue.text = m.OscAddress;
            m_MessageTagsValue.text = string.Join(string.Empty, m.Tags.Select(t => (char)t));

            m_MessageValuesList.Clear();

            for (var i = 0; i < m.Values.Length; i++)
            {
                var row = new VisualElement();

                var type = new Label
                {
                    name = "type",
                    text = m.Tags[i].ToString() ?? string.Empty,
                };
                var value = new Label
                {
                    name = "value",
                    text = m.Values[i] ?? string.Empty,
                };

                row.Add(type);
                row.Add(value);

                row.style.marginTop = 2f;
                row.style.marginBottom = 2f;
                row.style.flexDirection = FlexDirection.Row;
                type.AddToClassList(k_MessageValueNameLabelClass);
                value.focusable = true;
                MakeSelectable(value);

                m_MessageValuesList.Add(row);
            }
        }

        bool FilterMessage(MessageData message)
        {
            if (!(message.MessageType switch
            {
                MessageData.Type.Sent => Prefs.FilterSent,
                MessageData.Type.Received => Prefs.FilterReceived,
                _ => false,
            }))
            {
                return false;
            }

            if (string.IsNullOrEmpty(m_SearchText))
            {
                return true;
            }

            if (message.OscAddress.Contains(m_SearchText, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            for (var i = 0; i < message.Values.Length; i++)
            {
                if (message.Values[i].Contains(m_SearchText, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        void SetCaptureEnabled(bool captureEnabled)
        {
            Prefs.EnableMonitor = captureEnabled;

            OscClient.MessageSent -= OnMessageSent;
            OscServer.MessageReceived -= OnMessageReceived;

            if (captureEnabled)
            {
                OscClient.MessageSent += OnMessageSent;
                OscServer.MessageReceived += OnMessageReceived;
            }
        }

        void OnMessageSent(OscClient client, OscMessage message, string destination)
        {
            AddMessage(MessageData.Type.Sent, destination, message);
        }

        void OnMessageReceived(OscServer server, OscMessage message, string origin)
        {
            AddMessage(MessageData.Type.Received, origin, message);
        }

        void AddMessage(MessageData.Type type, string source, OscMessage message)
        {
            var sequenceNumber = Interlocked.Increment(ref m_SequenceNumber);
            var oscAddress = message.GetAddressPattern().ToString();
            var tags = new TypeTag[message.ArgumentCount];
            var values = new string[message.ArgumentCount];

            for (var i = 0; i < tags.Length; i++)
            {
                tags[i] = message.GetTag(i);
                values[i] = message.ReadString(i);
            }

            var data = new MessageData(sequenceNumber, type, source, oscAddress, tags, values);

            m_MessageQueue.Enqueue(data);
        }

        void SetClearOnPlay(bool clearOnPlay)
        {
            Prefs.ClearOnPlay = clearOnPlay;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            if (clearOnPlay)
            {
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (Prefs.ClearOnPlay && state == PlayModeStateChange.ExitingEditMode)
            {
                ClearMessageList();
            }
        }

        static void MakeSelectable(Label label)
        {
#if UNITY_2022_2_OR_NEWER
            (label as ITextSelection).isSelectable = true;
#else
            label.isSelectable = true;
#endif
        }
    }
}
