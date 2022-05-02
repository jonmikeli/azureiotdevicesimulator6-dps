#!/bin/bash
i=0

while [ $i -lt 20 ]
do
  deviceName='autodevice'$i

  echo "Stopping the device: $deviceName"
  docker stop $deviceName
  echo "Device Stopped: $deviceName"
  
  let "i+=1" 
done