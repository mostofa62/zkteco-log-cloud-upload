using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripodAccessWithDisplayAndLogSaveCloud.models
{
    class CDeviceLog
    {
        public long id { get; set; }
        public string userid { get; set; }
        public int terminalid { get; set; }
        public string name { get; set; }
        public string checktime { get; set; }
        public int cloud_upload { get; set; }

    }
}
