using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;

namespace RigidFrame_Development
{
    // Class for the purpose of creating a network connection to a controllable robot frame.
    public class Rframe_Network : MonoBehaviour {
		public string ipAddressOfRobotFrame;
		public int portNumber = 45833;
		public Text connectionStatus; // Text box for displaying the connection status
		Socket connectionForRobotSocket;
		public bool connectOnLoad = true;
		public InputField ipAddressInputFied;
		IPEndPoint ipe;

		public float networkSendWaitGap = 0.001f;
		bool anglesChanged = false; // Flag to indicate if the read angle has changed from before. Triggers network packet sending.
		float lastTime = 0.0f;

		float[] previousAngles = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
		// Use this for initialization
		void Start () {
			if(connectOnLoad) {
				createConnection();
			}



			//connectionForRobotSocket.Close();
		}
		
		// Update is called once per frame
		void Update () {
			// Collect the angles from each angle link
			bool ableToSend = true;
			if(connectionForRobotSocket!=null) {
				if(connectionForRobotSocket.Connected & Time.time > lastTime) {
					BMod_BalancePosition balPos = GetComponentInChildren<BMod_BalancePosition>();
					if(balPos!=null) {
						// Prevent the sending of movement commands until the balance positioner
						// has completed its start-up routine.
						if (balPos.balPosWorkingStatus==BMod_BalancePosition.balPolStatusEnum.startingUp) {
							ableToSend = false;
						}
					}
					if(ableToSend) {
						lastTime = Time.time + networkSendWaitGap;
						RFrame_AngleLink[] angleLinks = GetComponentsInChildren<RFrame_AngleLink>();
						string messageToSend = "|servo";
						anglesChanged = false;
						foreach (RFrame_AngleLink link in angleLinks) {
							float angleToSend = Mathf.Round(link.channelSendAngle * 10.0f)/10.0f;
							if(previousAngles[link.linkChannel]!=angleToSend) {
								anglesChanged = true;
								previousAngles[link.linkChannel] = angleToSend;
							}
							// An end tag "*" is added after each angle to ensure complete angle values are applied.
							// This is because during a network send, sometimes not all of the message is received,
							// and sometimes a valid angle is reveived but it is truncated. When running this causes
							// twitching.
							// This isn't required for the channel number since if the message is truncated that
							// far then there won't be any angle to apply. Plus the python code on the other end
							// filters out any incomplete channel-angle pairs.
							messageToSend = string.Concat(messageToSend, ",",link.linkChannel.ToString(), "-", angleToSend.ToString("0.00") + "*");
						}
						// Only send message if the angles have changed.
						if(anglesChanged) {
							byte[] msg = System.Text.Encoding.ASCII.GetBytes(messageToSend);
							connectionForRobotSocket.Send(msg);
						}
					}
				}
			}
		}

		public void endPythonScript() {
			// This is so the loop in the python script is stopped and the program can quit.
			if(connectionForRobotSocket!=null) {
					lastTime = Time.time + networkSendWaitGap;
					string messageToSend = "|endPython";
					byte[] msg = System.Text.Encoding.ASCII.GetBytes(messageToSend);
					connectionForRobotSocket.Send(msg);
			}
		}

		public void sendSaveStartupPosition() {
			// This sends a save command and the list of angles for the rigid frame to load
			// on startup rather than the degault 90 degrees.
			if(connectionForRobotSocket!=null) {
					lastTime = Time.time + networkSendWaitGap;
					string messageToSend = "|saveServo";
					RFrame_AngleLink[] angleLinks = GetComponentsInChildren<RFrame_AngleLink>();
					foreach (RFrame_AngleLink link in angleLinks) {
						messageToSend = string.Concat(messageToSend, ",",link.linkChannel.ToString(), "-", link.channelSendAngle.ToString());
					}
					byte[] msg = System.Text.Encoding.ASCII.GetBytes(messageToSend);
					connectionForRobotSocket.Send(msg);
			}
		}

		public void createConnection() {

			string ipToUse = ipAddressOfRobotFrame;
			if(ipAddressInputFied.text!=null) {
				ipToUse = ipAddressInputFied.text;
			}
			// Creating a socket to communucate over networky magic pipes
			connectionForRobotSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			
			// Collect stuff
			IPHostEntry ipHostInfo = Dns.GetHostEntry(ipToUse);
			// The IPAddress object to use isn't always the first in the list.
			// Roll through it and make sure to pull out the correct one matching the IP address
			// we're trying to communicate with.
			IPAddress robotIP = null; // = ipHostInfo.AddressList[0];
			foreach (IPAddress ipAddressCheck in ipHostInfo.AddressList) {
				if (ipAddressCheck.ToString()==ipToUse) {
					robotIP = ipAddressCheck;
				}
			}
			if (robotIP==null) {
				connectionStatus.text = "Could not find matching IPAdress object";
				return;
			}

			// Updating the status text box
			connectionStatus.text = string.Concat("Found : IP ", robotIP.ToString(), ":", portNumber);
			
			// Create the end point on the port number.
			ipe = new IPEndPoint(robotIP, portNumber);
			// Attempt to create the connection, otherwise return the error and splat it on the status
			// text box
			try {
				connectionForRobotSocket.Connect(ipe);
				connectionStatus.text = string.Concat(connectionStatus.text, " : Connected");
			} catch(System.ArgumentNullException ae) {  
				Debug.Log("ArgumentNullException : " + ae.ToString());
				connectionStatus.text = string.Concat(connectionStatus.text, " : ", ae.ToString()); 
			} catch(SocketException se) {  
				Debug.Log("SocketException : " + se.ToString()); 
				connectionStatus.text = string.Concat(connectionStatus.text, " : ", se.ToString()); 
			} catch(System.Exception e) {  
				Debug.Log("Unexpected exception : " + e.ToString());
				connectionStatus.text = string.Concat(connectionStatus.text, " : ", e.ToString());
			}  
		}

		public void breakConnection() {
			if(connectionForRobotSocket!=null) {
				connectionForRobotSocket.Disconnect(true);
			}
		}

	}
}
