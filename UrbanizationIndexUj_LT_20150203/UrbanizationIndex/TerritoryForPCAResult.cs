using System;

namespace UrbanizationIndex
{
    public class TerritoryForPCAResult : IComparable
    {
        public Territory territory { get; private set; }

        public TerritoryForPCAResult(Territory t)
        {
            territory = t;
        }

        public override string ToString()
        {
            return territory.Name + ": " + territory.PCAResult.ToString() + " (Lat: " + territory.Latitude.ToString() + ", Lng: " + territory.Longitude.ToString() + ")";
        }

        public int CompareTo(object obj)
        {
            var o = (TerritoryForPCAResult)obj;
            return territory.PCAResult.CompareTo(o.territory.PCAResult);
        }
    }
}
