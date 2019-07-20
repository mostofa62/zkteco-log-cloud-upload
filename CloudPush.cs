using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TripodAccessWithDisplayAndLogSaveCloud.models;

namespace TripodAccessWithDisplayAndLogSaveCloud
{
    class CloudPush
    {
        private static readonly HttpClient client = new HttpClient();
        //Database and other Part
        string constr = null;
        int cloudPool = 1;
        string cloudUrl = null;       
        //end Database Part


        public CloudPush()
        {
            constr = ConfigurationManager.AppSettings["DatabaseConnection"];
            cloudUrl = ConfigurationManager.AppSettings["CloudUrl"];
            cloudPool = Convert.ToInt32(ConfigurationManager.AppSettings["CloudPool"]);


        }
        public int intervalue()
        {
            int min = Convert.ToInt32(ConfigurationManager.AppSettings["CloudPushInterval"]), factor = 60000;
            int interval = min * factor;// 4 * 1 minutes

            return interval;
        }
        /*
        public void intervalRunner()
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            int min = Convert.ToInt32(ConfigurationManager.AppSettings["CloudPushInterval"]), factor = 60000;
            int interval = min * factor;// 4 * 1 minutes

            timer.Elapsed += new System.Timers.ElapsedEventHandler((object sender, System.Timers.ElapsedEventArgs e) =>
            {

            });
            timer.Interval = interval;
            timer.Enabled = true;
        }
        */

        public async Task uploadCloudData()
        {

            Console.WriteLine("!Uploading Section Started!");
            //init part
            List<CDeviceLog> logModelList = new List<CDeviceLog>();
            //CDeviceLog logModel = null;
            //end init part
            // Database select Part
            string query = @"select id, userid, checktime, terminalid, name from device_log where cloud_upload=0 order by id asc limit " + cloudPool;
            using (MySqlConnection con = new MySqlConnection(constr))
            using (MySqlCommand cmd = new MySqlCommand(query, con))
            {
                try
                {

                    if (con.State == ConnectionState.Closed)
                    {
                        await con.OpenAsync();
                        cmd.CommandType = CommandType.Text;
                        using (MySqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.HasRows)
                            {
                                while (dr.Read())
                                {
                                    //Console.WriteLine("Userid"+dr.GetValue(1));
                                    CDeviceLog logModel = new CDeviceLog();
                                    logModel.id = Convert.ToInt64(dr.GetValue(0).ToString());
                                    logModel.userid = dr.GetValue(1).ToString();
                                    logModel.checktime = dr.GetValue(2).ToString();
                                    logModel.terminalid = Convert.ToInt32(dr.GetValue(3).ToString());
                                    logModel.name = dr.GetValue(4).ToString();
                                    logModelList.Add(logModel);
                                }
                            }
                        }

                    }

                }
                catch (MySqlException ex)
                {
                    Console.WriteLine("Database Error:" + ex.ToString());
                }
                finally
                {
                    con.Close();
                }
            }
            //Program.writeErrorLog("Query Data:" + logModel.userid);
            //end database select part

            //json string 
            string postObject = null;
            if (logModelList.Count > 0)
            {
                postObject = JsonConvert.SerializeObject(logModelList);
                Program.writeErrorLog("JSON FOR POSTING:" + postObject);
            }
            //end json string


            //web push part
            //logModel = null;
            logModelList.Clear();
            if (postObject != null) {
                //Program.writeErrorLog("JSON INSIDE POST:" + postObject);               
                try { 
                    var response = await client.PostAsync(
                                        cloudUrl,
                                            new StringContent(postObject, Encoding.UTF8, "application/json"));

                    //Program.writeErrorLog("RESPONSE :" + response);
                    string content = await response.Content.ReadAsStringAsync();
                    Program.writeErrorLog("RESPONSE FORM :" + content);
                    logModelList = JsonConvert.DeserializeObject<List<CDeviceLog>>(@content);                    
                }
                catch (HttpRequestException ex)
                {
                    Program.writeErrorLog(cloudUrl+":" + ex.ToString());
                }


            }
            //end web push part

            //database update part
            if (logModelList.Count > 0)
            {
                Console.WriteLine("--Updating Data--");
                query = @"update device_log set cloud_upload=?cloud_upload where id=?id";
                using (MySqlConnection con = new MySqlConnection(constr))
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    try
                    {
                        if (con.State == ConnectionState.Closed)
                        {
                            await con.OpenAsync();
                            foreach (CDeviceLog logModel in logModelList)
                            {


                                Console.WriteLine("DBSTATE:" + con.State + "| Cloud Uplaod " + logModel.cloud_upload + "| Id" + logModel.id);

                                try
                                {
                                    cmd.CommandType = CommandType.Text;
                                    cmd.Parameters.AddWithValue("?cloud_upload", logModel.cloud_upload);
                                    cmd.Parameters.AddWithValue("?id", logModel.id);
                                    cmd.ExecuteNonQuery();
                                }
                                catch (MySqlException ex)
                                {
                                    Console.WriteLine("Database Insert Error:" + ex.ToString());
                                }
                            }

                        }
                    }
                    catch (MySqlException ex)
                    {
                        Console.WriteLine("Database Connection Error:" + ex.ToString());
                    }
                    finally
                    {
                        con.Close();
                    }

                }

            }
            //end database update part
            //set logmodel reset
            logModelList.Clear();
            //end set logmodel reset

            Console.WriteLine("!Uploading Section Ended!");
        }
           
    }
}
