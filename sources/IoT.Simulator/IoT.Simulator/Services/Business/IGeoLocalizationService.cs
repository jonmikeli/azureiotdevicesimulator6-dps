using IoT.Simulator.Models;

namespace IoT.Simulator.Services.Business
{
    public interface IGeoLocalizationService
    {
        public Position RandomizePosition(Position initialPosition, PositionRandomType randomType, RandomPositionPrecision precision);

        public Position RandomizePosition(PositionRandomType randomType, RandomPositionPrecision precision);

        public Position UpdateInitialPosution(Position position);
    }
}
