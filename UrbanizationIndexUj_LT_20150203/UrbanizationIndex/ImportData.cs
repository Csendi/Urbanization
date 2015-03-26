using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UrbanizationIndex
{
    public class ImportData
    {
        private int ID;
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool Import { get; set; }
        public bool Teaching { get; set; }

        public ImportData(int ID, string Name, double Lat, double Long)
        {
            this.ID = ID;
            this.Name = Name;
            this.Latitude = Lat;
            this.Longitude = Long;
            Import = true;
            Teaching = false;
        }

        public int getID()
        {
            return ID;
        }
    }
}
