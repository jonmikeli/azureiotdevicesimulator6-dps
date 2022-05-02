#!/bin/bash
i=1

transportType="Mqtt"
dpsIDScope="0ne005B88F3"
symmetricKey="LsTpe1VeMJGqTAgZVN4gxUrC8VE7kb5lCR/zgMETYLWnBfwIT7kC7ltEWVjNv/jRU9VTe6lSsISOcg00Lon1oA=="



while [ $i -lt 20 ]
do
  deviceName='autodevice'$i

  echo "Creating the device: $deviceName"
  docker run -ti --rm --name $deviceName -d -e PROVISIONING_REGISTRATION_ID=$deviceName -e DPS_SECURITY_TYPE="SymmetricKey" -e TRANSPORT_TYPE=$transportType -e DPS_IDSCOPE=$dpsIDScope -e PRIMARY_SYMMETRIC_KEY=$symmetricKey --network="host" "tifmscrdev.azurecr.io/iotsimulator-jmi-dps6:latest"
  #docker run -ti --name $deviceName -e PROVISIONING_REGISTRATION_ID=$deviceName -e DPS_SECURITY_TYPE="SymmetricKey" -e TRANSPORT_TYPE=$transportType -e DPS_IDSCOPE=$dpsIDScope -e PRIMARY_SYMMETRIC_KEY=$symmetricKey --network="host" iotsimulator-dps
  echo "Device created: $deviceName"
  
  let "i+=1" 
done