using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using TripodAccessWithDisplayAndLogSaveCloud.models;
using Newtonsoft.Json;
using System.Net.Http;
using System.ServiceProcess;

namespace TripodAccessWithDisplayAndLogSaveCloud
{
    class Program
    {
        //[STAThread]
        //private static readonly HttpClient client = new HttpClient();
        static void Main(string[] args)
        {

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ServiceLog()
            };
            ServiceBase.Run(ServicesToRun);

            //cloudUpload();
            //LoadSingleRealtimer();
            //pushT();
            //Console.ReadLine();





        }

       

        public static int LoadSingleRealtimer()
        {
            DeviceManager dm = new DeviceManager();
            string singleDeviceIp = ConfigurationManager.AppSettings["singleDeviceIp"];
            int singleDeviceNumber = Convert.ToInt32(ConfigurationManager.AppSettings["singleDeviceNumber"]);
            int connected = dm.isConected(singleDeviceIp, singleDeviceNumber);
            if (connected == 1)
            {
                dm.realEvent_OnAttTransaction(singleDeviceNumber, singleDeviceIp);
                dm.intervalRunner(singleDeviceNumber, singleDeviceIp);
                dm.intervalRunner(singleDeviceNumber, true);
            }

            return connected;
        }


        public static void cloudUpload()
        {
            
            CloudPush cpush = new CloudPush();
            int delay = cpush.intervalue();
            
            Task.Run(async() =>
            {
                while (true)
                {
                    Console.WriteLine("Delay Time("+delay+" minutes )");
                    await cpush.uploadCloudData();                    
                    await Task.Delay(delay);
                }

            });

        }

        public static bool DataBaseConnectionTest()
        {
            bool consuccess = true;
            string constr = ConfigurationManager.AppSettings["DatabaseConnection"];
            using (MySqlConnection con = new MySqlConnection(constr))
            {
                try
                {
                    con.Open();
                }
                catch (MySqlException ex)
                {
                    Program.writeErrorLog(ex.ToString());
                    consuccess = false;
                }
                finally
                {
                    con.Close();

                }
            }

            return consuccess;
        }

        /*
        public static void pushT()
        {
            Task.Run(async () => {
                await pushTest();
            });
        }

        public static async Task pushTest()
        {
            CDeviceLog logModel = new CDeviceLog();
            logModel.userid = "0002";
            logModel.checktime = "2019-07-15 18:06:11";
            logModel.terminalid = 1;
            logModel.name = "Mostofa";
            logModel.cloud_upload = 0;
            string postObject = JsonConvert.SerializeObject(logModel);
            Console.WriteLine("JSON FOR POSTING:" + postObject);
            string cloudUrl = ConfigurationManager.AppSettings["CloudUrl"];
            var response = await client.PostAsync(
                                cloudUrl,
                                 new StringContent(postObject, Encoding.UTF8, "application/json"));
            string content = await response.Content.ReadAsStringAsync();
            Console.WriteLine("RESPONSE FORM :" + content);
            CDeviceLog logModelDs = JsonConvert.DeserializeObject<CDeviceLog>(@content);
            Console.WriteLine("RESPONSE Object CAST :" + logModelDs.cloud_upload);
        }
        */
        public static void writeErrorLog(string message)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\log.txt", true);
                sw.WriteLine(DateTime.Now.ToString() + ":" + message);
                sw.Flush();
                sw.Close();
            }
            catch
            {

            }
        }


    }
}
