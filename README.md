HomegearLib.NET
===============

HomegearLib.NET is an API to easily communicate with Homegear.

## Requirements

* Microsoft .NET-Framework 4.5

## Small C# usage example

First add HomegearLib.NET.dll as reference to your project. Now you can access the two main classes:

* RPCController: Handles all RPC communication
* Homegear: Main class to access Homegear features

### Create a new Homegear object

We need to create a RPC controller object first. The RPC controller bundles a RPC client and RPC server. The RPC client connects to your server running Homegear and is used for all RPC requests. Homegear again will connect to the RPC controller's RPC server. This connection is used to notify your program immediately on for example variable changes.

```
//Without SSL support:
RPCController rpc = new RPCController
					(
						"homegear", 	//Hostname of your server running Homegear
						2001,			//Port Homegear listens on
						"MyComputer",	//The hostname or ip address of the computer your program runs on
						"0.0.0.0",		//The ip address the callback event server listens on
						9876			//The port the callback event server listens on
					);

//With SSL support:
SSLClientInfo sslClientInfo = new SSLClientInfo
								(
									"MyComputer",	//Hostname of the computer your program runs on.
													//This hostname is used for certificate verification.
									"user",
									"secret",
									true			//Enable certificate verification
								);
//You can create the certificate file with: openssl pkcs12 -export -inkey YourPrivateKey.key -in YourCA.pem -in YourPublicCert.pem -out MyCertificate.pfx
SSLServerInfo sslServerInfo = new SSLServerInfo
								(
									"MyCertificate.pfx",	//Path to the certificate the callback server
															//will use.
									"secret",				//Certificate password
									"localUser",			//The username Homegear needs to use to connect
															//to our callback server
									"localSecret"			//The password Homegear needs to use to connect
															//to our callback server
								);
RPCController rpc = new RPCController("homegear", 2003, "MyComputer", "0.0.0.0", 9876, sslClientInfo, sslServerInfo);
```

Now we can instantiate a new Homegear object. Upon creation the Homegear object will connect to Homegear.

```
Homegear homegear = new Homegear(rpc);
```

The Homegear object automatically handles the connection to Homegear. It will reconnect automatically, when the connection is disrupted and also automatically tries to find all changes during the down time. There are no connection errors thrown. To still be able to find out, when there is no connection, there are five events:

* RPCController.ClientConnected: Raised, when the Homegear object managed to successfully connect to Homegear. Important: The event is also raised, when user authentication is not successful!
* RPCController.ClientDisconnected: Raised, when the connection to Homegear is closed.
* RPCController.ServerConnected: Raised, when there is a successful incoming connection from Homegear to the librarie's callback event server.
* RPCController.ServerDisconnected: Raised, when the incoming connection to our event server is closed.
* Homegear.ConnectError: Raised on all errors during the connection procedure.

"Homegear.ConnectError" is the most important. Here's an example implementation of an event handler:

```
	.
	.
	.
	homegear.ConnectError += homegear_OnConnectError;
	.
	.
	.

void homegear_OnConnectError(Homegear sender, string message, string stackTrace)
{
	WriteLog("Error connecting to Homegear: " + message + "\r\nStacktrace: " + stackTrace);
}
```

After this we need need to implement the event "Homegear.Reloaded". This event is always raised, when a full reload is successfully completed. We have to wait for the first "reload" to complete, before we can work with the Homegear object:

```
	.
	.
	.
	homegear.Reloaded += homegear_OnReloaded;
	.
	.
	.

void homegear_OnReloaded(Homegear sender)
{
	WriteLog("Reload complete. Received " + sender.Devices.Count + " devices.");
	//Now we can start working with the Homegear object
	StartWorking();
}
```

Now you can access all of Homegear's features through the Homegear object. You don't need to think about the RPC communication. Everything is automagically handled ;-) and data is automatically requested from Homegear when needed.

#### Other events you should implement an event handler for

The Homegear object does not update any data by itself, which causes objects to be invalidated. Instead Homegear raises "reload events". When such an event is raised, you can finish all operations on the object needing a reload and then call the "reload method" of the object. Here's an example implementation:

```
	.
	.
	.
	homegear.ReloadRequired += homegear_OnReloadRequired;
    homegear.DeviceReloadRequired += homegear_OnDeviceReloadRequired;
	.
	.
	.

void homegear_OnReloadRequired(Homegear sender, ReloadType reloadType)
{
	if (reloadType == ReloadType.Full)
	{
		WriteLog("Received reload required event. Reloading.");
		//Finish all operations on the Homegear object and then call:
		homegear.Reload();
	}
	else if(reloadType == ReloadType.SystemVariables)
	{
		WriteLog("Reloading system variables.");
		//Finish all operations on the system variables and then call:
		homegear.SystemVariables.Reload();
	}
	else if (reloadType == ReloadType.Events)
	{
		WriteLog("Reloading timed events.");
		//Finish all operations on the timed events and then call:
		homegear.TimedEvents.Reload();
	}
}

void homegear_OnDeviceReloadRequired(Homegear sender, Device device, Channel channel, DeviceReloadType reloadType)
{
	if(reloadType == DeviceReloadType.Full)
	{
		WriteLog("Reloading device " + device.ID.ToString() + ".");
		//Finish all operations on the device and then call:
		device.Reload();
	}
	else if(reloadType == DeviceReloadType.Metadata)
	{
		WriteLog("Reloading metadata of device " + device.ID.ToString() + ".");
		//Finish all operations on the device's metadata and then call:
		device.Metadata.Reload();
	}
	else if (reloadType == DeviceReloadType.Channel)
	{
		WriteLog("Reloading channel " + channel.Index + " of device " + device.ID.ToString() + ".");
		//Finish all operations on the device's channel and then call:
		channel.Reload();
	}
	else if (reloadType == DeviceReloadType.Links)
	{
		WriteLog("Device links were updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString());
		WriteLog("Reloading links of channel " + channel.Index + " and device " + device.ID.ToString() + ".");
		//Finish all operations on the channels's links and then call:
		channel.Links.Reload();
	}
	else if(reloadType == DeviceReloadType.Team)
	{
		WriteLog("Device team was updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString());
		WriteLog("Reloading channel " + channel.Index + " of device " + device.ID.ToString() + ".");
		//Finish all operations on the device's channel and then call:
		channel.Reload();
	}
	else if(reloadType == DeviceReloadType.Events)
	{
		WriteLog("Device events were updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString());
		WriteLog("Reloading events of device " + device.ID.ToString() + ".");
		//Finish all operations on the device's events and then call:
		device.Events.Reload();
	}
}
```

This is all the important setup stuff. There are a bunch of other events, which you can implement as needed:

* homegear.SystemVariableUpdated
* homegear.MetadataUpdated
* homegear.DeviceVariableUpdated
* homegear.DeviceConfigParameterUpdated:
* homegear.DeviceLinkConfigParameterUpdated:
* homegear.EventUpdated

### Change the value of a device variable

Using the library should be self-explanatory. Just type "homegear." and IntelliSense will show you all available options. As an example let's set a device's variable. All devices can be accessed through the "Devices" property of the Homegear object. This property basically is a dictionary with the id of the device as key and the device object as value.
Every device again has a number of channels. One channel for example represents a button of a remote or one output of an actor. All device's variables can be accessed through the "Channels" property. This property again is a dictionary with the index of the channel as key and the channel object as value.
The "Variables" property of the channel object is a dictionary with the name of the variable as key and the variable object as value.

Here's now how to set the "STATE" variable of the first output of an actor as an example:

```
homegear.Devices[myActorId].Channels[1].Variables["STATE"].BooleanValue = true
```