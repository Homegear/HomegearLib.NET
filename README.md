HomegearLib.NET
===============

HomegearLib.NET is an API written for .NET Standard 2.0 to easily communicate with Homegear.

[![NuGet](https://img.shields.io/nuget/v/HomegearLib.NET.svg?maxAge=2592000)](https://www.nuget.org/packages/HomegearLib.NET/)

## Requirements

One of

* Microsoft .NET Core Framework 2.0 or later
* Microsoft .NET Framework 4.6.1 or later
* Mono 5.4 or later
* Xamarin.iOS 10.14 or later
* Xamarin.Mac 3.8 or later
* Xamarin.Android 7.5 or later
* Upcoming version of UWP

## Small C# usage example

First add HomegearLib.NET.dll as reference to your project. Now you can access the two main classes:

* **RPCController:** Handles all RPC communication
* **Homegear:** Main class to access Homegear features

### Create a new Homegear object

We need to create a RPC controller object first. The controller connects to your server running Homegear and is used for all RPC requests. Homegear will also send notifications immediately over this connection on for example variable changes.

```
//Without SSL support:
RPCController rpc = new RPCController(
		"homegear", 	//Hostname of your server running Homegear
		2001
	);

//With SSL support and no authentication:
SslInfo sslInfo = new SslInfo();

RPCController rpc = new RPCController("homegear", 2003, sslInfo);

//With SSL support and username/password:
SslInfo sslInfo = new SslInfo(
		new Tuple<string, string>("user", "secret"),
		true			//Enable hostname verification
	);

RPCController rpc = new RPCController("homegear", 2003, sslInfo);

//With SSL support and client certificate authentication:
SslInfo sslInfo = new SslInfo(
		"Path to PKCS #12 certificate file",
		"secret",
		true			//Enable hostname verification
	);

RPCController rpc = new RPCController("homegear", 2003, sslInfo);
```

Now we can instantiate a new Homegear object. Upon creation the Homegear object will connect to Homegear. The second parameter defines, whether the library should receive notifications from Homegear. When set to "true" you will receive device state changes from Homegear immediately. As this is not always needed - i. e. when you only want to set values - it can be disabled by specifying "false" here to reduce the load on the Homegear system and the network load.

```
Homegear homegear = new Homegear(rpc, true);
```

The Homegear object automatically handles the connection to Homegear. It will reconnect automatically, when the connection is disrupted and also automatically tries to find all changes during the down time. There are no connection errors thrown. To still be able to find out, when there is no connection, there are three events:

Event | Description
--- | ---
**RPCController.Connected** | Raised, when the Homegear object managed to successfully connect to Homegear. Important: The event is also raised, when user authentication is not successful!
**RPCController.Disconnected** | Raised, when the connection to Homegear is closed.
**Homegear.ConnectError** | Raised on all errors during the connection procedure.

"**Homegear.ConnectError**" is the most important. Here's an example implementation of an event handler:

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

After this we need to implement the event "**Homegear.Reloaded**". This event is always raised, when a full reload is successfully completed. We have to wait for the first "reload" to complete, before we can work with the Homegear object:

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
	else if (reloadType == DeviceReloadType.Variables)
	{
		WriteLog("Device variables were updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString());
		WriteLog("Reloading variables of channel " + channel.Index + " and device " + device.ID.ToString() + ".");
		//Finish all operations on the channels's variables and then call:
		channel.Variables.Reload();
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

* **homegear.HomegearError**
* **homegear.SystemVariableUpdated**
* **homegear.MetadataUpdated**
* **homegear.DeviceVariableUpdated**
* **homegear.DeviceConfigParameterUpdated**
* **homegear.DeviceLinkConfigParameterUpdated**
* **homegear.EventUpdated**

### Exceptions

Name | Description
--- | ---
**HomegearException** | Base class for all library specific exceptions.
**HomegearVariableException** | Base class for variable/config parameter exceptions.
**HomegearVariableReadOnlyException**| Thrown when you try to set a read only variable.
**HomegearVariableTypeException** | Thrown when the type of the value you try to set does not match the variable type.
**HomegearVariableValueOutOfBoundsException** | Thrown when the value you try to set is not withing the range of valid values.
**HomegearRpcClientException** | Thrown on all errors withing the RPC client. **You should always catch this exception, as it can be thrown by pretty much all operations.**
**HomegearRpcClientSSLException** | Thrown on SSL specific errors within the RPC client.

### Disposing

To orderly destroy the Homegear object and to orderly disconnect from Homegear, call "**Dispose**". This might take a few seconds.

```
homegear.Dispose()
```

### Change the value of a device variable

Using the library should be self-explanatory. Just type "homegear." and IntelliSense will show you all available options. As an example let's set a device's variable. All devices can be accessed through the "Devices" property of the Homegear object. This property basically is a dictionary with the id of the device as key and the device object as value.
Every device again has a number of channels. One channel for example represents a button of a remote or one output of an actor. All device's variables can be accessed through the "Channels" property. This property again is a dictionary with the index of the channel as key and the channel object as value.
The "Variables" property of the channel object is a dictionary with the name of the variable as key and the variable object as value.

Here's now how to set the "STATE" variable of the first output of an actor as an example:

```
homegear.Devices[myActorId].Channels[1].Variables["STATE"].BooleanValue = true
```

### Change the value of a configuration parameter

As changing configuration parameters might cause a lot of traffic, the library doesn't submit the configuration value changes immediately to Homegear. Instead you can change as many configuration parameters as you want. When you are finished, call the method ```put``` to commit your changes.

```
homegear.Devices[myActorId].Channels[0].Config["ROAMING"].BooleanValue = true
_selectedDevice.Channels[_selectedVariable.Channel].Config.Put();
```