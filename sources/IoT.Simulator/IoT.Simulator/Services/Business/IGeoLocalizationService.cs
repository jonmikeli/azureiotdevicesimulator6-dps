using IoT.Simulator.Models;

namespace IoT.Simulator.Services.Business
{
    public interface IGeoLocalizationService
    {
        public Position RandomizePosition(Position initialPosition, PositionRandomType randomType, RandomPositionPrecision precision);
    }
}
