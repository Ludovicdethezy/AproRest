using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using LumenWorks.Framework.IO.Csv;
using LumenWorks.Framework.IO;
using Microsoft.Xaml.Behaviors;
using MartinCostello.SqlLocalDb;
using System.Reflection;

namespace AproRest
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    /// 

    //Data Source = (LocalDB)\MSSQLLocalDB;AttachDbFilename="D:\REPO\AproRest\AproRest\Nouvelle base de données.mdf";Integrated Security = True; Connect Timeout = 30
    public partial class MainWindow : Window
    {
        public string IP_host;
    public string IP_visu;
        public string Connect_string = @"Data Source = (LocalDB)\MSSQLLocalDB;  Integrated Security = True; ";
       public bool comok;
        public class custom_dataC
        {
            public string LoadWeight { get; set; }
            public string LoadDimensionX { get; set; }
            public string LoadDimensionY { get; set; }
            public string FetchHeight { get; set; }
            public string DeliverHeight { get; set; }

            public string FetchAddress { get; set; }

            public string DeliverAddress { get; set; }



        }



        public class ColorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                int id = 0;
                if (value != null && int.TryParse(value.ToString(), out id))
                {
                    if (id == 3)
                    {
                        return new SolidColorBrush(Colors.Red);
                    }
                }
                return new SolidColorBrush(Colors.Black);
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

   


        private void InsertIntoSql(string CmdText, List<SqlParameter> PrmtrList, string MsgDesc)
        {
            using (SqlConnection con = new SqlConnection(this.Connect_string))
            {
                SqlCommand SqlCmd_Insert = new SqlCommand();
                try
                {


                    con.Open();

                    SqlCmd_Insert.Connection = con;
                    SqlCmd_Insert.CommandTimeout = 5;
                    SqlCmd_Insert.CommandType = CommandType.Text;

                    SqlCmd_Insert.CommandText = CmdText;

                    List<SqlParameter> parameters = new List<SqlParameter>(PrmtrList);

                    foreach (SqlParameter Prmtr in parameters)
                    {
                        SqlCmd_Insert.Parameters.Add(Prmtr);
                    }

                    SqlCmd_Insert.ExecuteScalar();
                    con.Close();
                    //  Console.WriteLine("Nouvelle donnée insérée : " + MsgDesc);
                    //WriteLog("Nouvelle donnée insérée : " + Msg_1.ToString());
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
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("ERREUR ECRITURE ...");
                    Console.WriteLine(ex.Message.ToString());
                    // WriteLog("ERREUR ECRITURE ... " + ex.Message);

                    throw ex;
                }
                // autres exceptions seront levées dans le try catch appelant cette fonction

                SqlCmd_Insert.Dispose();
                if (con.State == ConnectionState.Open)
                    con.Close();
                SqlCmd_Insert = null;
            }
        }

        private TransportOrderDefinition NewTO(string fetch,string deliver, int delay)
        {

            TransportOrderStep stp1 = new TransportOrderStep
            {
                Operation_type = "Drop",
                Addresses = new string[] { deliver }
            };

            TransportOrderStep stp0 = new TransportOrderStep
            {
                Operation_type = "Pick",
                Addresses = new string[] { fetch }
            };
            custom_dataC custom = new custom_dataC
            {
                LoadWeight = Weight.Text,
                LoadDimensionX = DimX.Text,
                LoadDimensionY = DimY.Text,
                FetchHeight = Fetch_Height.Text,
                DeliverHeight = Deliver_Height.Text,
                FetchAddress = fetch,
                DeliverAddress = deliver
            };

            var to = new TransportOrderDefinition
            {
                Transport_order_id = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(),
                Transport_unit_type = "Pallet",
                Start_time = null,
                End_time = DateTimeOffset.UtcNow.AddMinutes(delay),
                Custom_data = custom,
                Steps = new TransportOrderStep[] { stp0, stp1 },
                Partial_steps = false,
            };

            string CmdText;
            SqlParameter SqlPrmtr;
            List<SqlParameter> PrmtrList;



            CmdText = "USE [order] INSERT INTO orderbuffer" +

                "([transport_order_id],[fetch_address],[fetch_height] ,[deliver_address],[deliver_height],[LoadDimensionX],[LoadDimensionY],[LoadWeight],[CreatedAt],[Sent]) " +

                "VALUES (@transport_order_id,@fetch_address,@fetch_height, @deliver_address,@deliver_height,@LoadDimensionX,@LoadDimensionY,@LoadWeight,@CreatedAt,@Sent)";

            PrmtrList = new List<SqlParameter>();

            SqlPrmtr = new SqlParameter("@transport_order_id", SqlDbType.VarChar);
            SqlPrmtr.Value = to.Transport_order_id;
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@fetch_address", SqlDbType.VarChar);
            SqlPrmtr.Value = custom.FetchAddress;
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@fetch_height", SqlDbType.Int);
            SqlPrmtr.Value = int.Parse(custom.FetchHeight);
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@deliver_address", SqlDbType.VarChar);
            SqlPrmtr.Value = custom.DeliverAddress;
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@deliver_height", SqlDbType.Int);
            SqlPrmtr.Value = int.Parse(custom.DeliverHeight);
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@LoadDimensionX", SqlDbType.Int);
            SqlPrmtr.Value = int.Parse(custom.LoadDimensionX); ;
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@LoadDimensionY", SqlDbType.Int);
            SqlPrmtr.Value = int.Parse(custom.LoadDimensionY); ;
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@LoadWeight", SqlDbType.Int);
            SqlPrmtr.Value = int.Parse(custom.LoadWeight);
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@CreatedAt", SqlDbType.DateTimeOffset);
            SqlPrmtr.Value = DateTimeOffset.UtcNow;
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@Sent", SqlDbType.TinyInt);
            SqlPrmtr.Value = 1;
            PrmtrList.Add(SqlPrmtr);

            InsertIntoSql(CmdText, PrmtrList, "new order added");


            return to;
        }


        private TransportOrderDefinition NewcarwahTO()
        {

            TransportOrderStep stp1 = new TransportOrderStep
            {
                Operation_type = "Drop",
                Addresses = new string[] { "A5" }
            };


            custom_dataC custom = new custom_dataC
            {
                LoadWeight = "0",
                LoadDimensionX = "0",
                LoadDimensionY = "0",
                FetchHeight = "0",
                DeliverHeight = "0",
                FetchAddress = "carwash",
                DeliverAddress = "A5",
            };

            var to = new TransportOrderDefinition
            {
                Transport_order_id = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(),
                Transport_unit_type = "Pallet",
                Start_time = null,
                End_time = DateTimeOffset.UtcNow.AddMinutes(10),
                Custom_data = custom,
                Steps = new TransportOrderStep[] { stp1 },
                Partial_steps = false,
            };

            string CmdText;
            SqlParameter SqlPrmtr;
            List<SqlParameter> PrmtrList;



            CmdText = "USE [order] INSERT INTO orderbuffer" +

                "([transport_order_id],[fetch_address],[fetch_height] ,[deliver_address],[deliver_height],[LoadDimensionX],[LoadDimensionY],[LoadWeight],[CreatedAt],[Sent]) " +

                "VALUES (@transport_order_id,@fetch_address,@fetch_height, @deliver_address,@deliver_height,@LoadDimensionX,@LoadDimensionY,@LoadWeight,@CreatedAt,@Sent)";

            PrmtrList = new List<SqlParameter>();

            SqlPrmtr = new SqlParameter("@transport_order_id", SqlDbType.VarChar);
            SqlPrmtr.Value = to.Transport_order_id;
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@fetch_address", SqlDbType.VarChar);
            SqlPrmtr.Value = custom.FetchAddress;
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@fetch_height", SqlDbType.Int);
            SqlPrmtr.Value = int.Parse(custom.FetchHeight);
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@deliver_address", SqlDbType.VarChar);
            SqlPrmtr.Value = custom.DeliverAddress;
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@deliver_height", SqlDbType.Int);
            SqlPrmtr.Value = int.Parse(custom.DeliverHeight);
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@LoadDimensionX", SqlDbType.Int);
            SqlPrmtr.Value = int.Parse(custom.LoadDimensionX); ;
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@LoadDimensionY", SqlDbType.Int);
            SqlPrmtr.Value = int.Parse(custom.LoadDimensionY); ;
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@LoadWeight", SqlDbType.Int);
            SqlPrmtr.Value = int.Parse(custom.LoadWeight);
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@CreatedAt", SqlDbType.DateTimeOffset);
            SqlPrmtr.Value = DateTimeOffset.UtcNow;
            PrmtrList.Add(SqlPrmtr);

            SqlPrmtr = new SqlParameter("@Sent", SqlDbType.TinyInt);
            SqlPrmtr.Value = 1;
            PrmtrList.Add(SqlPrmtr);

            InsertIntoSql(CmdText, PrmtrList, "new order added");


            return to;
        }


        private delegate void InvokeDelegate();
        public void getack()
        {
            HttpClient client2 = new HttpClient();
            var sys = new Client(client2);
            sys.BaseUrl = "http://" + IP_host + "/api/v1/";
            while (true)
            {

                System.Threading.Thread.Sleep(5000);


                try
                {

                    if (comok == true)
                    {

                        var x = sys.EventsAllAsync(10, true, 0, null).GetAwaiter().GetResult();

                        foreach (Event item in x)
                        {
                            Console.WriteLine(item.Event_id);

                            Console.WriteLine(item.Created_at);
                            Console.WriteLine(item.Type);


                            if (item.Type == "AgvOperationEndEvent")
                            {

                                item.Payload.AdditionalProperties.TryGetValue("address", out object value);
                                item.Payload.AdditionalProperties.TryGetValue("transport_order_id", out object value2);
                                item.Payload.AdditionalProperties.TryGetValue("step_index", out object value3);
                                item.Payload.AdditionalProperties.TryGetValue("drive_start_time", out object value4);
                                Console.WriteLine(" validation adress: {0},transport_order_id : {1} , step {2} ", value, value2, value3);
                                Dispatcher.Invoke(new InvokeDelegate(() =>
                                {
                                    this.log2.Items.Add("adresse: " + value.ToString() + " , transport_order_id: " + value2.ToString() + ", step: " + value3.ToString());
                                }));
                                sys.ContinueAsync(value2.ToString(), item.Event_id);
                                Console.WriteLine(" ack operation envoyé ");
                                Dispatcher.Invoke(new InvokeDelegate(() =>
                                {
                                    this.log2.Items.Add(" ack operation envoyé adresse: " + value.ToString() + " , transport_order_id: " + value2.ToString() + ", step: " + value3.ToString());
                                }));
                                SqlCommand command = new SqlCommand();

                                string CmdText;
                                SqlParameter SqlPrmtr;
                                List<SqlParameter> PrmtrList;

                                CmdText = "USE [order] INSERT INTO ackevent" +

                                    "([transport_order_id],[type],[step_index] ,[address],[created_at],[event_id],[waiting_acknowledgment]) " +

                                    "VALUES (@transport_order_id,@type,@step_index ,@address,@created_at,@event_id,@waiting_acknowledgment)";

                                PrmtrList = new List<SqlParameter>();

                                SqlPrmtr = new SqlParameter("@transport_order_id", SqlDbType.VarChar);
                                SqlPrmtr.Value = value2.ToString();
                                PrmtrList.Add(SqlPrmtr);

                                SqlPrmtr = new SqlParameter("@type", SqlDbType.VarChar);
                                SqlPrmtr.Value = item.Type;
                                PrmtrList.Add(SqlPrmtr);

                                SqlPrmtr = new SqlParameter("@Step_index", SqlDbType.VarChar);
                                SqlPrmtr.Value = value3.ToString();
                                PrmtrList.Add(SqlPrmtr);

                                SqlPrmtr = new SqlParameter("@address", SqlDbType.VarChar);
                                SqlPrmtr.Value = value.ToString();
                                PrmtrList.Add(SqlPrmtr);

                                SqlPrmtr = new SqlParameter("@created_at", SqlDbType.VarChar);
                                SqlPrmtr.Value = item.Created_at.ToString();
                                PrmtrList.Add(SqlPrmtr);

                                SqlPrmtr = new SqlParameter("@event_id", SqlDbType.VarChar);
                                SqlPrmtr.Value = item.Event_id;
                                PrmtrList.Add(SqlPrmtr);

                                SqlPrmtr = new SqlParameter("@waiting_acknowledgment", SqlDbType.VarChar);
                                SqlPrmtr.Value = "TRUE";
                                PrmtrList.Add(SqlPrmtr);

                                InsertIntoSql(CmdText, PrmtrList, "ack operation added");
                                string status;
                                if (value3.ToString() == "0")
                                {
                                    status = "Palett fetched";
                                    updatedb(value2.ToString(), DateTimeOffset.Parse(value4.ToString()), status);
                                }
                                else
                                {
                                    status = "Palett delivered";
                                    updateendoforder(value2.ToString(), DateTimeOffset.Parse(item.Created_at.ToString()), status);

                                }


                                // refresh_orderDG();

                            }
                            if (item.Type == "AgvArrivedToAddressEvent")
                            {
                                item.Payload.AdditionalProperties.TryGetValue("address", out object value);
                                item.Payload.AdditionalProperties.TryGetValue("transport_order_id", out object value2);
                                item.Payload.AdditionalProperties.TryGetValue("step_index", out object value3);
                                item.Payload.AdditionalProperties.TryGetValue("drive_start_time", out object value4);
                                Console.WriteLine("adress: {0},transport_order_id : {1} , step {2} ", value, value2, value3);
                                Dispatcher.Invoke(new InvokeDelegate(() =>
                                {
                                    this.log2.Items.Add("adresse: " + value.ToString() + " , transport_order_id: " + value2.ToString() + ", step: " + value3.ToString());
                                }));
                                sys.ContinueAsync(value2.ToString(), item.Event_id);
                                Console.WriteLine(" ack arrivé envoyé ");
                                Dispatcher.Invoke(new InvokeDelegate(() =>
                                {
                                    this.log2.Items.Add(" ack arrivée envoyé : adresse: " + value.ToString() + " , transport_order_id: " + value2.ToString() + ", step: " + value3.ToString());
                                //  this.log.Items[this.log.Items.Count - 1].Background = (Brush)new BrushConverter().ConvertFromString("#FF00B25A")
                            }));

                                string CmdText;
                                SqlParameter SqlPrmtr;
                                List<SqlParameter> PrmtrList;



                                CmdText = "USE [order] INSERT INTO ackevent" +

                                    "([transport_order_id],[type],[step_index] ,[address],[created_at],[event_id],[waiting_acknowledgment]) " +

                                    "VALUES (@transport_order_id,@type,@step_index ,@address,@created_at,@event_id,@waiting_acknowledgment)";

                                PrmtrList = new List<SqlParameter>();

                                SqlPrmtr = new SqlParameter("@transport_order_id", SqlDbType.VarChar);
                                SqlPrmtr.Value = value2.ToString();
                                PrmtrList.Add(SqlPrmtr);

                                SqlPrmtr = new SqlParameter("@type", SqlDbType.VarChar);
                                SqlPrmtr.Value = item.Type;
                                PrmtrList.Add(SqlPrmtr);

                                SqlPrmtr = new SqlParameter("@Step_index", SqlDbType.VarChar);
                                SqlPrmtr.Value = value3.ToString();
                                PrmtrList.Add(SqlPrmtr);

                                SqlPrmtr = new SqlParameter("@address", SqlDbType.VarChar);
                                SqlPrmtr.Value = value.ToString();
                                PrmtrList.Add(SqlPrmtr);

                                SqlPrmtr = new SqlParameter("@created_at", SqlDbType.VarChar);
                                SqlPrmtr.Value = item.Created_at.ToString();
                                PrmtrList.Add(SqlPrmtr);

                                SqlPrmtr = new SqlParameter("@event_id", SqlDbType.VarChar);
                                SqlPrmtr.Value = item.Event_id;
                                PrmtrList.Add(SqlPrmtr);

                                SqlPrmtr = new SqlParameter("@waiting_acknowledgment", SqlDbType.VarChar);
                                SqlPrmtr.Value = "TRUE";
                                PrmtrList.Add(SqlPrmtr);

                                InsertIntoSql(CmdText, PrmtrList, "ack preload added");
                                string status;
                                if (value3.ToString() == "0")
                                    status = "AGV ready to fetch";
                                else
                                    status = "AGV ready to deliver";


                                updatedb(value2.ToString(), DateTimeOffset.Parse(value4.ToString()), status);
                                //refresh_orderDG();

                            }
                            if (item.Type == "UnconnectedOrderCreatedEvent")
                            {
                                Console.WriteLine("carwash");

                                var tcwo = new TransportOrderDefinition();
                                tcwo = NewcarwahTO();
                                var sys2 = new OrderClient(client2);
                                try
                                {
                                    var x2 = sys2.AckAsync(item.Event_id, tcwo).GetAwaiter().GetResult();
                                }
                                catch
                                { }

                                //refresh_orderDG();

                            }

                        }
                        refresh_orderDG();

                    }
                }
                catch
                {
                    Console.WriteLine(" Problem with ack ");
                }
            }
        }
        public void getevent()
        {
            HttpClient client2 = new HttpClient();
            
            var sys = new Client(client2);
            sys.BaseUrl = "http://" + IP_host + "/api/v1/";
            int ev_id = 0;

            while (true)
            {

                System.Threading.Thread.Sleep(5000);

                if (comok == true)
                {
                    Console.ResetColor();
                    try
                    {


                        var x = sys.EventsAllAsync(10, false, ev_id, null).GetAwaiter().GetResult();

                        foreach (Event item in x)
                        {
                            //Console.WriteLine(item.Event_id);
                            if (item.Event_id > ev_id)
                            { ev_id = item.Event_id; }
                            //  Console.WriteLine(item.Created_at);
                            //  Console.WriteLine(item.Type);
                            if (item.Type == "TransportOrderEndEvent")
                            {
                                IDictionary<string, object> icoll = item.Payload.AdditionalProperties;

                                object value2;
                                object value3;
                                //icoll.TryGetValue("address", out value);
                                item.Payload.AdditionalProperties.TryGetValue("transport_order_id", out value2);
                                item.Payload.AdditionalProperties.TryGetValue("end_reason", out value3);

                                Console.WriteLine(" fin ordre : {0},transport_order_id : {1}  ", value2, value3);
                                Dispatcher.Invoke(new InvokeDelegate(() =>
                                {
                                    this.log2.Items.Add("fin ordre:  transport_order_id: " + value2.ToString() + ", reason: " + value3.ToString());
                                    this.log2.SelectedItem = this.log2.Items.Count;
                                    updateendoforder(value2.ToString(), DateTimeOffset.Parse(item.Created_at.ToString()), value3.ToString());
                                    //affich.Text = log.Items.Count.ToString();
                                }));
                            }
                            else
                            {

                                Dispatcher.Invoke(new InvokeDelegate(() =>
                                {
                                    this.log2.Items.Add(item.Type + ", id: " + item.Event_id + "date: " + item.Created_at);
                                    this.log2.SelectedItem = this.log2.Items.Count;
                                    //affich.Text = log.Items.Count.ToString();
                                }));
                            }
                        }
                        insert_agv();

                        //refresh_orderDG();
                        //if (item.Type == "AgvOperationEndEvent")
                        //{
                        //    IDictionary<string, object> icoll = item.Payload.AdditionalProperties;
                        //    object value;
                        //    object value2;
                        //    object value3;
                        //    icoll.TryGetValue("address", out value);
                        //    item.Payload.AdditionalProperties.TryGetValue("transport_order_id", out value2);
                        //    item.Payload.AdditionalProperties.TryGetValue("step_index", out value3);

                        //    Console.WriteLine(" validation adress: {0},transport_order_id : {1} , step {2} ", value, value2, value3);
                        //    Dispatcher.Invoke(new InvokeDelegate(() =>
                        //    {
                        //        this.log2.Items.Add("adresse: " + value.ToString() + " , transport_order_id: " + value2.ToString() + ", step: " + value3.ToString());

                        //        //affich.Text = log.Items.Count.ToString();
                        //    }));
                        //    sys.ContinueAsync(value2.ToString(), item.Event_id);
                        //    Console.WriteLine(" ack operation envoyé ");
                        //    Dispatcher.Invoke(new InvokeDelegate(() =>
                        //    {
                        //        this.log2.Items.Add(" ack operation envoyé adresse: " + value.ToString() + " , transport_order_id: " + value2.ToString() + ", step: " + value3.ToString());
                        //    }));
                        //}
                        //if (item.Type == "AgvArrivedToAddressEvent")
                        //{
                        //    //Console.WriteLine(item.Payload.AdditionalProperties.TryGetValue("addresse");
                        //    IDictionary<string, object> icoll = item.Payload.AdditionalProperties;
                        //    object value;
                        //    object value2;
                        //    object value3;
                        //    icoll.TryGetValue("address", out value);
                        //    item.Payload.AdditionalProperties.TryGetValue("transport_order_id", out value2);
                        //    item.Payload.AdditionalProperties.TryGetValue("step_index", out value3);
                        //    Console.WriteLine("adress: {0},transport_order_id : {1} , step {2} ", value, value2, value3);
                        //    Dispatcher.Invoke(new InvokeDelegate(() =>
                        //    {
                        //        this.log2.Items.Add("adresse: " + value.ToString() + " , transport_order_id: " + value2.ToString() + ", step: " + value3.ToString());
                        //    }));



                        //    sys.ContinueAsync(value2.ToString(), item.Event_id);
                        //    Console.WriteLine(" ack arrivé envoyé ");
                        //    Dispatcher.Invoke(new InvokeDelegate(() =>
                        //    {
                        //        this.log2.Items.Add(" ack arrivée envoyé : adresse: " + value.ToString() + " , transport_order_id: " + value2.ToString() + ", step: " + value3.ToString());
                        //    }));



                        //}

                    }

                    catch (FormatException ex)
                    {
                        Console.WriteLine(" Problem with event thread : {0}", ex.Message.ToString());

                    }
                    catch
                    {

                    }


                }
            }
        }



        public void insert_agv()
        {
            string CmdText;


            string connectionString = Connect_string;


            SqlParameter SqlPrmtr;
            List<SqlParameter> PrmtrList;
            HttpClient clientvisu = new HttpClient();
            var svisu = new visu.Client(clientvisu);
            svisu.BaseUrl = "http://"+IP_visu+ "/api/v1/";
            var x3 = svisu.AgvsAllAsync().GetAwaiter().GetResult();
            foreach (visu.AgvState agvState in x3)
            {
                // Console.WriteLine("{0} loaded {1} ", agvState.Id, agvState.Loaded1);

                try
                {
                    using (SqlConnection connection2 = new SqlConnection(connectionString))
                    {
                        connection2.Open();
                        SqlCommand cmd2 = new SqlCommand("USE [order] DELETE FROM agvs WHERE agv_id = " + agvState.Id + " ", connection2);
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



                CmdText = "USE [order] INSERT INTO agvs" +

                    "([agv_id],[type_name],[state],[loaded1],[ems_auto],[ems_man],[user_stop],[blocked_by_agv],[blocked_by_IO],[error],[battery_state],[low_battery],[current_order]) " +

                    "VALUES (@agv_id,@type_name,@state ,@loaded1,@ems_auto,@ems_man,@user_stop,@blocked_by_agv,@blocked_by_IO,@error,@battery_state,@low_battery,@current_order)";

                PrmtrList = new List<SqlParameter>();

                SqlPrmtr = new SqlParameter("@agv_id", SqlDbType.Int);
                SqlPrmtr.Value = agvState.Id;
                PrmtrList.Add(SqlPrmtr);

                SqlPrmtr = new SqlParameter("@type_name", SqlDbType.VarChar);
                SqlPrmtr.Value = agvState.Type_name;
                PrmtrList.Add(SqlPrmtr);

                SqlPrmtr = new SqlParameter("@state", SqlDbType.VarChar);
                SqlPrmtr.Value = agvState.State;
                PrmtrList.Add(SqlPrmtr);

                SqlPrmtr = new SqlParameter("@loaded1", SqlDbType.Bit);
                SqlPrmtr.Value = agvState.Loaded1;
                PrmtrList.Add(SqlPrmtr);

                SqlPrmtr = new SqlParameter("@ems_auto", SqlDbType.Bit);
                SqlPrmtr.Value = agvState.Emergency_stop_auto_reset;
                PrmtrList.Add(SqlPrmtr);

                SqlPrmtr = new SqlParameter("@ems_man", SqlDbType.Bit);
                SqlPrmtr.Value = agvState.Emergency_stop_manual_reset;
                PrmtrList.Add(SqlPrmtr);

                SqlPrmtr = new SqlParameter("@user_stop", SqlDbType.Bit);
                SqlPrmtr.Value = agvState.User_stop;
                PrmtrList.Add(SqlPrmtr);

                SqlPrmtr = new SqlParameter("@blocked_by_agv", SqlDbType.Bit);
                SqlPrmtr.Value = agvState.Blocked_by_agv;
                PrmtrList.Add(SqlPrmtr);

                SqlPrmtr = new SqlParameter("@blocked_by_IO", SqlDbType.Bit);
                SqlPrmtr.Value = agvState.Blocked_by_io;
                PrmtrList.Add(SqlPrmtr);

                SqlPrmtr = new SqlParameter("@error", SqlDbType.Bit);
                SqlPrmtr.Value = agvState.Blocked_by_io;
                PrmtrList.Add(SqlPrmtr);

                SqlPrmtr = new SqlParameter("@battery_state", SqlDbType.Int);
                SqlPrmtr.Value = agvState.Battery_state;
                PrmtrList.Add(SqlPrmtr);

                SqlPrmtr = new SqlParameter("@low_battery", SqlDbType.Bit);
                SqlPrmtr.Value = agvState.Low_battery;
                PrmtrList.Add(SqlPrmtr);

                SqlPrmtr = new SqlParameter("@current_order", SqlDbType.VarChar);
                if (agvState.Current_order == null)
                    SqlPrmtr.Value = "";
                else
                    SqlPrmtr.Value = agvState.Current_order;

                PrmtrList.Add(SqlPrmtr);


                InsertIntoSql(CmdText, PrmtrList, "ack preload added");


                var x4 = svisu.OrdersAllAsync().GetAwaiter().GetResult();
                foreach (visu.OrderState orderstate in x4)
                {
                    int curagv2 = 0;
                    foreach (int curagv in orderstate.Current_agvs)
                    {
                        //updateorder(orderstate.Id, DateTimeOffset.Parse(orderstate.Estimated_finish_time.ToString()), curagv);
                        curagv2 = curagv;
                    }
                    if (orderstate.Estimated_finish_time != null)
                    updateorder(orderstate.Id, DateTimeOffset.Parse(orderstate.Estimated_finish_time.ToString()), curagv2);

                }
            }

            using (SqlConnection connection2 = new SqlConnection(connectionString))
            {
                connection2.Open();
                SqlCommand cmd = new SqlCommand("USE [order] SELECT * FROM dbo.agvs", connection2);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                sda.Fill(ds);

                Dispatcher.Invoke(new InvokeDelegate(() =>
                {
                    status_db.Content = "Data refresh to db";
                    status_db.Background = (Brush)new BrushConverter().ConvertFromString("#FFFFB25A");
                    agv.ItemsSource = ds.Tables[0].DefaultView;
                    //  this.order_list.Resources["columnForeground"] = Brushes.Red;

                }));




            }



            // sda.SelectCommand = cmd;




        }



        public void refresh_orderDG()
        {
            string connectionString = Connect_string;
            DataSet ds = new DataSet();
            try
            {


                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {

                    try
                    {
                        connection2.Open();
                        string str = "SELECT  [id] ,[transport_order_id] ,[fetch_address] ,[fetch_height] ,[deliver_address] ,[deliver_height],+" +
                            "[LoadDimensionX],[LoadDimensionY],[LoadWeight],[CreatedAt] AT TIME ZONE 'Central Europe Standard Time' AS[CreatedAt] ,[StartedAt] " +
                            "AT TIME ZONE 'Central Europe Standard Time' AS[StartedAt] ,[FinishedAt] AT TIME ZONE 'Central Europe Standard Time' AS[FinishedAt]  ,[CurentStatus] " +
                            " ,[Sent] ,agv FROM[order].[dbo].[orderbuffer] WHERE CreatedAt>getdate()-1 order by CreatedAt desc ";



                        SqlCommand cmd = new SqlCommand(str, connection2);

                        SqlDataAdapter sda = new SqlDataAdapter(cmd);

                        // sda.SelectCommand = cmd;
                        sda.Fill(ds);


                        Dispatcher.Invoke(new InvokeDelegate(() =>
                        {
                            status_db.Content = "Data refresh to db";
                            status_db.Background = (Brush)new BrushConverter().ConvertFromString("#FFFFB25A");
                            order_list.ItemsSource = ds.Tables[0].DefaultView;
                        //  this.order_list.Resources["columnForeground"] = Brushes.Red;

                    }));
                    }
                    catch
                    { }



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
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("ERREUR ECRITURE ...");
                Console.WriteLine(ex.Message.ToString());
                // WriteLog("ERREUR ECRITURE ... " + ex.Message);

                throw ex;
            }
            
        }

        public void updatedb(string toid, DateTimeOffset start, string curentstatus)
        {
            string connectionString = Connect_string;

            try
            {
                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {
                    connection2.Open();

                    //status_db.Content = "Data updated to db";
                    SqlCommand cmd = new SqlCommand("USE [order] UPDATE orderbuffer SET  StartedAt = @start, CurentStatus = @status Where  transport_order_id = @toid ", connection2);

                    cmd.Parameters.AddWithValue("@start", start.AddHours(2));
                    cmd.Parameters.AddWithValue("@status", curentstatus);
                    cmd.Parameters.AddWithValue("@toid", toid);

                    cmd.ExecuteNonQuery();

                    Dispatcher.Invoke(new InvokeDelegate(() =>
                    {
                        status_db.Content = "Data updated to db";
                        status_db.Background = (Brush)new BrushConverter().ConvertFromString("#FF00B25A");
                    }));

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
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("ERREUR ECRITURE ...");
                Console.WriteLine(ex.Message.ToString());
                // WriteLog("ERREUR ECRITURE ... " + ex.Message);

                throw ex;
            }
        }

        public void updateorder(string toid, DateTimeOffset start, int agv)
        {
            string connectionString = Connect_string ;

            try
            {
                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {
                    connection2.Open();

                    //status_db.Content = "Data updated to db";
                    SqlCommand cmd = new SqlCommand("USE [order] UPDATE orderbuffer SET  FinishedAt = @start, agv = @agv Where  transport_order_id = @toid ", connection2);

                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@agv", agv);
                    cmd.Parameters.AddWithValue("@toid", toid);

                    cmd.ExecuteNonQuery();

                    Dispatcher.Invoke(new InvokeDelegate(() =>
                    {
                        status_db.Content = "Data updated to db";
                        status_db.Background = (Brush)new BrushConverter().ConvertFromString("#FF00B25A");
                    }));

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
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("ERREUR ECRITURE ...");
                Console.WriteLine(ex.Message.ToString());
                // WriteLog("ERREUR ECRITURE ... " + ex.Message);

                throw ex;
            }
            catch (FormatException ex)
            {
                Console.WriteLine(" Problem : {0}", ex.Message.ToString());
                throw ex;

            }
        }


        public void updateendoforder(string toid, DateTimeOffset end, string curentstatus)
        {
            string connectionString = Connect_string;

            try
            {
                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {
                    connection2.Open();

                    //status_db.Content = "Data updated to db";
                    SqlCommand cmd = new SqlCommand("USE [order] UPDATE orderbuffer SET  FinishedAt = @start, CurentStatus = @status Where  transport_order_id = @toid ", connection2);

                    cmd.Parameters.AddWithValue("@start", end);
                    cmd.Parameters.AddWithValue("@status", curentstatus);
                    cmd.Parameters.AddWithValue("@toid", toid);

                    cmd.ExecuteNonQuery();


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
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("ERREUR ECRITURE ...");
                Console.WriteLine(ex.Message.ToString());
                // WriteLog("ERREUR ECRITURE ... " + ex.Message);

                throw ex;
            }
        }

        public void connection()
        {
            HttpClient client2 = new HttpClient();
            IP_host = Ip_host.Text;
            var sys = new Client(client2);
            sys.BaseUrl = "http://" + IP_host + "/api/v1/";
            try
            {
                var status2 = sys.SystemAsync();
                status.Content = "Server : " + status2.Result.Machine_name + "  |  system name : " + status2.Result.System_name + "  | IP : " + IP_host;
                status.Background = (Brush)new BrushConverter().ConvertFromString("#FF00B25A");

                if (status2.Result.Machine_name == null)
                {
                    status.Content = "Server : " + "no connection" + "  |  system name : " + "no system";
                    status.Background = (Brush)new BrushConverter().ConvertFromString("#ff9999");

                }

            }
            catch
            {
                status.Content = "Server : " + "no connection" + "  |  system name : " + "no system | error ip ";
                status.Background = (Brush)new BrushConverter().ConvertFromString("#ff9999");
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            string loc = "AproRest.mdf";
            string directory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().GetName().CodeBase)+loc;

            // string DataSource = "(LocalDB)\MSSQLLocalDB AttachDbFilename = "+loc+"; Integrated Security = True; Connect Timeout = 30";
            string connectionString = @"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = "+ loc +";  Integrated Security = True; ";
          //  Connect_string = connectionString;
          //  
            connection();
           
            //HttpClient client2 = new HttpClient();
            
            //var sys = new Client(client2);
            //sys.BaseUrl = "http://" + IP_host + "/api/v1/";
            //try
            //{
            //    var status2 = sys.SystemAsync();
            //    status.Content = "Server : " + status2.Result.Machine_name + "  |  system name : " + status2.Result.System_name +" IP" + IP_host;
            //    status.Background = (Brush)new BrushConverter().ConvertFromString("#FF00B25A");
            //}
            //catch
            //{
            //    status.Content = "Server : " + "no connection" + "  |  system name : " + "no system";
            //    status.Background = (Brush)new BrushConverter().ConvertFromString("#ff9999");
            //}
            using (SqlConnection connection2 = new SqlConnection(Connect_string))
            {

                try
                {
                    connection2.Open();

                    SqlCommand cmd2 = new SqlCommand("USE [order] TRUNCATE TABLE agvs ", connection2);
                    var exist = cmd2.ExecuteNonQuery();
                }
                catch (System.Exception ex)
                {
                    //MessageBox.Show(ex.ToString(), "Create Database", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                finally
                {
                    connection2.Close();
                }
            }
            refresh_orderDG();
            Thread a = new Thread(getack);
            a.IsBackground = true;
            a.Start();
            Thread b = new Thread(getevent);
            b.IsBackground = true;
            b.Start();
            HttpClient client2 = new HttpClient();
            var sys = new Client(client2);
           

        }
        private void OnWindowclose(object sender, EventArgs e)
        {
            Environment.Exit(Environment.ExitCode); // Prevent memory leak
                                                    // System.Windows.Application.Current.Shutdown(); // Not sure if needed
        }
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            HttpClient client2 = new HttpClient();
            var sys = new Client(client2);
            sys.BaseUrl = "http://" + IP_host + "/api/v1/";


            var to = new TransportOrderDefinition();


            try
            {


                to = NewTO(Fetch_address.Text, Deliver_address.Text, 10);
                var x2 = sys.Orders2Async(to, to.Transport_order_id).GetAwaiter().GetResult();

            }
            catch (ApiException ex)
            {
                Console.WriteLine("ERREUR ordre ...{0}", to.Transport_order_id);
                Console.WriteLine(ex.Message.ToString());

                updatedb(to.Transport_order_id, DateTime.Now, "Error");

                
            }

            //refresh_orderDG();




        }



        private void cancel_Click(object sender, RoutedEventArgs e)
        {

            if (order_list.SelectedItem != null)
            {// insert_agv();

                string connectionString = Connect_string;

                DataRowView dataRowView = (DataRowView)order_list.SelectedItem;
                int ID = Convert.ToInt32(dataRowView.Row[0]);
                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {
                    connection2.Open();
                    SqlCommand cmd2 = new SqlCommand("USE [order] SELECT transport_order_id FROM orderbuffer WHERE id = " + ID + " ", connection2);
                    var exist = cmd2.ExecuteScalar();
                    connection2.Close();
                    HttpClient client2 = new HttpClient();
                    var sys = new Client(client2);
                    sys.BaseUrl = "http://" + IP_host + "/api/v1/";
                    var x = sys.Orders3Async(false, exist.ToString());

                }

            }

        }



        private void Button_Click(object sender, RoutedEventArgs e)
        {

            if (order_list.SelectedItem != null)
            {// insert_agv();
                string connectionString = Connect_string;

                DataRowView dataRowView = (DataRowView)order_list.SelectedItem;
                int ID = Convert.ToInt32(dataRowView.Row[0]);
                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {
                    connection2.Open();
                    SqlCommand cmd2 = new SqlCommand("USE [order] DELETE FROM orderbuffer WHERE id = " + ID + " ", connection2);
                    var exist = cmd2.ExecuteNonQuery();
                    connection2.Close();


                }
            }

        }

        private void Agv_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "StartedAt")
                (e.Column as DataGridTextColumn).Binding.StringFormat = "dd.MM HH:mm:ss";



            if (e.PropertyName == "FinishedAt")
                (e.Column as DataGridTextColumn).Binding.StringFormat = "dd.MM HH:mm:ss";
            if (e.PropertyName == "CreatedAt")
                (e.Column as DataGridTextColumn).Binding.StringFormat = "dd.MM HH:mm:ss";
        }

        private void cancel2_Click(object sender, RoutedEventArgs e)
        {
            string connectionString = Connect_string;


            using (SqlConnection connection2 = new SqlConnection(connectionString))
            {
                connection2.Open();
                SqlCommand cmd2 = new SqlCommand("USE [order] TRUNCATE TABLE orderbuffer ", connection2);
                var exist = cmd2.ExecuteScalar();
                connection2.Close();

            }
            log2.Items.Clear();
        }

        private void Csv_loazd_Click(object sender, RoutedEventArgs e)
        {
            var csvTable = new DataTable();
            HttpClient client2 = new HttpClient();
            var sys = new Client(client2);
            sys.BaseUrl = "http://" + IP_host + "/api/v1/";
            var to = new TransportOrderDefinition();
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "Document"; // Default file name
            dialog.DefaultExt = ".csv"; // Default file extension
            dialog.Filter = "Text documents (.csv)|*.csv"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();
            string filename;
            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                 filename = dialog.FileName;
                 using (var csvReader = new CsvReader(new StreamReader(System.IO.File.OpenRead(filename)), true,';'))
                            {
                                csvTable.Load(csvReader);
                            }
                            string Column1 = csvTable.Columns[0].ToString();
                            string Row1 = csvTable.Rows[0][1].ToString();
                            foreach (DataRow dr in csvTable.Rows)
                            {
                                Console.WriteLine("{0}, {1}, {2}", dr[0].ToString(), dr[1].ToString(), dr[2].ToString());

                
                                try
                                {

                                    to = NewTO(dr[0].ToString(), dr[1].ToString(), int.Parse(dr[2].ToString()));
                    
                                    var x2 = sys.Orders2Async(to, to.Transport_order_id).GetAwaiter().GetResult();

                                }
                                catch (ApiException ex)
                                {
                                    Console.WriteLine("ERREUR ordre ...{0}", to.Transport_order_id);
                                    Console.WriteLine(ex.Message.ToString());

                                    updatedb(to.Transport_order_id, DateTime.Now, "Error "+ex.StatusCode);
                    
                                }

                                // refresh_orderDG();
Console.WriteLine(Column1, Row1);
        }

                            }
            }

           

            

      

        private void ip(object sender, TextChangedEventArgs e)
        {
            IP_host = Ip_host.Text;
            
            connection();
        }

        private void ipvisu(object sender, TextChangedEventArgs e)
        {
            IP_visu = Ip_visu.Text;
        }

        private void Connect_Checked(object sender, RoutedEventArgs e)
        {
            if (connect.IsChecked == true)
            {
                comok = true;
            }
            else
            {
                comok = false;
                status.Content = "Server : " + "no connection" + "  |  system name : " + "no system | error ip ";
                status.Background = (Brush)new BrushConverter().ConvertFromString("#ff9999");
            }
        }
    }
    
}
