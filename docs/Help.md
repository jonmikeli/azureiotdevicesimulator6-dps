﻿# Azure IoT Device Simulator (.NET 5, C#) - DPS version - Help

This section describes the different artifacts of the solution and how they work.

## Application
The application consists of:
 - an application console (.NET 5, C#)
 - configuration files:
   - [appsettings.json](####appsettings.json) (described below)
   - [devicesettings.json](####devicesettings.json) (described below)
   - [modulessettings.json](####modulessettings.json) (described below)
   - [dpssettings.json](https://github.com/jonmikeli/azureiotdevicesimulator5-dps/blob/master/sources/IoT.Simulator/IoT.Simulator/docs/Provisioning.md)
 - message template files
   - [commissioning.json](####comissioning.json) (examples below)
   - [error.json](####error.json) (examples below)
   - [measureddata.json](####measureddata.json) (examples below)

<br/>

*Global device model architecture*


![Global device model architecture](images/GlobalDiagrams.png)

<br/>

## Features
### Device
#### D2C
##### Messages
1. [Commissioning](##Commissioning) messages
2. [Measured](####measureddata.json) data messages (aka telemetry)
3. [Error](####error.json) messages (functional errors sent by devices)

##### Twins
The device sends updated Reported Properties (Twins) after many operations/commands.

> [!NOTE]
> 
> Example: after an OnOff Direct Method request, the device sends its status to the cloud solution (Microsoft Azure IoT Hub) using the Twin Reported Properties.

#### C2D
##### Direct Methods

|Method name|Description|Request|Response|Comments|
|:-|:-|:-|:-|:-|
| SendLatencyTest | Allows to start a [latency](LatencyTests.md) test between a given device and the Microsoft Azure IoT Hub where the device is registered. | ```{ "deviceId":"", "messageType":"latency", "startTimestamp":12345} ```| NA |The request contains the initial  timpestamp, which is sent back to the device after all the process in order to allow him to measure latency. <br>***NOTE: this feature requires an [Azure Function](https://github.com/jonmikeli/azureiotdevicesimulator/tree/master/sources/IoT.Simulator/IoT.Simulator.AF) responding to the latency test requests and calling back the C2D LatencyTestCallBack Direct Method.***|
|LatencyTestCallBack|Allows to end a [latency](LatencyTests.md) test between a given device and the Microsoft Azure IoT Hub where the device is registered. |```startTimestamp``` value, allowing to math the [latency](LatencyTests.md) (string)|<ul><li>message notifying that the LatencyTestCallBack Direct Method has been called (string).</li><li> result code, 200</li></ul>|NA|
| Reboot | Simulates a device reboot operation. | NA | <ul><li>message notifiying that the Reboot Direct Method has been called (string).</li><li> result code, 200</li></ul>|Sends Twins (Reported properties) notifying the reboot.|
| OnOff | Turns a given device on/off. | JSON Object | <ul><li>message notifying that the OnOff Direct Method has been called (string). The message contains request's payload.</li><li> result code, 200</li></ul>|
| ReadTwins | Orders a given device to read its Twin data. | NA | <ul><li>message notifying that the ReadTwins Direct Method has been called (string).</li><li>result code, 200</li></ul>|
| GenericJToken | Generic method | JSON Token | <ul><li>message notifying that the GenericJToken Direct Method has been called (string).</li><li> result code, 200</li></ul>|
| Generic | Generic method | string | <ul><li>message notifying that the Generic Direct Method has been called (string).</li><li> result code, 200</li></ul>|
| SetTelemetryInterval | Updates the time rate used to send telemetry data. | seconds (int) | <ul><li>message notifying that the SetTelemetryInterval Direct Method has been called (string).</li><li> result code, 200</li></ul>|


##### Messages
The device can be configured to receive generic **messages** coming from the cloud (Microsoft Azure IoT Hub C2D Messages).

##### Twins
###### Desired
Any change in a **Desired Property** (device level) is notified to the device and it can be handled.


> ![NOTE]
> 
> ###### Tags and Microsoft IoT Hub Jobs
>
> The simulator can benefit from the use of **Microsoft IoT Hub Jobs** for operations based in property queries.
> A typical example of this would be a **FOTA** (Firmware Over The Air) update according to criteria based in **Twin.Tag properties* (ex: firmwareVersion, location, environment, etc).


### Modules

#### M2C
##### Messages
1. [Commissioning](##Commissioning) messages
2. [Telemetry](####measureddata.json) data messages (aka telemetry)
3. [Error](####error.json) messages (functional errors sent by devices)

##### Twins
Modules send updated **Reported Properties (Twins)** after many operations/commands.

> [!NOTE]
> 
> Example: after a OnOff Direct Method request, a givne module sends its status to the cloud solution (Microsoft Azure IoT Hub) using the Twin Reported Properties.

#### C2M
##### Direct Methods

|Method name |Description|Request|Response|Comments|
|:-|:-|:-|:-|:-|
| Reboot | Simulates a device reboot operation. | NA | <ul><li>message notifying that the Reboot Direct Method has been called (string).</li><li> result code, 200</li></ul>|Sends Twins (Reported properties) notifying the reboot.|
| OnOff | Turns a given device on/off. | JSON Object | <ul><li>message notifying that the OnOff Direct Method has been called (string). The message contains request's payload.</li><li> result code, 200</li></ul>|
| ReadTwins | Orders a given device to read its Twin data. | NA | <ul><li>message notifying that the ReadTwins Direct Method has been called (string).</li><li>result code, 200.</li></ul>|
| GenericJToken | Generic method | JSON Token | <ul><li>message notifying that the GenericJToken Direct Method has been called (string).</li><li> result code, 200</li></ul>|
| Generic | Generic method | string | <ul><li>message notifying that the Generic Direct Method has been called (string).</li><li> result code, 200</li></ul>|
| SetTelemetryInterval | Updates the time rate used to send telemetry data. | seconds (int) | <ul><li>message notifying that the SetTelemetryInterval Direct Method has been called (string).</li><li> result code, 200</li></ul>|


##### Messages
Each module can be configured to receive generic **messages** coming from the cloud (Microsoft Azure IoT Hub).

##### Twins
###### Desired
Any change in a **Desired property** (***module level***) is notified to the module and it can be handled.



## How does the simulator work?
### Description
The application is configurable by an ***appsettings.json*** file.

The features of the application rely on two main components:
 - device (**one single device per application**)
 - modules (**none, one or many modules per device**)
 
 The device component is configured by a ***devicesettings.json*** file while the modules are configured by a ***modulessettings.json*** file.

<br/>

 *Device model architecture*
 ![Device model architecture](images/GlobalDiagrams.png)

<br/>

### Runing the simulator
 The simulator is a .NET 5 application.
 
 To run the simulator, there are two alternatives:
  1. running the simulator as a **.NET 5 application** (Console Application). Besides the general performance improvement and the cross-platform orientation, .NET 5 includes very interesting capabilities to package the applications. These capabilities can be especially useful in IoT solutions (upcoming publications will focus on this point).
  1. running the *Docker container* (which contains in turn the .NET 5 binaries, packages and other required prerequisites)
  
 > ![NOTE]
 > 
 > See [Docker](https://www.docker.com) and container oriented development for those who are not familiar with.
 
 Whatever the alternative will be, check that all the **configuration** files are set properly.

 > ![IMPORTANT]
 > 
 > The configurations files have to be present and contain the proper Microsoft Azure IoT Hub connection strings, IDs or keys.


 ### Configurations
 #### Application
 Technical settings of the application can be configured at *appsettings.json*.

 > Example (Production environment):
 ```json
 {
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "DeviceManagementServiceSettings": {
    "BaseUrl": "[TO BE REPLACED]",
    "AddModulesToDeviceRoute": "modules/add",
    "AllowAutosignedSSLCertificates": true
  }
}
 ```


 > [!NOTE]
 >
 > The solution contains different settings depending on the environment (similar to transformation files).


 > Example (Development environment) - *appsettings.Development.json*:
  ```json
 {
  "Logging": {
    "Debug": {
      "LogLevel": {
        "Default": "Trace"
      }
    },
    "Console": {
      "IncludeScopes": true,
      "LogLevel": {
        "Default": "Trace"
      }
    },
    "LogLevel": {
      "Default": "Trace",
      "System": "Trace",
      "Microsoft": "Trace"
    }
  },
   "DeviceManagementServiceSettings": {
    "BaseUrl": "[TO BE REPLACED]",
    "AddModulesToDeviceRoute": "modules/add",
    "AllowAutosignedSSLCertificates": true
  }
}
 ```


#### Device
IoT Simulator is linked to **one and only one** device.
The device behavior is configured by the *devicessettings.json* configuration file.

> Example:

```json
{
  "deviceId": "[REQUIRED]",
  "connectionString": "[GENERATED BY THE PROVISIONING PROCESS-NO NEED TO FEED IT]",
  "simulationSettings": {
    "enableLatencyTests": false,
    "latencyTestsFrecuency": 10,
    "enableDevice": true,
    "enableModules": true,
    "enableTelemetryMessages": false,
    "telemetryFrecuency": 60,
    "enableErrorMessages": false,
    "errorFrecuency": 60,
    "enableCommissioningMessages": false,
    "commissioningFrecuency": 60,
    "enableTwinReportedMessages": false,
    "twinReportedMessagesFrecuency": 60,
    "enableReadingTwinProperties": false,
    "enableC2DDirectMethods": true,
    "enableC2DMessages": true,
    "enableTwinPropertiesDesiredChangesNotifications": true
  }
}
```
Properties are quite self-explanatory.

> [!NOTE]
> 
> Emission intervals are set in seconds.
> The device Id may be set through an environment variable too (PROVISIONING_REGISTRATION_ID) and it overwrites the settings in the json file.
>
> The device Id is mandatory (either through the environment variable or the devicesettings.json file).


#### Modules
A simulated device may contain **zero, one or more modules but no module is mandatory**.
Behaviors of the modules are configured by the *modulessettings.json* configuration file.


> Example of a configuration file of two modules:
```json
{
 "modules":[
    {
      "deviceId": "[FED BY THE PROCESS]",
      "moduleId": "[REQUIRED]",
      "connectionString": "",      
      "simulationSettings": {
        "enableLatencyTests": false,
        "latencyTestsFrecuency": 10,
        "enableTelemetryMessages": true,
        "telemetryFrecuency": 20,
        "enableErrorMessages": false,
        "errorFrecuency": 30,
        "enableCommissioningMessages": false,
        "commissioningFrecuency": 60,
        "enableTwinReportedMessages": false,
        "twinReportedMessagesFrecuency": 60,
        "enableReadingTwinProperties": true,
        "enableC2DDirectMethods": true,
        "enableC2DMessages": true,
        "enableTwinPropertiesDesiredChangesNotifications": true
      }
    },
    {
      "deviceId": "[FED BY THE PROCESS]",
      "moduleId": "[REQUIRED]",
      "connectionString": "",      
      "simulationSettings": {
        "enableLatencyTests": false,
        "latencyTestsFrecuency": 10,
        "enableTelemetryMessages": true,
        "telemetryFrecuency": 20,
        "enableErrorMessages": false,
        "errorFrecuency": 30,
        "enableCommissioningMessages": false,
        "commissioningFrecuency": 60,
        "enableTwinReportedMessages": false,
        "twinReportedMessagesFrecuency": 60,
        "enableReadingTwinProperties": true,
        "enableC2DDirectMethods": true,
        "enableC2DMessages": true,
        "enableTwinPropertiesDesiredChangesNotifications": true
      }
    }
  ]
}
```

> [!NOTE]
> 
> Emission intervals are set in seconds.
>
> The module Id is mandatory.


#### DPS Settings

Example
```json
{
  "dpsSettings": {
    "enrollmentType": "Group", //Group
    "groupEnrollmentSettings": {
      "securityType": "SymmetricKey", //SymmetricKey, X509CA
      "symmetricKeySettings": {
        "idScope": "0ne000E3E14",
        "primaryKey": "wi3VEjzWZpxhPlWT85O8sg/hZvqk2sNPHPDsP+M9v73BKs9NQHky+Tvg/IFNu1QEWqt5OPZuz1Ia/9IM6R+rbb==",
        "enrollmentType": "Group",
        "globalDeviceEndpoint": "global.azure-devices-provisioning.net",
        "transportType": "Mqtt"
      },
      "CAX509Settings": {
        "idScope": "",
        "deviceX509Path": "",
        "password": "",
        "enrollmentType": "Group",
        "globalDeviceEndpoint": "global.azure-devices-provisioning.net",
        "transportType": "Mqtt"
      }
    }
  }
}
```


### Messages

The messages below are basic proposals to start working. You will probably need to adjust them to each of your IoT projects taking into account your customers' requirements.

> [!TIP]
> 
> `deviceId` and `messageType` fields have been included in the message to simplify processes in the backend side.
> Indeed, even though these two properties can be reachable in the metadata of the message, having them inside the message itself brings simplicity to richer scenarios (ex: gateways sending messages in behalf of devices) and storing that information in the repository (autosufficient message). 
> Debugging and message routing in Microsoft Azure IoT Hub are other of the fields that benefit from this practice.

This being said, if at some point you need to avoid including that information in the message, feel free to do it. Routing could also work on metadata.

> [!NOTE]
>
> Microsoft Azure IoT Hub does not require the devices to send JSON messages.
> If your project requires other formats (ex: very small hexadecimal messages, frequent in IoT small devices), Microsoft Azure IoT Hub has no problem in processing them. Also, you can still use Azure IoT Device Simulator; it will require small code changes in the Message Service but all the remaining implemented features remain valid.

> [!WARNING]
> Not all the IoT projects can implement C2D features. Devices and communication platforms have to take into account the requirements of such features.


#### commissioning.json
```json
{
  "deviceId": "",
  "messageType": "commissioning",
  "timestamp": 13456,
  "userId": "",
  "building": {
    "buildingId": "",
    "floor": "",
    "departmentId": "",
    "roomId": null
  }
} 
```

> [!WARNING]
> 
> No `moduleId` is required since commissioning is related to devices and only devices (functional choice).


#### error.json
```json
{
  "deviceId": "",
  "moduleId": "",
  "messageType": "error",
  "errorCode": "code",
  "errorSeverity": "severity",
  "errorStatus": "status",
  "timestamp": 13456
}
```

> [!WARNING]
> 
> `moduleId` can be empty in case the message is sent by a device.

#### measureddata.json

We consider that each item (device or module) can send many "measured data" in a single message.
This responds to data flow optimization scenarios and explains the chosen message schema.

```json
{
  "deviceId": "",
  "moduleId": "",
  "timestamp": 0,
  "schemaVersion": "v1.0",
  "messageType": "data",
  "data": [
    {
      "timestamp": 0,
      "propertyName": "P1",
      "propertyValue": 35,
      "propertyUnit": "T",
      "propertyDivFactor": 1
    },
    {
      "timestamp": 0,
      "propertyName": "P2",
      "propertyValue": 1566,
      "propertyUnit": "U",
      "propertyDivFactor": 10
    }
  ]
}
```

> [!WARNING]
> 
> This version includes a dependency between message templates and message service implementation (randomized values and ID properties).
> For that reason, if the message template is completely reviewed and new randomized properties are added, you will need to either update the existing message service or create yours and update the IoC/DI settings.
> If you need dynamic content generation, this [version of the simulator based on IoT Plug and Play](https://github.com/jonmikeli/azureiotdevicesimulator5-pnp) may help you.


> NOTE
> 
> `moduleId` can be empty in case the message is sent by a device.

## Evolutivity

If you need to customize messages, their generation relies on services that implement the interfaces below:
 - `ITelemetryMessageService` for telemetry or measured data
 - `IErrorMessageService`
 - `ICommissioningMessageService`

If you need richer business logic or more evolved dynamic ways to generate messages, you can easily use your own implementation of these interfaces and update the IoC/DI.

The method that registers services is located in the `Program.cs` class:
```csharp
RegisterMessagingServices
```

Replace the lines below with your own implementation:
```csharp
services.AddTransient<ITelemetryMessageService, SimpleTelemetryMessageService>();
services.AddTransient<IErrorMessageService, SimpleErrorMessageService>();       
services.AddTransient<ICommissioningMessageService, SimpleCommissioningMessageService>();
```

# Glossary
A few word explanations to be sure they are understood in the context of this document.

## Commissioning

Commissioning represents the act of linking a provisioned device and a user (or user related information).

## Provisioning

Provisioning represents the action of creating an identity for the device in the Microsoft Azure IoT Hub.

## Azure IoT related vocabulary

## Twin
[Device twins](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-device-twins) store device state information including metadata, configurations, and conditions. Microsoft Azure IoT Hub maintains a device twin for each device that you connect to IoT Hub.

Similarly, [module twins](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-module-twins) play the same role thant device twins but at module level.

Twins contain 3 main sections:
 - Tags
 - Properties (Desired)
 - Properties (Reported)

For more details, follow the links provided.

## Tags
A section of the JSON document that the solution back end can read from and write to. Tags are not visible to device apps.

## Twin Desired properties
Used along with reported properties to synchronize device configuration or conditions. The solution back end can set desired properties, and the device app can read them. The device app can also receive notifications of changes in the desired properties.

## Twin Reported properties
Used along with desired properties to synchronize device configuration or conditions. The device app can set reported properties, and the solution back end can read and query them.

