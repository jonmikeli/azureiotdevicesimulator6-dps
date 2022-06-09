using IoT.Simulator.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;

namespace IoT.Simulator.Services.Business
{
    public class GeolocalisationService: IGeoLocalizationService
    {
        Position _initialPosition;
        ILogger<GeolocalisationService> _iLogger;

        public GeolocalisationService(IOptions<Position> initialPosition, ILogger<GeolocalisationService> iLogger)
        {
            if (initialPosition == null)
                throw new ArgumentNullException(nameof(initialPosition));

            if (initialPosition.Value == null)
                throw new ArgumentNullException(nameof(initialPosition.Value));

            if (iLogger == null)
                throw new ArgumentNullException(nameof(iLogger));

            _initialPosition = initialPosition.Value;
            _iLogger = iLogger;
        }
        
        public Position RandomizePosition(Position initialPosition, PositionRandomType randomType, RandomPositionPrecision precision)
        {
            Position result = initialPosition;
            Random r = new Random(DateTime.UtcNow.Millisecond);

            switch (randomType)
            {
                case PositionRandomType.Forward:
                    //TODO
                    
                    break;
                case PositionRandomType.Circular:
                    //TODO
                    
                    break;
                case PositionRandomType.Rectangular:
                    //TODO

                    break;
                case PositionRandomType.Path:
                    //TODO

                    break;
                case PositionRandomType.FullRandom:                    
                    int opertationSign = r.Next(0, 100) > 50 ? 1 : -1;
                    decimal delta = (precision == RandomPositionPrecision.m) ? 0.0001m: 0.1m;
                    result.Latitude = result.Latitude + opertationSign * delta;

                    opertationSign = r.Next(0, 100) > 50 ? 1 : -1;
                    delta = (precision == RandomPositionPrecision.m) ? 0.0001m : 0.1m;
                    result.Longitude = result.Longitude + opertationSign * delta;
                    break;
                default:
                    break;
            }

            return result;
        }

        public Position RandomizePosition(PositionRandomType randomType, RandomPositionPrecision precision)
        {
            return RandomizePosition(_initialPosition, randomType, precision);
        }

        public Position UpdateInitialPosution(Position position)
        {
            if (position == null)
                throw new ArgumentNullException(nameof(position));

            _initialPosition = position;
            return _initialPosition;
        }
    }
}
