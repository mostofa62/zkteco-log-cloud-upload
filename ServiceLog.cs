using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TripodAccessWithDisplayAndLogSaveCloud
{
    partial class ServiceLog : ServiceBase
    {
        public ServiceLog()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
            bool dbconnected = Program.DataBaseConnectionTest();
            if (!dbconnected)
            {
                Program.writeErrorLog("Database Connection Error..Service Not Starting");
                this.Stop();
            }
            else
            {
                int connected = Program.LoadSingleRealtimer();
                if (connected != 1)
                {
                    this.Stop();
                }
                else
                {
                    Program.cloudUpload();
                }
            }
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
        }

        
    }
}
