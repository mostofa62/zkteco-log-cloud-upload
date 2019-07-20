using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TripodAccessWithDisplayAndLogSaveCloud
{
    class DeviceManager
    {

        public zkemkeeper.CZKEMClass axCZKEM1 = new zkemkeeper.CZKEMClass();
        private int idwErrorCode;

        string sdwEnrollNumber = "";
        int idwVerifyMode = 0;
        int idwInOutMode = 0;
        int idwYear = 0;
        int idwMonth = 0;
        int idwDay = 0;
        int idwHour = 0;
        int idwMinute = 0;
        int idwSecond = 0;
        int idwWorkcode = 0;

        //Database Part
        string constr = null;
        long areaCode = 0;
        //end Database Part


        public DeviceManager()
        {
            //constr = "Data Source=(DESCRIPTION=(ADDRESS =(PROTOCOL=tcp)(HOST=" + host + ")(PORT=" + port + "))(CONNECT_DATA=(SERVICE_NAME=" + service + ")));User Id=" + userId + ";Password=" + userPassword;
            areaCode = Convert.ToInt64(ConfigurationManager.AppSettings["AreaId"]);            
            constr = ConfigurationManager.AppSettings["DatabaseConnection"];
        }


        public void intervalRunner(int iMachineNumber, string IPAddr)
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            int min = Convert.ToInt32(ConfigurationManager.AppSettings["ReRegisterEventInterval"]), factor = 60000;
            int interval = min * factor;// 4 * 1 minutes
            timer.Elapsed += new System.Timers.ElapsedEventHandler((object sender, System.Timers.ElapsedEventArgs e) =>
            {

                if (axCZKEM1.GetDeviceIP(iMachineNumber, IPAddr))
                {
                    Console.WriteLine("---Device(" + iMachineNumber + ")[" + IPAddr + "]-Connected!Checking after Interval:=" + min + "(minutes)---");
                    //this.realEvent_OnAttTransaction(iMachineNumber[i]);
                }

            });
            timer.Interval = interval;
            timer.Enabled = true;

        }

        public void intervalRunner(int iMachineNumber, bool task)
        {
            string DailyTime = ConfigurationManager.AppSettings["LogDailyDownloadTime"];
            string[] timeParts = DailyTime.Split(new char[1] { ':' });
            DateTime dateNow = DateTime.Now;
            DateTime date = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day,
                       int.Parse(timeParts[0]), int.Parse(timeParts[1]), int.Parse(timeParts[2]));
            TimeSpan ts;
            if (date > dateNow)
                ts = date - dateNow;
            else
            {
                date = date.AddDays(1);
                ts = date - dateNow;
            }
            //Console.WriteLine("Device(" + iMachineNumber + ") Log Downloading At Daily ("+ DailyTime + ")");
            //waits certan time and run the code
            Task.Delay(ts).ContinueWith((x) => getLogData(iMachineNumber));
        }


        public int isConected(String div_ip, int machineNo)
        {


            bool isConnected = axCZKEM1.Connect_Net(div_ip, 4370);
            Console.WriteLine("Device(" + machineNo + ")[" + div_ip + "]=" + isConnected);
            if (isConnected == true)
            {
                int machineNumber = machineNo;
                bool gg = axCZKEM1.EnableDevice(machineNumber, true);
                return 1;
            }
            else
            {
                axCZKEM1.GetLastError(ref idwErrorCode);
                Program.writeErrorLog("Device Conection Fialed" + idwErrorCode);

                return idwErrorCode;
            }

        }

        public void isDisconnected()
        {
            axCZKEM1.Disconnect();
        }

        /** Event Maincast **/
        public void realEvent_OnAttTransaction(int iMachineNumber, string deviceIp)
        {

            if (axCZKEM1.RegEvent(iMachineNumber, 65535))
            {
                Console.WriteLine("Registering Realtime Event For Machine:" + iMachineNumber);
                axCZKEM1.OnAttTransactionEx += new zkemkeeper._IZKEMEvents_OnAttTransactionExEventHandler((string sEnrollNumber, int iIsInValid, int iAttState, int iVerifyMethod, int iYear, int iMonth, int iDay, int iHour, int iMinute, int iSecond, int iWorkCode) =>
                {
                    axCZKEM1_OnAttTransaction_SaveOnly(sEnrollNumber, iIsInValid, iAttState, iVerifyMethod, iYear, iMonth, iDay, iHour, iMinute, iSecond, iWorkCode, iMachineNumber, deviceIp);
                });
            }

        }

        //the actual event

        //save only
        public void axCZKEM1_OnAttTransaction_SaveOnly(string sEnrollNumber, int iIsInValid, int iAttState, int iVerifyMethod, int iYear, int iMonth, int iDay, int iHour, int iMinute, int iSecond, int iWorkCode, int MachineNo, string deviceIp)
        {

            Console.WriteLine("--------------- Per Log Device(" + MachineNo + ")[" + deviceIp + "] --------------");
            string time = iYear.ToString() + "-" + iMonth.ToString() + "-" + iDay.ToString() + " " + iHour.ToString() + ":" + iMinute.ToString() + ":" + iSecond.ToString();
            Console.WriteLine("Verified(" + MachineNo + ") [ UserID=" + sEnrollNumber + " isInvalid=" + iIsInValid.ToString() + " state=" + iAttState.ToString() + " verifystyle=" + iVerifyMethod.ToString() + " time=" + time + "]");
            //global data for setting

            string name = "";
            string password = "";
            int privilage = 0;
            bool bEnabled = false;

            //end global data for setting

            //Read Data From Machine



            axCZKEM1.EnableDevice(MachineNo, false);
            axCZKEM1.SSR_GetUserInfo(MachineNo, sEnrollNumber, out name, out password, out privilage, out bEnabled);
            Console.WriteLine("Name: " + name);

            axCZKEM1.EnableDevice(MachineNo, true);


            //end Read Data From Machine

            //Database Part           
          
            using (MySqlConnection con = new MySqlConnection(constr))
            {
                try
                {
                    con.Open();

                    using (MySqlCommand cmd = con.CreateCommand())
                    {

                       

                        //insert
                        try
                        {
                            cmd.CommandText = @"insert into device_log(userid, checktime, terminalid, name, area_id) values(?userid, ?checktime, ?terminalid, ?name, ?area_id)";
                            cmd.Parameters.AddWithValue("?userid", sEnrollNumber);
                            cmd.Parameters.AddWithValue("?checktime", time);
                            cmd.Parameters.AddWithValue("?terminalid", MachineNo);
                            cmd.Parameters.AddWithValue("?name", name);
                            cmd.Parameters.AddWithValue("?area_id", areaCode);
                            cmd.ExecuteNonQuery();
                        }
                        catch (MySqlException ex)
                        {
                            Program.writeErrorLog("Database Insert Exception: " + ex.ToString());
                        }
                        //end insert


                    }



                }
                catch (MySqlException ex)
                {
                    Program.writeErrorLog("Database Exception: " + ex.ToString());
                }
                finally
                {
                    con.Close();
                }



            }
            

            //end Database Part

            
            //save data to database

            //end save data to database

            Console.WriteLine("-------End Per Log(" + MachineNo + ")[" + deviceIp + "] --------");
        }
        //end save only
        //end actual event

        /** End Event MainCast **/

        /** late night log **/

        private void getLogData(int iMachineNumber)
        {

            //global data for setting

            string name = "";
            string password = "";
            int privilage = 0;
            bool bEnabled = false;
            int fromDateInterval = Convert.ToInt32(ConfigurationManager.AppSettings["DateFetchInterval"]);
            DateTime oneMax = DateTime.Now.AddDays(1);
            string toDate = oneMax.ToString("yyyy-MM-dd HH:mm:ss");
            DateTime nMin = DateTime.Now.AddDays(-fromDateInterval);
            string fromDate = nMin.ToString("yyyy-MM-dd")+" 00:05:00";
            //end global data for setting

            axCZKEM1.EnableDevice(iMachineNumber, false);            
            ///if (axCZKEM1.ReadGeneralLogData(iMachineNumber))//read all the attendance records to the memory
            if(axCZKEM1.ReadTimeGLogData(iMachineNumber,fromDate,toDate))
            {

                using (MySqlConnection con = new MySqlConnection(constr))
                {
                    try
                    {
                        con.Open();

                        
                        while (axCZKEM1.SSR_GetGeneralLogData(iMachineNumber, out sdwEnrollNumber, out idwVerifyMode,
                           out idwInOutMode, out idwYear, out idwMonth, out idwDay, out idwHour, out idwMinute, out idwSecond, ref idwWorkcode))//get records from the memory
                        {

                            axCZKEM1.SSR_GetUserInfo(iMachineNumber, sdwEnrollNumber, out name, out password, out privilage, out bEnabled);
                            string time = idwYear + "-" + idwMonth + "-" + idwDay + " " + idwHour + ":" + idwMinute + ":" + idwSecond;
                            Console.WriteLine("User Id:" + sdwEnrollNumber + "(" + name + "), Time: (" + time + ")");

                            using (MySqlCommand cmd = con.CreateCommand())
                            {



                                //insert
                                try
                                {
                                    cmd.CommandText = @"insert into device_log(userid, checktime, terminalid, name, area_id) values(?userid, ?checktime, ?terminalid, ?name, ?area_id)";
                                    cmd.Parameters.AddWithValue("?userid", sdwEnrollNumber);
                                    cmd.Parameters.AddWithValue("?checktime", time);
                                    cmd.Parameters.AddWithValue("?terminalid", iMachineNumber);
                                    cmd.Parameters.AddWithValue("?name", name);
                                    cmd.Parameters.AddWithValue("?area_id", areaCode);
                                    cmd.ExecuteNonQuery();
                                }
                                catch (MySqlException ex)
                                {
                                    //Program.writeErrorLog("Database Insert Exception: " + ex.ToString());
                                    continue;
                                }
                                //end insert


                            }

                        }

                    }
                    catch (MySqlException ex)
                    {
                        Program.writeErrorLog("Database Exception: " + ex.ToString());
                    }
                    finally
                    {

                        con.Close();
                    }

                }

            }
            else
            {

                axCZKEM1.GetLastError(ref idwErrorCode);

                if (idwErrorCode != 0)
                {
                    //Console.WriteLine("Reading data from terminal failed,ErrorCode: " + idwErrorCode.ToString(), "Error");
                    Program.writeErrorLog("Reading data from terminal failed,ErrorCode: " + idwErrorCode.ToString());
                }
                else
                {
                    Program.writeErrorLog("No data from terminal returns!");
                }
            }
            axCZKEM1.EnableDevice(iMachineNumber, true);

        }
    }
}
