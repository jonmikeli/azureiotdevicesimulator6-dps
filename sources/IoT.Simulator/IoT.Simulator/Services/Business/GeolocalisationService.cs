using IoT.Simulator.Models;

namespace IoT.Simulator.Services.Business
{
    public class GeolocalisationService: IGeoLocalizationService
    {
        public Position RandomizePosition(Position initialPosition, PositionRandomType randomType, RandomPositionPrecision precision)
        {
            Position result = initialPosition;

            switch (randomType)
            {
                case PositionRandomType.Forward:
                    break;
                case PositionRandomType.FullRandom:
                    break;
                default:
                    break;
            }

            return result;
        }
    }
}
