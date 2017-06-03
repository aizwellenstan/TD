﻿using System;
using System.Collections.Generic;
using ExitGames.Client.Photon.Chat;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This simple Chat UI demonstrate basics usages of the Chat Api
/// </summary>
/// <remarks>
/// The ChatClient basically lets you create any number of channels.
///
/// some friends are already set in the Chat demo "DemoChat-Scene", 'Joe', 'Jane' and 'Bob', simply log with them so that you can see the status changes in the Interface
///
/// Workflow:
/// Create ChatClient, Connect to a server with your AppID, Authenticate the user (apply a unique name,)
/// and subscribe to some channels.
/// Subscribe a channel before you publish to that channel!
///
///
/// Note:
/// Don't forget to call ChatClient.Service() on Update to keep the Chatclient operational.
/// </remarks>
public class ChatGui : MonoBehaviour, IChatClientListener
{

    public string[] ChannelsToJoinOnConnect; // set in inspector. Demo channels to join automatically.

    public string[] FriendsList;

    public int HistoryLengthToFetch; // set in inspector. Up to a certain degree, previously sent messages can be fetched for context

    public string UserName { get; set; }

    private string selectedChannelName; // mainly used for GUI/input

    public ChatClient chatClient;

    public GameObject ConnectingLabel;

    public RectTransform ChatPanel;     // set in inspector (to enable/disable panel)

    public InputField InputFieldChat;   // set in inspector
    public Text CurrentChannelText;     // set in inspector  

    private readonly Dictionary<string, Toggle> channelToggles = new Dictionary<string, Toggle>();

    private readonly Dictionary<string, FriendItem> friendListItemLUT = new Dictionary<string, FriendItem>();

    public bool ShowState = true; 
    public Text StateText; // set in inspector
    public Text UserIdText; // set in inspector

    // private static string WelcomeText = "Welcome to chat. Type \\help to list commands.";
    private static string HelpText = "\n    -- HELP --\n" +
        "To subscribe to channel(s):\n" +
            "\t<color=#E07B00>\\subscribe</color> <color=green><list of channelnames></color>\n" +
            "\tor\n" +
            "\t<color=#E07B00>\\s</color> <color=green><list of channelnames></color>\n" +
            "\n" +
            "To leave channel(s):\n" +
            "\t<color=#E07B00>\\unsubscribe</color> <color=green><list of channelnames></color>\n" +
            "\tor\n" +
            "\t<color=#E07B00>\\u</color> <color=green><list of channelnames></color>\n" +
            "\n" +
            "To switch the active channel\n" +
            "\t<color=#E07B00>\\join</color> <color=green><channelname></color>\n" +
            "\tor\n" +
            "\t<color=#E07B00>\\j</color> <color=green><channelname></color>\n" +
            "\n" +
            "To send a private message:\n" +
            "\t\\<color=#E07B00>msg</color> <color=green><username></color> <color=green><message></color>\n" +
            "\n" +
            "To change status:\n" +
            "\t\\<color=#E07B00>state</color> <color=green><stateIndex></color> <color=green><message></color>\n" +
            "<color=green>0</color> = Offline " +
            "<color=green>1</color> = Invisible " +
            "<color=green>2</color> = Online " +
            "<color=green>3</color> = Away \n" +
            "<color=green>4</color> = Do not disturb " +
            "<color=green>5</color> = Looking For Group " +
            "<color=green>6</color> = Playing" +
            "\n\n" +
            "To clear the current chat tab (private chats get closed):\n" +
            "\t<color=#E07B00>\\clear</color>";


    public void Start()
    {
        UserIdText.text = "";
        StateText.text = "";
        StateText.gameObject.SetActive(true);
        UserIdText.gameObject.SetActive(true); 
        ChatPanel.gameObject.SetActive(false);
        ConnectingLabel.SetActive(false);
        if (string.IsNullOrEmpty(UserName))
        {
            UserName = "user" + Environment.TickCount % 99; //made-up username
        }
        if (string.IsNullOrEmpty(PhotonNetwork.PhotonServerSettings.ChatAppID))
        {
            Debug.LogError("You need to set the chat app ID in the PhotonServerSettings file in order to continue.");
            return;
        }
    }

    public void Connect()
    {
        this.chatClient = new ChatClient(this);
        this.chatClient.Connect(PhotonNetwork.PhotonServerSettings.ChatAppID, "1.0", new ExitGames.Client.Photon.Chat.AuthenticationValues(UserName));
 
        Debug.Log("Connecting as: " + UserName);

        ConnectingLabel.SetActive(true);
    }

    /// <summary>To avoid that the Editor becomes unresponsive, disconnect all Photon connections in OnApplicationQuit.</summary>
    public void OnApplicationQuit()
    {
        if (this.chatClient != null)
        {
            this.chatClient.Disconnect();
        }
    }

    public void Update()
    {
        if (this.chatClient != null)
        {
            this.chatClient.Service(); // make sure to call this regularly! it limits effort internally, so calling often is ok!
        }

        // check if we are missing context, which means we got kicked out to get back to the Photon Demo hub.
        if (this.StateText == null)
        {
            Destroy(this.gameObject);
            return;
        }

        this.StateText.gameObject.SetActive(ShowState); // this could be handled more elegantly, but for the demo it's ok.
    }


    public void OnEnterSend()
    {
        if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
        {
            SendChatMessage(this.InputFieldChat.text);
            this.InputFieldChat.text = "";
        }
    }

    public void OnClickSend()
    {
        if (this.InputFieldChat != null)
        {
            SendChatMessage(this.InputFieldChat.text);
            this.InputFieldChat.text = "";
        }
    }


    public int TestLength = 2048;
    private byte[] testBytes = new byte[2048];

    private void SendChatMessage(string inputLine)
    {
        if (string.IsNullOrEmpty(inputLine))
        {
            return;
        }
        if ("test".Equals(inputLine))
        {
            if (this.TestLength != this.testBytes.Length)
            {
                this.testBytes = new byte[this.TestLength];
            }

            this.chatClient.SendPrivateMessage(this.chatClient.AuthValues.UserId, testBytes, true);
        }

        foreach(string channel in ChannelsToJoinOnConnect)
        {
            chatClient.PublishMessage(channel, inputLine);
        }
    }

    public void PostHelpToCurrentChannel()
    {
        this.CurrentChannelText.text += HelpText;
    }

    public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string message)
    {
        if (level == ExitGames.Client.Photon.DebugLevel.ERROR)
        {
            UnityEngine.Debug.LogError(message);
        }
        else if (level == ExitGames.Client.Photon.DebugLevel.WARNING)
        {
            UnityEngine.Debug.LogWarning(message);
        }
        else
        {
            UnityEngine.Debug.Log(message);
        }
    }

    public void OnConnected()
    {
        if (this.ChannelsToJoinOnConnect != null && this.ChannelsToJoinOnConnect.Length > 0)
        {
            this.chatClient.Subscribe(this.ChannelsToJoinOnConnect, this.HistoryLengthToFetch);
        }

        ConnectingLabel.SetActive(false);

        UserIdText.text = "Connected as " + this.UserName;

        this.ChatPanel.gameObject.SetActive(true);

        if (FriendsList != null && FriendsList.Length > 0)
        {
            this.chatClient.AddFriends(FriendsList); // Add some users to the server-list to get their status updates 
        } 

        this.chatClient.SetOnlineStatus(ChatUserStatus.Online); // You can set your online state (without a mesage).
    }

    public void OnDisconnected()
    {
        ConnectingLabel.SetActive(false);
    }

    public void OnChatStateChange(ChatState state)
    {
        // use OnConnected() and OnDisconnected()
        // this method might become more useful in the future, when more complex states are being used.

        this.StateText.text = state.ToString();
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        // in this demo, we simply send a message into each channel. This is NOT a must have!
        foreach (string channel in channels)
        {
            this.chatClient.PublishMessage(channel, "says 'hi'."); // you don't HAVE to send a msg on join but you could.
        }

        Debug.Log("OnSubscribed: " + string.Join(", ", channels));

        /*
        // select first subscribed channel in alphabetical order
        if (this.chatClient.PublicChannels.Count > 0)
        {
            var l = new List<string>(this.chatClient.PublicChannels.Keys);
            l.Sort();
            string selected = l[0];
            if (this.channelToggles.ContainsKey(selected))
            {
                ShowChannel(selected);
                foreach (var c in this.channelToggles)
                {
                    c.Value.isOn = false;
                }
                this.channelToggles[selected].isOn = true;
                AddMessageToSelectedChannel(WelcomeText);
            }
        }
        */

        // Switch to the first newly created channel
        ShowChannel(channels[0]);
    }
     

    public void OnUnsubscribed(string[] channels)
    {
        foreach (string channelName in channels)
        {
            if (this.channelToggles.ContainsKey(channelName))
            {
                Toggle t = this.channelToggles[channelName];
                Destroy(t.gameObject);

                this.channelToggles.Remove(channelName);

                Debug.Log("Unsubscribed from channel '" + channelName + "'.");

                // Showing another channel if the active channel is the one we unsubscribed from before
                if (channelName == selectedChannelName && channelToggles.Count > 0)
                {
                    IEnumerator<KeyValuePair<string, Toggle>> firstEntry = channelToggles.GetEnumerator();
                    firstEntry.MoveNext();

                    ShowChannel(firstEntry.Current.Key);

                    firstEntry.Current.Value.isOn = true;
                }
            }
            else
            {
                Debug.Log("Can't unsubscribe from channel '" + channelName + "' because you are currently not subscribed to it.");
            }
        }
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        if (channelName.Equals(this.selectedChannelName))
        {
            // update text
            ShowChannel(this.selectedChannelName);
        }
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {
        byte[] msgBytes = message as byte[];
        if (msgBytes != null)
        {
            Debug.Log("Message with byte[].Length: " + msgBytes.Length);
        }
        if (this.selectedChannelName.Equals(channelName))
        {
            ShowChannel(channelName);
        }
    }

    /// <summary>
    /// New status of another user (you get updates for users set in your friends list).
    /// </summary>
    /// <param name="user">Name of the user.</param>
    /// <param name="status">New status of that user.</param>
    /// <param name="gotMessage">True if the status contains a message you should cache locally. False: This status update does not include a
    /// message (keep any you have).</param>
    /// <param name="message">Message that user set.</param>
    public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
    {

        Debug.LogWarning("status: " + string.Format("{0} is {1}. Msg:{2}", user, status, message));

        if (friendListItemLUT.ContainsKey(user))
        {
            FriendItem _friendItem = friendListItemLUT[user];
            if (_friendItem != null) _friendItem.OnFriendStatusUpdate(status, gotMessage, message);
        }
    }

    public void AddMessageToSelectedChannel(string msg)
    {
        ChatChannel channel = null;
        bool found = this.chatClient.TryGetChannel(this.selectedChannelName, out channel);
        if (!found)
        {
            Debug.Log("AddMessageToSelectedChannel failed to find channel: " + this.selectedChannelName);
            return;
        }

        if (channel != null)
        {
            channel.Add("Bot", msg);
        }
    }



    public void ShowChannel(string channelName)
    {
        if (string.IsNullOrEmpty(channelName))
        {
            return;
        }

        ChatChannel channel = null;
        bool found = this.chatClient.TryGetChannel(channelName, out channel);
        if (!found)
        {
            Debug.Log("ShowChannel failed to find channel: " + channelName);
            return;
        }

        this.selectedChannelName = channelName;
        this.CurrentChannelText.text = channel.ToStringMessages();
        Debug.Log("ShowChannel: " + this.selectedChannelName);

        foreach (KeyValuePair<string, Toggle> pair in channelToggles)
        {
            pair.Value.isOn = pair.Key == channelName ? true : false;
        }
    }

    public void OpenDashboard()
    {
        Application.OpenURL("https://www.photonengine.com/en/Dashboard/Chat");
    }




}