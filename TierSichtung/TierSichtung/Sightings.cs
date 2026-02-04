using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TierSichtung
{
    public class Sightings
    {
        public int sightingsid { get; set; }
        public DateTime? date { get; set; }       // ACHTUNG: Datumstyp kann string sein – ggf. anpassen!
        public int animalid { get; set; }
        public string ort { get; set; }
        public int positive { get; set; }
        public int negative { get; set; }
        public int status { get; set; }
        public int count { get; set; }

        public double lat { get; set; }
        public double lng { get; set; }

        // Aus dem JOIN (empfohlen, siehe Backend):

        public DateTime? lastseen { get; set; }
        public string trivialname { get; set; }
        public string sciencename { get; set; }
        public string family { get; set; }
    }
}
