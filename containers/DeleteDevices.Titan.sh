#!/bin/bash
i=0

while [ $i -lt 20 ]
do
  deviceName='autodevice'$i

  echo "Deleting the device: $deviceName"
  docker rm -f $deviceName
  echo "Device deleted: $deviceName"
  
  let "i+=1" 
done