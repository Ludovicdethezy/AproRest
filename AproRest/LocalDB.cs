using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using System.Data;
using AproRest;
using System.Net.Http;

namespace AproRest.webhook
{
    public class LocalDB
    {
        //private void InsertIntoSql2(string CmdText, List<SqlParameter> PrmtrList, string MsgDesc)
        //{
        //    using (SqlConnection con = new SqlConnection(Connect_string))
        //    {
        //        SqlCommand SqlCmd_Insert = new SqlCommand();
        //        try
        //        {


        //            con.Open();

        //            SqlCmd_Insert.Connection = con;
        //            SqlCmd_Insert.CommandTimeout = 5;
        //            SqlCmd_Insert.CommandType = CommandType.Text;

        //            SqlCmd_Insert.CommandText = CmdText;

        //            List<SqlParameter> parameters = new List<SqlParameter>(PrmtrList);

        //            foreach (SqlParameter Prmtr in parameters)
        //            {
        //                SqlCmd_Insert.Parameters.Add(Prmtr);
        //            }

        //            SqlCmd_Insert.ExecuteScalar();
        //            con.Close();
        //            //  Console.WriteLine("Nouvelle donnée insérée : " + MsgDesc);
        //            //WriteLog("Nouvelle donnée insérée : " + Msg_1.ToString());
        //        }
        //        catch (InvalidCastException ex)
        //        {
        //            Console.WriteLine("ERREUR COMMANDE ...");
        //            Console.WriteLine(ex.Message.ToString());
        //            // WriteLog("ERREUR COMMANDE ... " + ex.Message);

        //            throw ex;
        //            // permet de passer l'exeption à la fonction appelante,  comme le try catch dans
        //            // lequel cette fonction est appelée et déclenchera le catch
        //        }
        //        catch (SqlException ex)
        //        {
        //            Console.WriteLine("ERREUR DE CONNEXION ...");
        //            Console.WriteLine(ex.Message.ToString());
        //            // WriteLog("ERREUR DE CONNEXION ... " + ex.Message);

        //            throw ex;
        //        }
        //        catch (InvalidOperationException ex)
        //        {
        //            Console.WriteLine("ERREUR ECRITURE ...");
        //            Console.WriteLine(ex.Message.ToString());
        //            // WriteLog("ERREUR ECRITURE ... " + ex.Message);

        //            throw ex;
        //        }
        //        // autres exceptions seront levées dans le try catch appelant cette fonction

        //        SqlCmd_Insert.Dispose();
        //        if (con.State == ConnectionState.Open)
        //            con.Close();
        //        SqlCmd_Insert = null;
        //    }
        //}
        public delegate void InvokeDelegate();
        public static void webhook_receive(dynamic ev)
        {
            string type = ev.type;
            string Ev_id = ev.event_id;
            string ack = ev.waiting_acknowledgement;
            string To_id = "";
            string start_time = "";
            string error ="";
            string end_reason ="";
            string end_status = "";
            string created ="";

            HttpClient client2 = new HttpClient();
            var sys = new Client(client2);
            //  sys.BaseUrl = "http://" + MainWindow.IP_host + "/api/v1/";
            if (MainWindow.usew == true)
                sys.BaseUrl = "http://" + MainWindow.WIP_host + "/api/v1/";
            else
                sys.BaseUrl = "http://" + MainWindow.IP_host + "/api/v1/";

            if (ev.payload != null)
            { 
            To_id = ev.payload.transport_order_id;
            start_time = ev.payload.drive_start_time;
            error = ev.payload.error_name;
            end_reason = ev.payload.end_reason;
            end_status = ev.payload.end_status;
            created = ev.created_at;
           
            }
            switch (type)
            {
                case var s when type.Contains("AgvOperationEndEvent"):
                    {
                        Console.WriteLine("wait ack postload");
                        string status;
                        DateTimeOffset crea = ev.created_at;
                        DateTimeOffset startd = ev.payload.drive_start_time;
                        string step = ev.payload.step_index;
                        if (MainWindow.autoackw == false)
                        {
                            if (step == "0")
                                
                            if (end_status == "Success")
                            {
                                    status = "AGV waiting fetched ack";
                                    //MainWindow.updatedb(To_id, DateTimeOffset.Parse(start_time), status, int.Parse(Ev_id));
                                    MainWindow.updatedb(To_id, startd, status, int.Parse(Ev_id));
                                }
                            else
                            {
                                status = "AGV waiting ack "+end_status.ToString();
                                    //MainWindow.updatedb_error(To_id, DateTimeOffset.Parse(start_time), status, error, int.Parse(Ev_id));
                                    MainWindow.updatedb_error(To_id, startd, status, error, int.Parse(Ev_id));
                                }

                            else
                                

                            if (end_status == "Success")
                            {
                                status = "AGV waiting delivered ack";
                                //MainWindow.updateendoforder(To_id, DateTimeOffset.Parse(created), status, int.Parse(Ev_id));
                                MainWindow.updateendoforder(To_id, crea, status, int.Parse(Ev_id));
                            }


                            else
                            {
                                status = "AGV waiting ack " + end_status.ToString();
                                //MainWindow.updateendoforder_error(To_id, DateTimeOffset.Parse(start_time), status, error, int.Parse(Ev_id));
                                MainWindow.updateendoforder_error(To_id, startd, status, error, int.Parse(Ev_id));
                            }

                        }

                        else
                        {
                            sys.ContinueAsync(To_id, int.Parse(Ev_id));

                            if (step == "0")
                            {

                                if (end_status == "Success")
                                {
                                    status = "Pallet fetched";
                                    //MainWindow.updatedb(To_id, DateTimeOffset.Parse(start_time), status, int.Parse(Ev_id));
                                    MainWindow.updatedb(To_id, startd, status, int.Parse(Ev_id));
                                }
                                else
                                {
                                    status = end_status.ToString();
                                    // MainWindow.updatedb_error(To_id, DateTimeOffset.Parse(start_time), status, error, int.Parse(Ev_id));
                                    MainWindow.updatedb_error(To_id, startd, status, error, int.Parse(Ev_id));
                                }


                            }
                            else
                            {
                                if (end_status == "Success")
                                {
                                    status = "Pallet delivered";
                                    // MainWindow.updateendoforder(To_id, DateTimeOffset.Parse(created), status, int.Parse(Ev_id));
                                    MainWindow.updateendoforder(To_id, crea, status, int.Parse(Ev_id));
                                }


                                else
                                {
                                    status = end_status.ToString();
                                    // MainWindow.updateendoforder_error(To_id, DateTimeOffset.Parse(start_time), status, error, int.Parse(Ev_id));
                                    MainWindow.updateendoforder_error(To_id, startd, status, error, int.Parse(Ev_id));
                                }
                            }
                        }
                            //MainWindow.updatedb(To_id, DateTimeOffset.Parse(start_time), "");
                        break;
                    }
                case var s when type.Contains("AgvArrivedToAddressEvent"):
                    {
                        Console.WriteLine("wait ack preload");
                        string status;
                        string step = ev.payload.step_index;
                            DateTimeOffset startd = ev.payload.drive_start_time;
                        if (MainWindow.autoackw == false)
                        {
                            if (step == "0")
                                status = "AGV waiting ack to fetch";
                            else
                                status = "AGV waiting ack to deliver";

                           // MainWindow.updatedb(To_id, DateTimeOffset.Parse(start_time.ToString()), status, int.Parse(Ev_id));
                            MainWindow.updatedb(To_id, startd, status, int.Parse(Ev_id));

                        }
                        else
                        {
                            sys.ContinueAsync(To_id, int.Parse(Ev_id));
                       
                            if (step == "0")
                                status = "AGV ready to fetch";
                            else
                                status = "AGV ready to deliver";
                            // MainWindow.updatedb(To_id, DateTimeOffset.Parse(start_time.ToString()), status, int.Parse(Ev_id));
                            MainWindow.updatedb(To_id, startd, status, int.Parse(Ev_id));
                        }
                        break;
                    }
             
                case var s when type.Contains("SystemStartupEvent"):
                    {
                        break;
                    }
                case var s when type.Contains("TransportOrderEndEvent"):
                    {
                        Console.WriteLine("end event");
                        string stcustom = ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JContainer)ev.payload.custom_data).First).Value).Value.ToString();
                        if (stcustom == "io_request")
                        {
                            foreach (var item in (Newtonsoft.Json.Linq.JContainer)ev.payload.custom_data)
                            {
                                if (((Newtonsoft.Json.Linq.JProperty)item).Value.ToString() == "io_request")
                                {
                                    string connectionString = MainWindow.Connect_string;
                                    using (SqlConnection connection2 = new SqlConnection(connectionString))
                                    {
                                        connection2.Open();
                                        SqlCommand cmd2 = new SqlCommand(" TRUNCATE Table IO ", connection2);
                                        var exist = cmd2.ExecuteNonQuery();
                                        connection2.Close();


                                    }
                                }

                                else
                                {

                                    Console.WriteLine(item.ToString());
                                    string CmdText;
                                    SqlParameter SqlPrmtr;
                                    List<SqlParameter> PrmtrList;



                                    CmdText = " INSERT INTO IO" +

                                        "([IO_Name],[IO_Value],[IO_Type]) " +

                                        "VALUES (@IO_Name,@IO_Value,@IO_Type)";

                                    PrmtrList = new List<SqlParameter>();

                                    SqlPrmtr = new SqlParameter("@IO_Name", SqlDbType.VarChar);
                                    SqlPrmtr.Value = ((Newtonsoft.Json.Linq.JProperty)item).Name;
                                    PrmtrList.Add(SqlPrmtr);

                                    SqlPrmtr = new SqlParameter("@IO_Value", SqlDbType.VarChar);
                                    SqlPrmtr.Value = ((Newtonsoft.Json.Linq.JProperty)item).Value;
                                    PrmtrList.Add(SqlPrmtr);

                                    SqlPrmtr = new SqlParameter("@IO_Type", SqlDbType.VarChar);
                                    SqlPrmtr.Value = "input";
                                    PrmtrList.Add(SqlPrmtr);
                                    MainWindow.InsertIntoSql(CmdText, PrmtrList, "new IO added");

                                }


                            }
                        }
                        else
                        {
                            DateTimeOffset crea = ev.created_at;
                            MainWindow.updateendoforder(To_id, crea, end_reason, int.Parse(Ev_id));
                        }
                       // MainWindow.refresh_ioDG();
                        break;
                    }
                case var s when type.Contains("CustomInformationEvent"):
                    {
                       
                        string stcustom = ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JContainer)ev.payload.custom_data).First).Value).Value.ToString();
                        if (stcustom == "input_update")
                        {
                            string connectionString = MainWindow.Connect_string;
                            try
                            {
                                using (SqlConnection connection2 = new SqlConnection(connectionString))
                                {
                                    connection2.Open();

                                    //status_db.Content = "Data updated to db";
                                    SqlCommand cmd = new SqlCommand("UPDATE IO SET  IO_Value = @IO_Value ,IO_Type = @IO_Type, IO_State = @IO_State Where  IO_Name = @IO_Name ", connection2);

                                    cmd.Parameters.AddWithValue("@IO_Type", "input_wu");
                                    cmd.Parameters.AddWithValue("@IO_Value", ev.payload.custom_data.Input_val.ToString());
                                    cmd.Parameters.AddWithValue("@IO_Name", ev.payload.custom_data.Input_nam.ToString());
                                    cmd.Parameters.AddWithValue("@IO_State", ev.payload.custom_data.Input_sta.ToString());
                                    Console.WriteLine(ev.payload.custom_data.Input_val.ToString() + "  " + ev.payload.custom_data.Input_nam);

                                    cmd.ExecuteNonQuery();



                                    connection2.Close();
                                }
                            }
                            catch (SqlException ex)
                            {
                                Console.WriteLine("ERREUR DE CONNEXION ...");
                                Console.WriteLine(ex.Message.ToString());
                                // WriteLog("ERREUR DE CONNEXION ... " + ex.Message);

                                throw ex;
                            }


                        }
                        break;
                    }

                    case var s when type.Contains("AgvState"):
                    {
                        string connectionString = MainWindow.Connect_string;
                        string CmdText;





                        SqlParameter SqlPrmtr;
                        List<SqlParameter> PrmtrList;
                        try
                        {
                            using (SqlConnection connection2 = new SqlConnection(connectionString))
                            {
                                connection2.Open();
                                SqlCommand cmd2 = new SqlCommand(" DELETE FROM agvs WHERE agv_id = " + ev.data.id + " ", connection2);
                                var exist = cmd2.ExecuteNonQuery();
                                connection2.Close();


                            }
                        }
                        catch (InvalidCastException ex)
                        {
                            Console.WriteLine("ERREUR COMMANDE ...");
                            Console.WriteLine(ex.Message.ToString());
                            // WriteLog("ERREUR COMMANDE ... " + ex.Message);

                            throw ex;
                            // permet de passer l'exeption à la fonction appelante,  comme le try catch dans
                            // lequel cette fonction est appelée et déclenchera le catch
                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine("ERREUR DE CONNEXION ...");
                            Console.WriteLine(ex.Message.ToString());
                            // WriteLog("ERREUR DE CONNEXION ... " + ex.Message);

                            throw ex;
                        }



                        CmdText = "INSERT INTO agvs" +

                            "([agv_id],[type_name],[state],[loaded1],[ems_auto],[ems_man],[user_stop],[blocked_by_agv],[blocked_by_IO],[error],[battery_state],[low_battery],[current_order]) " +

                            "VALUES (@agv_id,@type_name,@state ,@loaded1,@ems_auto,@ems_man,@user_stop,@blocked_by_agv,@blocked_by_IO,@error,@battery_state,@low_battery,@current_order)";

                        PrmtrList = new List<SqlParameter>();

                        SqlPrmtr = new SqlParameter("@agv_id", SqlDbType.Int);
                        SqlPrmtr.Value = ev.data.id;
                        PrmtrList.Add(SqlPrmtr);

                        SqlPrmtr = new SqlParameter("@type_name", SqlDbType.VarChar);
                        SqlPrmtr.Value = ev.data.type_name;
                        PrmtrList.Add(SqlPrmtr);

                        SqlPrmtr = new SqlParameter("@state", SqlDbType.VarChar);
                        SqlPrmtr.Value = ev.data.state;
                        PrmtrList.Add(SqlPrmtr);

                        SqlPrmtr = new SqlParameter("@loaded1", SqlDbType.Bit);
                        SqlPrmtr.Value = ev.data.loaded1;
                        PrmtrList.Add(SqlPrmtr);

                        SqlPrmtr = new SqlParameter("@ems_auto", SqlDbType.Bit);
                        SqlPrmtr.Value = ev.data.emergency_stop_auto_reset;
                        PrmtrList.Add(SqlPrmtr);

                        SqlPrmtr = new SqlParameter("@ems_man", SqlDbType.Bit);
                        SqlPrmtr.Value = ev.data.emergency_stop_manual_reset;
                        PrmtrList.Add(SqlPrmtr);

                        SqlPrmtr = new SqlParameter("@user_stop", SqlDbType.Bit);
                        SqlPrmtr.Value = ev.data.user_stop;
                        PrmtrList.Add(SqlPrmtr);

                        SqlPrmtr = new SqlParameter("@blocked_by_agv", SqlDbType.Bit);
                        SqlPrmtr.Value = ev.data.blocked_by_agv;
                        PrmtrList.Add(SqlPrmtr);

                        SqlPrmtr = new SqlParameter("@blocked_by_IO", SqlDbType.Bit);
                        SqlPrmtr.Value = ev.data.blocked_by_io;
                        PrmtrList.Add(SqlPrmtr);

                        SqlPrmtr = new SqlParameter("@error", SqlDbType.Bit);
                        SqlPrmtr.Value = ev.data.error;
                        PrmtrList.Add(SqlPrmtr);

                        SqlPrmtr = new SqlParameter("@battery_state", SqlDbType.Int);
                        SqlPrmtr.Value = ev.data.battery_state;
                        PrmtrList.Add(SqlPrmtr);

                        SqlPrmtr = new SqlParameter("@low_battery", SqlDbType.Bit);
                        SqlPrmtr.Value = ev.data.low_battery;
                        PrmtrList.Add(SqlPrmtr);

                        SqlPrmtr = new SqlParameter("@current_order", SqlDbType.VarChar);
                        if (ev.data.current_order == null)
                            SqlPrmtr.Value = "";
                        else
                            SqlPrmtr.Value = ev.data.current_order;

                        PrmtrList.Add(SqlPrmtr);


                        MainWindow.InsertIntoSql(CmdText, PrmtrList, "AGV added");
                    
                        //try
                        //{
                        //using (SqlConnection connection2 = new SqlConnection(connectionString))
                        //{
                        //    connection2.Open();

                        ////status_db.Content = "Data updated to db";
                        //SqlCommand cmd = new SqlCommand("UPDATE agv SET  type_name = @type_name ,type_name = @type_name, state = @state ,loaded1 = @loaded1, ems_auto = @ems_auto, " +
                        //    " ems_man = @ems_man,user_stop = @user_stop , blocked_by_agv = @blocked_by_agv, blocked_by_IO= blocked_by_IO , battery_state = @battery_state , " +
                        //    " low_battery = @low_battery ,curent_order = @curent_order Where  agv_id = @agv_id ", connection2);

                        //cmd.Parameters.AddWithValue("@agv_id", ev.data.id);
                        //cmd.Parameters.AddWithValue("@type_name", ev.data.type_name);
                        //cmd.Parameters.AddWithValue("@state", ev.data.state);
                        ////cmd.Parameters.AddWithValue("@loaded1", ev.data.loaded1);
                        ////cmd.Parameters.AddWithValue("@ems_auto", ev.data.emergency_stop_auto_reset);
                        ////cmd.Parameters.AddWithValue("@ems_man", ev.data.emergency_stop_manual_reset);
                        ////cmd.Parameters.AddWithValue("@user_stop", ev.data.user_stop);
                        ////cmd.Parameters.AddWithValue("@blocked_by_agv", ev.data.blocked_by_agv);
                        ////cmd.Parameters.AddWithValue("@blocked_by_IO", ev.data.blocked_by_IO);
                        //cmd.Parameters.AddWithValue("@loaded1", 1);
                        //cmd.Parameters.AddWithValue("@ems_auto", 1);
                        //cmd.Parameters.AddWithValue("@ems_man", 1);
                        //cmd.Parameters.AddWithValue("@user_stop", 1);
                        //cmd.Parameters.AddWithValue("@blocked_by_agv", 1);
                        //cmd.Parameters.AddWithValue("@blocked_by_IO",1);
                        //cmd.Parameters.AddWithValue("@battery_state",100);
                        //cmd.Parameters.AddWithValue("@low_battery", ev.data.low_battery);
                        //if (ev.data.curent_order == null)
                        //    cmd.Parameters.AddWithValue("@curent_order", "");
                        //else
                        //    cmd.Parameters.AddWithValue("@curent_order", ev.data.curent_order);

                        //        //Console.WriteLine(ev.payload.custom_data.input_val.ToString() + "  " + ev.payload.custom_data.Input_nam);

                        //        cmd.ExecuteNonQuery();



                        //        connection2.Close();
                        //    }
                        //}
                       
                        break;
                    }

                case var s when type.Contains("UnconnectedOrderCreatedEvent"):
                    {
                        var tcwo = new TransportOrderDefinition();
                    tcwo =MainWindow.NewcarwahTO();
                    var sys2 = new OrderClient(client2);
                        if (MainWindow.usew == true)
                            sys2.BaseUrl = "http://" + MainWindow.WIP_host + "/api/v1/";
                        else
                            sys2.BaseUrl = "http://" + MainWindow.IP_host + "/api/v1/";
                        try
                    {
                        var x2 = sys2.AckAsync(int.Parse(Ev_id), tcwo).GetAwaiter().GetResult();
                    }
                    catch
                    {
                        Console.WriteLine(" Problem with carwash  ");

                    }
                        break;
                    }

                case var s when type.Contains("OrderState"):
                    {
                        string order_id = ev.data.id;
                        string est_finished_time = ev.data.estimated_finish_time;
                       // DateTimeOffset fin= ev.data.estimated_finish_time;
                        // string agvs = ev.data.current_agvs[].Lenght;
                        int agv = 0;
                        if (((Newtonsoft.Json.Linq.JArray)ev.data.current_agvs).Count != 0)
                            agv =(int)ev.data.current_agvs.First.Value;
                        if (est_finished_time != null)
                        {
                            DateTimeOffset fin = ev.data.estimated_finish_time;
                            MainWindow.updateorder(order_id, fin, agv, 0);
                        }
                           
                        //MainWindow.updateorder(order_id, DateTimeOffset.Parse(est_finished_time), agv,0);

                        break;
                    }

                default:
                    {
                        Console.WriteLine("default");
                        break;
                    }
            }
           
                
        }
    }
}
