﻿using System;
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
using System.Text.Json;
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
using AproRest.webhook;
using HtmlAgilityPack;
using System.Diagnostics;


namespace AproRest
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    /// 

    //Data Source = (LocalDB)\MSSQLLocalDB;AttachDbFilename="D:\REPO\AproRest\AproRest\Nouvelle base de données.mdf";Integrated Security = True; Connect Timeout = 30
    public partial class MainWindow : Window
    {
        public static string IP_host;
        public string IP_visu;
        public string IP_utility;
        public static string WIP_host;
        public string WIP_visu;
        public string WIP_utility;
        public static string carwash;
        public static string Connect_string = @"Data Source = (LocalDB)\MSSQLLocalDB;  Integrated Security = True; ";
        public bool comok;
        public bool autoack;
        public static bool autoackw = true;
        public static bool usew = false;
        public static bool useb = false;
        public bool web;
        public DBConnection DBConn = new DBConnection();
        public int ev_id = 0;
        public string stoid;
        public string sever2_adress;
        private string loca_serv2_adress;
        public string forward_adress;
        public string ctype = "0";

        Thread c;
        Thread d;
        private bool forward_enable;
        private bool web2 = false;
        private string ip_host_webhooks = "http://+:8001";
        private string loca_ip_host_webbhooks;
        private string ip_visu_webhooks;
        private string test_ip;

        public string Loca_test_ip { get; private set; }

        private bool test2;
        private string test_server_ips;
        private bool enablevisu;
        private string loca_ip_visu_webbhooks;
        public bool cfetch = false;
        public bool cdeliver = true;

        public string LoadWeigh;

        public string LoadWeight { get; private set; }

        public string LoadDimensionX;
        public string LoadDimensionY;
        public string FetchHeight;
        public string DeliverHeight;

        public string FetchAddress;

        public string DeliverAddress;

        public string AGV;
        public string Area;
        public string palett;

        public int Sequence;
        private bool useb_drop;
        private bool auto_increment;
        private bool use_fetch;

        public class custom_dataC
        {
            public string LoadWeight { get; set; }
            public string LoadDimensionX { get; set; }
            public string LoadDimensionY { get; set; }
            public string FetchHeight { get; set; }
            public string DeliverHeight { get; set; }

            public string FetchAddress { get; set; }

            public string DeliverAddress { get; set; }

            public string AGV { get; set; }
            public string FetchConstraint { get; set; }

            public int FetchSequence { get; set; }
            public object LoadType { get; internal set; }
        }

        public class IO_dataC
        {
            public string Output { get; set; }
            public string Value { get; set; }

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





        public static void InsertIntoSql(string CmdText, List<SqlParameter> PrmtrList, string MsgDesc)
        {
            using (SqlConnection con = new SqlConnection(MainWindow.Connect_string))
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

        private TransportOrderDefinition NewTO(string fetch, string deliver, int delay)
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
                DeliverAddress = deliver,
                AGV = AGVID.Text,
                LoadType = pallet.Text
            };

            var to = new TransportOrderDefinition
            {
                Transport_order_id = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(),
                Transport_unit_type = pallet.Text,
                Start_time = null,
                End_time = DateTimeOffset.UtcNow.AddMinutes(delay),
                Custom_data = custom,
                Steps = new TransportOrderStep[] { stp0, stp1 },
                Partial_steps = false,
            };

            string CmdText;
            SqlParameter SqlPrmtr;
            List<SqlParameter> PrmtrList;



            CmdText = " INSERT INTO orderbuffer" +

                "([transport_order_id],[fetch_address],[fetch_height] ,[deliver_address],[deliver_height],[LoadDimensionX],[LoadDimensionY],[LoadWeight],[CreatedAt],[Sent],[pallet_type]) " +

                "VALUES (@transport_order_id,@fetch_address,@fetch_height, @deliver_address,@deliver_height,@LoadDimensionX,@LoadDimensionY,@LoadWeight,@CreatedAt,@Sent,@pallet_type)";

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

            SqlPrmtr = new SqlParameter("@pallet_type", SqlDbType.VarChar);
            SqlPrmtr.Value = pallet.Text;
            PrmtrList.Add(SqlPrmtr);

            InsertIntoSql(CmdText, PrmtrList, "new order added");


            return to;
        }

        private TransportOrderDefinition updateTO(string fetch, string deliver, string toid)
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
                DeliverAddress = deliver,
                AGV = AGVID.Text,
            };


            var to = new TransportOrderDefinition
            {
                Transport_order_id = toid,
                Transport_unit_type = pallet.Text,
                Start_time = null,
                End_time = DateTimeOffset.UtcNow.AddMinutes(10),
                Custom_data = custom,
                Steps = new TransportOrderStep[] { stp0, stp1 },
                Partial_steps = false,
            };



            string connectionString = Connect_string;

            try
            {
                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {
                    connection2.Open();

                    //status_db.Content = "Data updated to db";
                    SqlCommand cmd = new SqlCommand("UPDATE orderbuffer SET   fetch_address= @fetch, CurentStatus = @status ,deliver_address = @deliver Where  transport_order_id = @toid ", connection2);

                    cmd.Parameters.AddWithValue("@fetch", fetch);
                    cmd.Parameters.AddWithValue("@status", "Updated");
                    cmd.Parameters.AddWithValue("@toid", toid);
                    cmd.Parameters.AddWithValue("@deliver", deliver);
                    // cmd.Parameters.AddWithValue("@dheight", toid);
                    // cmd.Parameters.AddWithValue("@fheight", FetchHeight);

                    cmd.ExecuteNonQuery();

                    Dispatcher.Invoke(new InvokeDelegate(() =>
                    {
                        status_db.Content = "Data updated to db";
                        status_db.Background = Brushes.LimeGreen;
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


            return to;
        }

        public void getinfo()
        {
            LoadWeight = Weight.Text;
            LoadDimensionX = DimX.Text;
            LoadDimensionY = DimY.Text;
            FetchHeight = Fetch_Height.Text;
            DeliverHeight = Deliver_Height.Text;
            palett = pallet.Text;
            AGV = AGVID.Text;


        }

        private TransportOrderDefinition createTO_const(string fetch, string deliver, int delay, string cid, int numid, string Area, string v)
        {
            string cid_drop = cid + "drop";
            int numid_drop = numid;

            if (v == "0" || v == "2")
            {
                cid = "";
                numid = 0;
            }


            TransportOrderStep stp0 = new TransportOrderStep
            {
                Operation_type = "Pick",
                Addresses = new string[] { fetch },
                Constraint_group_id = cid,
                Constraint_group_index = numid
            };

            if (v == "0" || v == "1")
            {
                cid_drop = "";
                numid_drop = 0;
            }

            TransportOrderStep stp1 = new TransportOrderStep
            {
                Operation_type = "Drop",
                Addresses = new string[] { deliver },
                Constraint_group_id = cid_drop,
                Constraint_group_index = numid_drop

            };



            custom_dataC custom = new custom_dataC
            {
                LoadWeight = LoadWeight,
                LoadDimensionX = LoadDimensionX,
                LoadDimensionY = LoadDimensionY,
                FetchHeight = FetchHeight,
                DeliverHeight = DeliverHeight,
                FetchAddress = fetch,
                DeliverAddress = deliver,
                AGV = AGV,
               // LoadType = pallet.Text,
                FetchConstraint = Area,
                FetchSequence = numid

            };


            var to = new TransportOrderDefinition
            {
                Transport_order_id = DateTime.Now.Subtract(new DateTime(2024, 1, 1)).TotalMilliseconds.ToString(),
                Transport_unit_type = palett,
                Start_time = null,
                End_time = DateTimeOffset.UtcNow.AddMinutes(delay),
                Custom_data = custom,
                Steps = new TransportOrderStep[] { stp0, stp1 },
                Partial_steps = false,
            };



            string connectionString = Connect_string;

            string CmdText;
            SqlParameter SqlPrmtr;
            List<SqlParameter> PrmtrList;



            CmdText = " INSERT INTO orderbuffer" +

                "([transport_order_id],[fetch_address],[fetch_height] ,[deliver_address],[deliver_height],[LoadDimensionX],[LoadDimensionY],[LoadWeight],[CreatedAt],[Sent],[pallet_type]) " +

                "VALUES (@transport_order_id,@fetch_address,@fetch_height, @deliver_address,@deliver_height,@LoadDimensionX,@LoadDimensionY,@LoadWeight,@CreatedAt,@Sent,@pallet_type)";

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

            SqlPrmtr = new SqlParameter("@pallet_type", SqlDbType.VarChar);
            SqlPrmtr.Value = palett;
            PrmtrList.Add(SqlPrmtr);

            InsertIntoSql(CmdText, PrmtrList, "new order added");



            return to;
        }

        public static TransportOrderDefinition NewcarwahTO()
        {

            TransportOrderStep stp1 = new TransportOrderStep
            {
                Operation_type = "Drop",
                Addresses = new string[] { carwash }
            };


            custom_dataC custom = new custom_dataC
            {
                LoadWeight = "0",
                LoadDimensionX = "0",
                LoadDimensionY = "0",
                FetchHeight = "0",
                DeliverHeight = "0",
                FetchAddress = "carwash",
                DeliverAddress = carwash,
            };

            var to = new TransportOrderDefinition
            {
                Transport_order_id = DateTime.Now.Subtract(new DateTime(2024, 1, 1)).Seconds.ToString(),
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



            CmdText = " INSERT INTO orderbuffer" +

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

        private TransportOrderDefinition createTO4_const(string fetch, string deliver, string fetch2, string deliver2, int delay, string cid, int numid, string Area)
        {

            TransportOrderStep stp1 = new TransportOrderStep
            {
                Operation_type = "Drop",
                Addresses = new string[] { deliver }

            };

            TransportOrderStep stp2 = new TransportOrderStep
            {
                Operation_type = "Pick",
                Addresses = new string[] { fetch2 }

            };
            TransportOrderStep stp3 = new TransportOrderStep
            {
                Operation_type = "Drop",
                Addresses = new string[] { deliver2 }

            };
            TransportOrderStep stp0 = new TransportOrderStep
            {
                Operation_type = "Pick",
                Addresses = new string[] { fetch },

            };


            if (numid > 0)
            {

                if (cfetch)
                {
                    stp0.Constraint_group_id = cid;
                    stp0.Constraint_group_index = numid;
                }
                if (cdeliver)
                {
                    stp1.Constraint_group_id = cid;
                    stp1.Constraint_group_index = numid + 1;
                }


            }




            custom_dataC custom = new custom_dataC
            {
                LoadWeight = "0",
                LoadDimensionX = "0",
                LoadDimensionY = "0",
                FetchHeight = "0",
                DeliverHeight = "0",
                FetchAddress = fetch,
                DeliverAddress = deliver,
                AGV = "0",
                FetchConstraint = Area,
                FetchSequence = numid
            };


            var to = new TransportOrderDefinition
            {
                Transport_order_id = DateTime.Now.Subtract(new DateTime(2024, 1, 1)).TotalSeconds.ToString(),
                Transport_unit_type = "Palett",
                Start_time = null,
                End_time = DateTimeOffset.UtcNow.AddMinutes(delay),
                Custom_data = custom,
                Steps = new TransportOrderStep[] { stp0, stp1, stp2, stp3 },
                Partial_steps = false,
            };



            string connectionString = Connect_string;

            string CmdText;
            SqlParameter SqlPrmtr;
            List<SqlParameter> PrmtrList;



            CmdText = " INSERT INTO orderbuffer" +

                "([transport_order_id],[fetch_address],[fetch_height] ,[deliver_address],[deliver_height],[LoadDimensionX],[LoadDimensionY],[LoadWeight],[CreatedAt],[Sent],[pallet_type]) " +

                "VALUES (@transport_order_id,@fetch_address,@fetch_height, @deliver_address,@deliver_height,@LoadDimensionX,@LoadDimensionY,@LoadWeight,@CreatedAt,@Sent,@pallet_type)";

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

            SqlPrmtr = new SqlParameter("@pallet_type", SqlDbType.VarChar);
            SqlPrmtr.Value = "Palett";
            PrmtrList.Add(SqlPrmtr);

            InsertIntoSql(CmdText, PrmtrList, "new order added");



            return to;
        }




        private UtilityOrderDefinition NewUtilityTO()
        {



            var to = new UtilityOrderDefinition
            {
                Utility_order_id = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(),
                Order_type = "io",
                Start_time = null,
                End_time = DateTimeOffset.UtcNow.AddMinutes(10),


                Partial_steps = false,
            };
            return to;
        }

        private UtilityOrderDefinition IOsetTO(string io, string state)
        {
            IO_dataC custom = new IO_dataC
            {
                Output = io,
                Value = state,
            };

            var to = new UtilityOrderDefinition
            {
                Utility_order_id = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(),
                Order_type = "output",
                Start_time = null,
                End_time = DateTimeOffset.UtcNow.AddMinutes(10),
                Custom_data = custom,

                Partial_steps = false,
            };
            return to;
        }

        public delegate void InvokeDelegate();
        public void getack()
        {
            HttpClient client2 = new HttpClient();
            var sys = new Client(client2);
            sys.BaseUrl = "http://" + IP_host + "/api/v1/";
            while (true)
            {

                System.Threading.Thread.Sleep(2000);
                sys.BaseUrl = "http://" + IP_host + "/api/v1/";

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
                                item.Payload.AdditionalProperties.TryGetValue("error_name", out object error);
                                item.Payload.AdditionalProperties.TryGetValue("end_status", out object status_op);
                                Console.WriteLine(" validation adress: {0},transport_order_id : {1} , step {2} ", value, value2, value3);
                                string er;
                                if (error == null)
                                    er = "";
                                else er = error.ToString();


                                Dispatcher.Invoke(new InvokeDelegate(() =>
                                {
                                    this.log2.Items.Add(" - t_id : " + value2.ToString() + " waiting ack for adresse: " + value.ToString() + ", step: " + value3.ToString() + "  ,status: " + status_op.ToString() + "  ,error: " + er);

                                }));

                                //MainWindow.updatedb(To_id, DateTimeOffset.Parse(start_time), status, int.Parse(Ev_id));
                                // MainWindow.updatedb(value2.ToString(), value4.ToString(), "AGV waiting fetched ack", int.Parse(Ev_id));
                                if (autoack == true)
                                {
                                    sys.ContinueAsync(value2.ToString(), item.Event_id);
                                    Console.WriteLine(" ack operation envoyé ");
                                    Dispatcher.Invoke(new InvokeDelegate(() =>
                                    {
                                        this.log2.Items.Add(" * t_id : " + value2.ToString() + " ack sent for adresse: " + value.ToString() + ", step: " + value3.ToString());
                                    }));
                                }
                                SqlCommand command = new SqlCommand();

                                string CmdText;
                                SqlParameter SqlPrmtr;
                                List<SqlParameter> PrmtrList;

                                CmdText = " INSERT INTO ackevent" +

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

                                    if (status_op.ToString() == "Success")
                                    {
                                        status = "Pallet fetched";
                                        updatedb(value2.ToString(), DateTimeOffset.Parse(value4.ToString()), status, item.Event_id);
                                    }
                                    else
                                    {
                                        status = status_op.ToString();
                                    }

                                    updatedb_error(value2.ToString(), DateTimeOffset.Parse(value4.ToString()), status, error.ToString(), item.Event_id);
                                }
                                else
                                {
                                    if (status_op.ToString() == "Success")
                                    {
                                        status = "Pallet delivered";
                                        updateendoforder(value2.ToString(), item.Created_at, status, item.Event_id);
                                    }


                                    else
                                    {
                                        status = status_op.ToString();
                                        updateendoforder_error(value2.ToString(), item.Created_at, status, error.ToString(), item.Event_id);
                                    }



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
                                    this.log2.Items.Add(" * t_id : " + value2.ToString() + " waiting preload ack : adress: " + value.ToString() + ", step: " + value3.ToString());
                                }));
                                if (autoack == true)
                                {
                                    sys.ContinueAsync(value2.ToString(), item.Event_id);
                                    Console.WriteLine(" ack arrivé envoyé ");
                                    Dispatcher.Invoke(new InvokeDelegate(() =>
                                    {
                                        this.log2.Items.Add(" - t_id : " + value2.ToString() + " preload ack sent : adress: " + value.ToString() + " , step: " + value3.ToString());
                                    //  this.log.Items[this.log.Items.Count - 1].Background = Brushes.LimeGreen
                                }));
                                }
                                string CmdText;
                                SqlParameter SqlPrmtr;
                                List<SqlParameter> PrmtrList;



                                CmdText = "INSERT INTO ackevent" +

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

                                DateTimeOffset crea = DateTimeOffset.Parse(value4.ToString()).AddHours(2);
                                updatedb(value2.ToString(), crea, status, item.Event_id);
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
                                {
                                    Console.WriteLine(" Problem with carwash  ");

                                }

                                //refresh_orderDG();

                            }
                            if (item.Type == "ParameterUpdateEvent")
                            {
                                Console.WriteLine("event update");
                                item.Payload.AdditionalProperties.TryGetValue("parameter_name", out object name);
                                item.Payload.AdditionalProperties.TryGetValue("transport_order_id", out object To_id);
                                item.Payload.AdditionalProperties.TryGetValue("parameter_value", out object value);
                                //   item.Payload.AdditionalProperties.TryGetValue("drive_start_time", out object value4);




                                ParameterRequestAnswer stp1 = new ParameterRequestAnswer
                                {
                                    Event_id = item.Event_id,
                                    Parameter_name = name.ToString(),
                                    Parameter_value = value.ToString()
                                };

                                HttpClient client = new HttpClient();

                                var syspar = new ParameterClient(client);

                                syspar.BaseUrl = "http://" + IP_host + "/api/v1/";
                                syspar.ParAsync(To_id.ToString(), stp1);




                            }
                        }


                    }
                }
                catch (System.InvalidOperationException ex)
                {
                    Console.WriteLine(ex);

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


            while (true)
            {

                System.Threading.Thread.Sleep(2000);
                sys.BaseUrl = "http://" + IP_host + "/api/v1/";

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
                            {
                                ev_id = item.Event_id;



                            }
                            //  Console.WriteLine(item.Created_at);
                            //  Console.WriteLine(item.Type);
                            if (item.Type == "SystemStartupEvent")
                            {
                                ev_id = item.Event_id;
                                {
                                    Dispatcher.Invoke(new InvokeDelegate(() =>
                                    {
                                        this.log2.Items.Add(item.Type + ", id: " + item.Event_id + "  date: " + item.Created_at);
                                        this.log2.SelectedItem = this.log2.Items.Count;
                                        //  affich.Text = log.Items.Count.ToString();
                                    }));

                                }
                            }

                            else if (item.Type == "TransportOrderEndEvent")
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
                                    this.log2.Items.Add(" -- end of order id: " + value2.ToString() + ", reason: " + value3.ToString());
                                    this.log2.SelectedItem = this.log2.Items.Count;
                                    updateendoforder(value2.ToString(), DateTimeOffset.Parse(item.Created_at.ToString()), value3.ToString(), item.Event_id);
                                    //affich.Text = log.Items.Count.ToString();
                                }));
                            }
                            else
                            {

                                Dispatcher.Invoke(new InvokeDelegate(() =>
                                {
                                    this.log2.Items.Add(item.Type + ", id: " + item.Event_id + "  date: " + item.Created_at);
                                    this.log2.SelectedItem = this.log2.Items.Count;
                                    //  affich.Text = log.Items.Count.ToString();
                                }));
                            }
                        }
                        insert_agv();
                        //refresh_orderDG();


                    }
                    catch (HttpRequestException)
                    {
                        // MessageBox.Show("fleet controller not running or connection to " + sys.BaseUrl.ToString() + " ", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                        // Console.WriteLine(" Problem with event thread : {0}", ex.Message.ToString());
                        Dispatcher.Invoke(new InvokeDelegate(() =>
                        {
                            status.Content = "Server : " + "no connection" + "  |  system name : " + "no system | error ip ";
                            this.log2.Items.Add(" Problem with event thread : {0} " + " no connection IP " + sys.BaseUrl.ToString());
                            status.Background = Brushes.OrangeRed;
                        }));
                    }

                    catch (FormatException ex)
                    {
                        Console.WriteLine(" Problem with event thread : {0}", ex.Message.ToString());
                        Dispatcher.Invoke(new InvokeDelegate(() =>
                        {
                            status.Content = "Server : " + "no connection" + "  |  system name : " + "no system | error ip ";
                            this.log2.Items.Add(" Problem with event thread : {0} " + ex.Message.ToString());
                            status.Background = Brushes.OrangeRed;
                        }));

                    }
                    catch
                    {

                        Dispatcher.Invoke(new InvokeDelegate(() =>
                        {
                            status.Content = "Server : " + "no connection" + "  |  system name : " + "no system | error ip ";
                            this.log2.Items.Add(" Problem with event thread ");
                            status.Background = Brushes.OrangeRed;
                        }));
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
            //  svisu.BaseUrl = "http://" + IP_visu + "/api/v1/";
            if (usew == true)
                svisu.BaseUrl = "http://" + WIP_visu + "/api/v1/";
            else
                svisu.BaseUrl = "http://" + IP_visu + "/api/v1/";
            try
            {



                var x3 = svisu.AgvsAllAsync().GetAwaiter().GetResult();

                foreach (visu.AgvState agvState in x3)
                {
                    // Console.WriteLine("{0} loaded {1} ", agvState.Id, agvState.Loaded1);

                    try
                    {
                        using (SqlConnection connection2 = new SqlConnection(connectionString))
                        {
                            connection2.Open();
                            SqlCommand cmd2 = new SqlCommand(" DELETE FROM agvs WHERE agv_id = " + agvState.Id + " ", connection2);
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
                    catch (HttpRequestException)
                    {
                        // MessageBox.Show("fleet controller not running or connection to " + svisu.BaseUrl.ToString() + " ", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }



                    CmdText = "INSERT INTO agvs" +

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


                    InsertIntoSql(CmdText, PrmtrList, "AGV added");


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
                            updateorder(orderstate.Id, DateTimeOffset.Parse(orderstate.Estimated_finish_time.ToString()), curagv2, 0, "");

                    }
                }

                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {
                    connection2.Open();
                    SqlCommand cmd = new SqlCommand("SELECT * FROM dbo.agvs order by agv_id", connection2);
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

            }
            catch (HttpRequestException)
            {
                // MessageBox.Show("fleet controller not running or connection to " + sys.BaseUrl.ToString() + " ", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Console.WriteLine(" Problem with event thread : {0}", ex.Message.ToString());
                Dispatcher.Invoke(new InvokeDelegate(() =>
                {
                    status.Content = "Server : " + "no connection" + "  |  system name : " + "no system | error ip ";
                    this.log2.Items.Add(" Problem with event thread : {0} " + " no connection IP " + svisu.BaseUrl.ToString());
                    status.Background = Brushes.OrangeRed;
                }));
            }
            catch (SqlException ex)
            {
                Console.WriteLine("ERREUR DE CONNEXION ...");
                Console.WriteLine(ex.Message.ToString());
                // WriteLog("ERREUR DE CONNEXION ... " + ex.Message);

                throw ex;
            }



            // sda.SelectCommand = cmd;




        }



        public void refresh_orderDG()
        {
            string connectionString = Connect_string;
            DataSet ds = new DataSet();


            //    DataSet ds2 = new DataSet();
            try
            {


                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {

                    try
                    {
                        connection2.Open();
                        string str = "SELECT  [id] ,[transport_order_id] ,[fetch_address] ,[fetch_height] as F_Height ,[deliver_address] ,[deliver_height] as D_Height,+" +
                            "[LoadDimensionX] as DimX,[LoadDimensionY] as DimY,[LoadWeight] as Lweight,[CreatedAt] AT TIME ZONE 'Central Europe Standard Time' AS[CreatedAt] ,[StartedAt] " +
                            "AT TIME ZONE 'Central Europe Standard Time' AS[StartedAt] ,[FinishedAt] AT TIME ZONE 'Central Europe Standard Time' AS[FinishedAt]  ,[CurentStatus] " +
                            " ,[Sent],ack_event ,agv,pallet_type, error,customfields  FROM orderbuffer WHERE CreatedAt>getdate()-1 order by CreatedAt desc ";



                        SqlCommand cmd = new SqlCommand(str, connection2);

                        SqlDataAdapter sda = new SqlDataAdapter(cmd);

                        // sda.SelectCommand = cmd;
                        sda.Fill(ds);
                        //str = "SELECT  * FROM IO ";
                        // cmd = new SqlCommand(str, connection2);
                        // SqlDataAdapter sda2 = new SqlDataAdapter(cmd);
                        // sda2.Fill(ds2);


                        cmd = new SqlCommand("SELECT * FROM dbo.agvs order by agv_id", connection2);
                        SqlDataAdapter sda2 = new SqlDataAdapter(cmd);
                        DataSet ds2 = new DataSet();
                        sda2.Fill(ds2);




                        //  this.order_list.Resources["columnForeground"] = Brushes.Red;








                        Dispatcher.Invoke(new InvokeDelegate(() =>
                        {
                            status_db.Content = "Data refresh to db";
                            status_db.Background = (Brush)new BrushConverter().ConvertFromString("#FFFFB25A");
                            order_list.ItemsSource = ds.Tables[0].DefaultView;

                            agv.ItemsSource = ds2.Tables[0].DefaultView;
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

        public void trefresh_orderDG()
        {
            string connectionString = Connect_string;

            while (true)
            {
                Thread.Sleep(5000);

                //    DataSet ds2 = new DataSet();
                try
                {


                    using (SqlConnection connection2 = new SqlConnection(connectionString))
                    {

                        try
                        {
                            connection2.Open();
                            string str = "SELECT  [id] ,[transport_order_id] ,[fetch_address] ,[fetch_height] as F_Height ,[deliver_address] ,[deliver_height] as D_Height,+" +
                                "[LoadDimensionX] as DimX,[LoadDimensionY] as DimY,[LoadWeight] as Lweight,[CreatedAt] AT TIME ZONE 'Central Europe Standard Time' AS[CreatedAt] ,[StartedAt] " +
                                "AT TIME ZONE 'Central Europe Standard Time' AS[StartedAt] ,[FinishedAt] AT TIME ZONE 'Central Europe Standard Time' AS[FinishedAt]  ,[CurentStatus] " +
                                " ,[Sent],ack_event ,agv,pallet_type, error, customfields FROM orderbuffer WHERE CreatedAt>getdate()-1 order by CreatedAt desc ";


                            DataSet ds = new DataSet();
                            SqlCommand cmd = new SqlCommand(str, connection2);

                            SqlDataAdapter sda = new SqlDataAdapter(cmd);

                            // sda.SelectCommand = cmd;
                            sda.Fill(ds);
                            //str = "SELECT  * FROM IO ";
                            // cmd = new SqlCommand(str, connection2);
                            // SqlDataAdapter sda2 = new SqlDataAdapter(cmd);
                            // sda2.Fill(ds2);


                            cmd = new SqlCommand("SELECT * FROM dbo.agvs order by agv_id ", connection2);
                            SqlDataAdapter sda2 = new SqlDataAdapter(cmd);
                            DataSet ds2 = new DataSet();
                            sda2.Fill(ds2);




                            //  this.order_list.Resources["columnForeground"] = Brushes.Red;








                            Dispatcher.Invoke(new InvokeDelegate(() =>
                            {
                                status_db.Content = "Data refresh to db";
                                status_db.Background = (Brush)new BrushConverter().ConvertFromString("#FFFFB25A");
                                order_list.ItemsSource = ds.Tables[0].DefaultView;

                                agv.ItemsSource = ds2.Tables[0].DefaultView;
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

        }

        public void refresh_ioDG()
        {
            string connectionString = Connect_string;
            DataSet ds = new DataSet();
            DataSet ds2 = new DataSet();
            try
            {


                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {

                    try
                    {
                        //connection2.Open();
                        string str = "SELECT  [id] ,[transport_order_id] ,[fetch_address] ,[fetch_height] ,[deliver_address] ,[deliver_height],+" +
                            "[LoadDimensionX],[LoadDimensionY],[LoadWeight],[CreatedAt] AT TIME ZONE 'Central Europe Standard Time' AS[CreatedAt] ,[StartedAt] " +
                            "AT TIME ZONE 'Central Europe Standard Time' AS[StartedAt] ,[FinishedAt] AT TIME ZONE 'Central Europe Standard Time' AS[FinishedAt]  ,[CurentStatus] " +
                            " ,[Sent] ,agv,pallet_type, error FROM orderbuffer WHERE CreatedAt>getdate()-1 order by CreatedAt desc ";



                        SqlCommand cmd = new SqlCommand(str, connection2);

                        //SqlDataAdapter sda = new SqlDataAdapter(cmd);

                        //// sda.SelectCommand = cmd;
                        //sda.Fill(ds);
                        str = "SELECT  * FROM IO ";
                        cmd = new SqlCommand(str, connection2);
                        SqlDataAdapter sda2 = new SqlDataAdapter(cmd);
                        sda2.Fill(ds2);

                        Dispatcher.Invoke(new InvokeDelegate(() =>
                        {
                            // status_db.Content = "Data refresh to db";
                            //  status_db.Background = (Brush)new BrushConverter().ConvertFromString("#FFFFB25A");
                            IO.ItemsSource = ds2.Tables[0].DefaultView;

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


        public static void updatedb_error(string toid, DateTimeOffset start, string curentstatus, string error, int evid)
        {
            string connectionString = Connect_string;

            try
            {
                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {
                    connection2.Open();
                    if (evid == 0)
                    {
                        SqlCommand cmd = new SqlCommand("UPDATE orderbuffer SET  StartedAt = @start, CurentStatus = @status ,error = @error Where  transport_order_id = @toid ", connection2);

                        cmd.Parameters.AddWithValue("@start", start);
                        cmd.Parameters.AddWithValue("@status", curentstatus);
                        cmd.Parameters.AddWithValue("@toid", toid);
                        cmd.Parameters.AddWithValue("@error", error);


                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        SqlCommand cmd = new SqlCommand("UPDATE orderbuffer SET  StartedAt = @start, CurentStatus = @status ,error = @error,ack_event = @evid Where  transport_order_id = @toid ", connection2);

                        cmd.Parameters.AddWithValue("@start", start);
                        cmd.Parameters.AddWithValue("@status", curentstatus);
                        cmd.Parameters.AddWithValue("@toid", toid);
                        cmd.Parameters.AddWithValue("@error", error);
                        cmd.Parameters.AddWithValue("@evid", evid);

                        cmd.ExecuteNonQuery();
                    }


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

        public static void updatedb(string toid, DateTimeOffset start, string curentstatus, int evid)
        {
            string connectionString = Connect_string;

            try
            {
                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {
                    connection2.Open();
                    if (evid == 0)
                    {
                        SqlCommand cmd = new SqlCommand(" UPDATE orderbuffer SET  StartedAt = @start, CurentStatus = @status Where  transport_order_id = @toid ", connection2);

                        cmd.Parameters.AddWithValue("@start", start);
                        cmd.Parameters.AddWithValue("@status", curentstatus);
                        cmd.Parameters.AddWithValue("@toid", toid);


                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        SqlCommand cmd = new SqlCommand(" UPDATE orderbuffer SET  StartedAt = @start, CurentStatus = @status,ack_event = @evid Where  transport_order_id = @toid ", connection2);

                        cmd.Parameters.AddWithValue("@start", start);
                        cmd.Parameters.AddWithValue("@status", curentstatus);
                        cmd.Parameters.AddWithValue("@toid", toid);
                        cmd.Parameters.AddWithValue("@evid", evid);

                        cmd.ExecuteNonQuery();
                    }


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

        public static void updatedback(string toid, string curentstatus, int evid)
        {
            string connectionString = Connect_string;

            try
            {
                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {
                    connection2.Open();
                    if (evid == 0)
                    {
                        SqlCommand cmd = new SqlCommand(" UPDATE orderbuffer SET   CurentStatus = @status Where  transport_order_id = @toid ", connection2);


                        cmd.Parameters.AddWithValue("@status", curentstatus);
                        cmd.Parameters.AddWithValue("@toid", toid);


                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        SqlCommand cmd = new SqlCommand(" UPDATE orderbuffer SET   CurentStatus = @status,ack_event = @evid Where  transport_order_id = @toid ", connection2);


                        cmd.Parameters.AddWithValue("@status", curentstatus);
                        cmd.Parameters.AddWithValue("@toid", toid);
                        cmd.Parameters.AddWithValue("@evid", evid);

                        cmd.ExecuteNonQuery();
                    }


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

        public static void updateorder(string toid, DateTimeOffset start, int agv, int evid, string csutom)
        {
            string connectionString = Connect_string;

            try
            {
                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {
                    connection2.Open();

                    //status_db.Content = "Data updated to db";
                    if (evid == 0)
                    {
                        SqlCommand cmd = new SqlCommand(" UPDATE orderbuffer SET  FinishedAt = @start, agv = @agv , customfields = @customfields Where  transport_order_id = @toid ", connection2);

                        cmd.Parameters.AddWithValue("@start", start);
                        cmd.Parameters.AddWithValue("@agv", agv);
                        cmd.Parameters.AddWithValue("@toid", toid);
                        cmd.Parameters.AddWithValue("@customfields", csutom);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        SqlCommand cmd = new SqlCommand(" UPDATE orderbuffer SET  FinishedAt = @start, agv = @agv,ack_event = @evid, customfields = @customfields  Where  transport_order_id = @toid ", connection2);

                        cmd.Parameters.AddWithValue("@start", start);
                        cmd.Parameters.AddWithValue("@agv", agv);
                        cmd.Parameters.AddWithValue("@toid", toid);
                        cmd.Parameters.AddWithValue("@evid", evid);
                        cmd.Parameters.AddWithValue("@customfields", csutom);
                        cmd.ExecuteNonQuery();

                    }

                    //Dispatcher.Invoke(new InvokeDelegate(() =>
                    //{
                    //    status_db.Content = "Data updated to db";
                    //    status_db.Background = Brushes.LimeGreen;
                    //}));

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




        public static void updateendoforder(string toid, DateTimeOffset end, string curentstatus, int evid)
        {
            string connectionString = Connect_string;

            try
            {
                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {
                    connection2.Open();
                    if (evid == 0)
                    {
                        SqlCommand cmd = new SqlCommand(" UPDATE orderbuffer SET  FinishedAt = @start, CurentStatus = @status Where  transport_order_id = @toid ", connection2);

                        cmd.Parameters.AddWithValue("@start", end);
                        cmd.Parameters.AddWithValue("@status", curentstatus);
                        cmd.Parameters.AddWithValue("@toid", toid);

                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        SqlCommand cmd = new SqlCommand(" UPDATE orderbuffer SET  FinishedAt = @start, CurentStatus = @status,ack_event = @evid Where  transport_order_id = @toid ", connection2);

                        cmd.Parameters.AddWithValue("@start", end);
                        cmd.Parameters.AddWithValue("@status", curentstatus);
                        cmd.Parameters.AddWithValue("@toid", toid);
                        cmd.Parameters.AddWithValue("@evid", evid);
                        cmd.ExecuteNonQuery();
                    }
                    //status_db.Content = "Data updated to db";



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

        public static void updateendoforder_error(string toid, DateTimeOffset end, string curentstatus, string error, int evid)
        {
            string connectionString = Connect_string;

            try
            {
                using (SqlConnection connection2 = new SqlConnection(connectionString))
                {
                    connection2.Open();

                    //status_db.Content = "Data updated to db";
                    SqlCommand cmd = new SqlCommand(" UPDATE orderbuffer SET  FinishedAt = @start, CurentStatus = @status, error = @error,ack_event = @evid Where  transport_order_id = @toid ", connection2);

                    cmd.Parameters.AddWithValue("@start", end);
                    cmd.Parameters.AddWithValue("@status", curentstatus);
                    cmd.Parameters.AddWithValue("@toid", toid);
                    cmd.Parameters.AddWithValue("@error", error);
                    cmd.Parameters.AddWithValue("@evid", evid);
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

        public class DBConnection
        {
            private SqlConnection conn;
            public DBConnection()
            {
                //constructor
            }
            ~DBConnection()
            {
                //destructor
                conn = null;
            }
            public void Disconnect()
            {
                conn.Close();
            }
            public string ConnectToDatabase()
            {
                try
                {
                    string loc = "AproRest.mdf";
                    string directory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().GetName().CodeBase) + "\\" + loc;
                    conn = new SqlConnection(@"Data Source=" + directory);
                    conn.Open();
                    return "Connected";
                }
                catch (SqlException e)
                {
                    conn = null;
                    return e.Message;
                }
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
                status.Background = Brushes.LimeGreen;

                if (status2.Result.Machine_name == null)
                {
                    status.Content = "Server : " + "no connection" + "  |  system name : " + "no system";
                    status.Background = Brushes.OrangeRed;

                }

            }
            catch
            {
                status.Content = "Server : " + "no connection" + "  |  system name : " + "no system | error ip ";
                status.Background = Brushes.OrangeRed;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            string loc = "Database1.mdf";
            string directory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().GetName().CodeBase) + "\\" + loc;

            // string DataSource = "(LocalDB)\MSSQLLocalDB AttachDbFilename = "+loc+"; Integrated Security = True; Connect Timeout = 30";
            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\Database1.mdf ";



            Connect_string = connectionString;
            update.IsEnabled = false;

            //  DBConn.ConnectToDatabase();


            //HttpClient client2 = new HttpClient();

            //var sys = new Client(client2);
            //sys.BaseUrl = "http://" + IP_host + "/api/v1/";
            //try
            //{
            //    var status2 = sys.SystemAsync();
            //    status.Content = "Server : " + status2.Result.Machine_name + "  |  system name : " + status2.Result.System_name +" IP" + IP_host;
            //    status.Background = Brushes.LimeGreen;
            //}
            //catch
            //{
            //    status.Content = "Server : " + "no connection" + "  |  system name : " + "no system";
            //    status.Background = Brushes.OrangeRed;
            //}

            using (SqlConnection connection2 = new SqlConnection(Connect_string))
            {

                try
                {
                    connection2.Open();

                    SqlCommand cmd2 = new SqlCommand("TRUNCATE TABLE agvs ", connection2);
                    var exist = cmd2.ExecuteNonQuery();
                    DataSet ds2 = new DataSet();
                    DataRow dRow;
                    string str = "SELECT  * FROM Settings ";
                    SqlCommand cmd = new SqlCommand(str, connection2);
                    SqlDataAdapter sda2 = new SqlDataAdapter(cmd);
                    sda2.Fill(ds2);
                    dRow = ds2.Tables[0].Rows[0];
                    Ip_host.Text = dRow.ItemArray.GetValue(1).ToString();
                    Ip_visu.Text = dRow.ItemArray.GetValue(2).ToString();
                    Ip_utility.Text = dRow.ItemArray.GetValue(3).ToString();

                    Ip_host_web.Text = dRow.ItemArray.GetValue(4).ToString();
                    Ip_visu_web.Text = dRow.ItemArray.GetValue(5).ToString();
                    Ip_utility_web.Text = dRow.ItemArray.GetValue(6).ToString();
                    CWloc.Text = dRow.ItemArray.GetValue(7).ToString();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Create Database");
                }
                finally
                {
                    connection2.Close();
                }
            }
            WIP_utility = Ip_utility_web.Text;
            WIP_host = Ip_host_web.Text;
            IP_utility = Ip_utility.Text;
            IP_host = Ip_host.Text;
            WIP_visu = Ip_visu_web.Text;
            IP_visu = Ip_visu.Text;
            webh.Content = "Webhook : " + "Not Listening on port 8001..." + "  ";
            webh.Background = Brushes.OrangeRed;
            status_db.Content = "DB created and connected";
            status_db.Background = Brushes.LimeGreen;
            status.Content = "Server : " + "no connection" + "  |  system name : " + "no system";
            status.Background = Brushes.OrangeRed;

            // refresh_orderDG();
            Thread a = new Thread(getack);
            a.IsBackground = true;
            a.Start();
            Thread b = new Thread(getevent);
            b.IsBackground = true;
            b.Start();
            HttpClient client2 = new HttpClient();
            var sys = new Client(client2);
            web = false;
            Thread c = new Thread(trefresh_orderDG);
            c.IsBackground = true;
            c.Start();

        }


        async void serverAsync(object ipweb)
        {
            try
            {


                var listener = new HttpListener();

                listener.Prefixes.Add(ipweb.ToString());

                listener.Start();


                Console.WriteLine("Listening on " + ipweb.ToString());

                while (true)
                {


                    Dispatcher.Invoke(new InvokeDelegate(() =>
                    {
                        webh.Content = "Webhook : " + "Listening on " + ipweb.ToString();
                        webh.Background = Brushes.LimeGreen;

                    }));
                    HttpListenerContext ctx = listener.GetContext();
                    HttpListenerRequest request = ctx.Request;
                    // Console.WriteLine($"Received request for {request.Url}");
                    string ua = request.Headers.Get("User-Agent");
                    Console.WriteLine($"{request.HttpMethod} {request.Url}");

                    var body = request.InputStream;
                    var encoding = request.ContentEncoding;
                    var reader = new StreamReader(body, encoding);
                    string s = "null";
                    bool ok = true;
                    if (request.HasEntityBody)
                    {


                        if (request.ContentType != null)
                        {
                            Console.WriteLine("Client data content type {0}", request.ContentType);
                        }
                        Console.WriteLine("Client data content length {0}", request.ContentLength64);

                        // Console.WriteLine("Start of data:");
                        s = reader.ReadToEnd();
                        //Console.WriteLine(s);
                        //Console.WriteLine("End of data:");
                        // var jsonTextReader = new Newtonsoft.Json.JsonTextReader(reader);

                        //Event Event2 = System.Text.Json.JsonSerializer.Deserialize<Event>(s);


                        var dyna = JsonConvert.DeserializeObject<dynamic>(s);
                        string type = dyna.type;
                        if (type == "stop")
                        {
                            var response2 = ctx.Response;
                            response2.StatusCode = (int)HttpStatusCode.OK;
                            response2.ContentType = "text/plain";
                            response2.OutputStream.Write(new byte[] { }, 0, 0);
                            response2.OutputStream.Close();
                            break;
                        }


                        if (forward_enable)
                        {

                            ok = await forward(s);
                        }
                        Thread a = new Thread(new ParameterizedThreadStart(LocalDB.webhook_receive));
                        a.IsBackground = true;
                        a.Start(s);

                        // LocalDB.webhook_receive(dyna);
                        if (type != "AgvState")

                        {
                            if (type != "OrderState")
                            {


                                WriteMessage("Webhooks: " + type.ToString() + " " + s, Brushes.Green, log2);
                            }
                        }

                        //Dispatcher.Invoke(new InvokeDelegate(() =>
                        //{

                        //    ListViewItem li = new ListViewItem();
                        //    li.Foreground = Brushes.Green;
                        //    li.Content = "Webhooks: " + type.ToString() + " " + s;
                        //    this.log2.Items.Add(li);
                        //    this.log2.SelectedItem = this.log2.Items.Count;
                        //    webh.Background = Brushes.LightGreen;


                        //}));
                        //  refresh_ioDG();
                        //refresh_orderDG();
                        reader.Close();
                        body.Close();

                    }
                    if (ok == false)
                    {

                        WriteMessage("REDIRECT:  TIMEOUT  " + s, Brushes.Green, bbview1);
                    }
                    else
                    {
                        var response = ctx.Response;

                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.ContentType = "text/plain";
                        response.OutputStream.Write(new byte[] { }, 0, 0);
                        response.OutputStream.Close();
                    }


                    //HttpListenerResponse resp = ctx.Response;
                    //resp.Headers.Set("Content-Type", "text/plain");

                    //string data = s ?? "unknown";
                    //byte[] buffer = Encoding.UTF8.GetBytes(data);
                    //resp.ContentLength64 = buffer.Length;

                    //Stream ros = resp.OutputStream;
                    //ros.Write(buffer, 0, buffer.Length);
                    //if (web == false)
                    //{
                    //    break;
                    //}
                }
                listener.Stop();
                Console.WriteLine("Stop Listening on port 8001...");
                Dispatcher.Invoke(new InvokeDelegate(() =>
                {
                    webh.Content = "Webhook : " + "Stop Listening on" + ipweb.ToString();
                    webh.Background = Brushes.OrangeRed;
                    webhook.IsEnabled = true;
                }));

            }
            catch (HttpListenerException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
            }

        }


        async void servervisuAsync()
        {
            try
            {


                var listener = new HttpListener();

                listener.Prefixes.Add(ip_host_webhooks);

                listener.Start();
                insert_agv();

                Console.WriteLine("Listening on " + ip_host_webhooks);

                while (true)
                {


                    Dispatcher.Invoke(new InvokeDelegate(() =>
                    {
                        webh.Content = "Webhook : " + "Listening on " + ip_host_webhooks;
                        webh.Background = Brushes.LimeGreen;

                    }));
                    HttpListenerContext ctx = listener.GetContext();
                    HttpListenerRequest request = ctx.Request;
                    // Console.WriteLine($"Received request for {request.Url}");
                    string ua = request.Headers.Get("User-Agent");
                    Console.WriteLine($"{request.HttpMethod} {request.Url}");

                    var body = request.InputStream;
                    var encoding = request.ContentEncoding;
                    var reader = new StreamReader(body, encoding);
                    string s = "null";
                    bool ok = true;
                    if (request.HasEntityBody)
                    {


                        if (request.ContentType != null)
                        {
                            Console.WriteLine("Client data content type {0}", request.ContentType);
                        }
                        Console.WriteLine("Client data content length {0}", request.ContentLength64);

                        // Console.WriteLine("Start of data:");
                        s = reader.ReadToEnd();
                        //Console.WriteLine(s);
                        //Console.WriteLine("End of data:");
                        // var jsonTextReader = new Newtonsoft.Json.JsonTextReader(reader);

                        //Event Event2 = System.Text.Json.JsonSerializer.Deserialize<Event>(s);


                        var dyna = JsonConvert.DeserializeObject<dynamic>(s);
                        string type = dyna.type;
                        if (type == "stop")
                        {
                            var response2 = ctx.Response;
                            response2.StatusCode = (int)HttpStatusCode.OK;
                            response2.ContentType = "text/plain";
                            response2.OutputStream.Write(new byte[] { }, 0, 0);
                            response2.OutputStream.Close();
                            break;
                        }


                        if (forward_enable)
                        {

                            ok = await forward(s);
                        }
                        Thread a = new Thread(new ParameterizedThreadStart(LocalDB.webhook_receive));
                        a.IsBackground = true;
                        a.Start(s);

                        // LocalDB.webhook_receive(dyna);
                        WriteMessage("Webhooks: " + type.ToString() + " " + s, Brushes.Green, log2);
                        //Dispatcher.Invoke(new InvokeDelegate(() =>
                        //{

                        //    ListViewItem li = new ListViewItem();
                        //    li.Foreground = Brushes.Green;
                        //    li.Content = "Webhooks: " + type.ToString() + " " + s;
                        //    this.log2.Items.Add(li);
                        //    this.log2.SelectedItem = this.log2.Items.Count;
                        //    webh.Background = Brushes.LightGreen;


                        //}));
                        //  refresh_ioDG();
                        // refresh_orderDG();
                        reader.Close();
                        body.Close();

                    }
                    if (ok == false)
                    {

                        WriteMessage("REDIRECT:  TIMEOUT  " + s, Brushes.Green, bbview1);
                    }
                    else
                    {
                        var response = ctx.Response;

                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.ContentType = "text/plain";
                        response.OutputStream.Write(new byte[] { }, 0, 0);
                        response.OutputStream.Close();
                    }


                    //HttpListenerResponse resp = ctx.Response;
                    //resp.Headers.Set("Content-Type", "text/plain");

                    //string data = s ?? "unknown";
                    //byte[] buffer = Encoding.UTF8.GetBytes(data);
                    //resp.ContentLength64 = buffer.Length;

                    //Stream ros = resp.OutputStream;
                    //ros.Write(buffer, 0, buffer.Length);
                    //if (web == false)
                    //{
                    //    break;
                    //}
                }
                listener.Stop();
                Console.WriteLine("Stop Listening on port 8001...");
                Dispatcher.Invoke(new InvokeDelegate(() =>
                {
                    webh.Content = "Webhook : " + "Stop Listening on" + ip_host_webhooks;
                    webh.Background = Brushes.OrangeRed;
                    webhook.IsEnabled = true;
                }));

            }
            catch (HttpListenerException ex)
            {
                MessageBox.Show(ex.Message);
            }

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

            if (usew == true)
                sys.BaseUrl = "http://" + WIP_host + "/api/v1/";
            else
                sys.BaseUrl = "http://" + IP_host + "/api/v1/";
            getinfo();

            var to = new TransportOrderDefinition();

            if (use_fetch)
            {
                
                area.Text = Fetch_address.Text;
            }

            try
            {
                if (useb == true || useb_drop == true)
                {
                    to = createTO_const(Fetch_address.Text, Deliver_address.Text, Int16.Parse(delay.Text), area.Text, Int16.Parse(sequence.Text), area.Text, ctype);
                    var x2 = sys.Orders2Async(to, to.Transport_order_id).GetAwaiter().GetResult();
                }
                else
                {
                    to = NewTO(Fetch_address.Text, Deliver_address.Text, Int16.Parse(delay.Text));
                    var x2 = sys.Orders2Async(to, to.Transport_order_id).GetAwaiter().GetResult();
                }



            }
            catch (ApiException ex)
            {
                Console.WriteLine("ERREUR ordre ...{0}", to.Transport_order_id);
                Console.WriteLine(ex.Message.ToString());

                updatedb_error(to.Transport_order_id, DateTime.Now, "Error", "bad location information please check", 0);


            }
            catch (HttpRequestException)
            {
                MessageBox.Show("fleet controller not running or connection to " + sys.BaseUrl.ToString() + " ", "error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (auto_increment)
            {
                int seq = Int16.Parse(sequence.Text);
                seq = seq + 1;
                sequence.Text = seq.ToString();
            }
            refresh_orderDG();


        }

        private void Start_Click4(object sender, RoutedEventArgs e)
        {
            HttpClient client2 = new HttpClient();
            var sys = new Client(client2);

            if (usew == true)
                sys.BaseUrl = "http://" + WIP_host + "/api/v1/";
            else
                sys.BaseUrl = "http://" + IP_host + "/api/v1/";

            var to = new TransportOrderDefinition();


            try
            {

                {
                    to = createTO4_const(Fetch_address1.Text, Deliver_address1.Text, Fetch_address2.Text, Deliver_address2.Text, 20, area.Text, Int16.Parse(sequence.Text), area.Text);
                    var x2 = sys.Orders2Async(to, to.Transport_order_id).GetAwaiter().GetResult();
                }




            }
            catch (ApiException ex)
            {
                Console.WriteLine("ERREUR ordre ...{0}", to.Transport_order_id);
                Console.WriteLine(ex.Message.ToString());

                updatedb_error(to.Transport_order_id, DateTime.Now, "Error", "bad location information please check", 0);


            }
            catch (HttpRequestException)
            {
                MessageBox.Show("fleet controller not running or connection to " + sys.BaseUrl.ToString() + " ", "error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //refresh_orderDG();


        }



        private void Start_Click3(object sender, RoutedEventArgs e)
        {
            HttpClient client2 = new HttpClient();
            var sys = new Client(client2);

            if (usew == true)
                sys.BaseUrl = "http://" + WIP_host + "/api/v1/";
            else
                sys.BaseUrl = "http://" + IP_host + "/api/v1/";

            var to = new TransportOrderDefinition();


            try
            {

                {
                    to = createTO3_const(Fetch_address3.Text, Deliver_address3.Text, WAIT_address3.Text, 20, area.Text, Int16.Parse(sequence.Text), area.Text,"0");
                    var x2 = sys.Orders2Async(to, to.Transport_order_id).GetAwaiter().GetResult();
                }




            }
            catch (ApiException ex)
            {
                Console.WriteLine("ERREUR ordre ...{0}", to.Transport_order_id);
                Console.WriteLine(ex.Message.ToString());

                updatedb_error(to.Transport_order_id, DateTime.Now, "Error", "bad location information please check", 0);


            }
            catch (HttpRequestException)
            {
                MessageBox.Show("fleet controller not running or connection to " + sys.BaseUrl.ToString() + " ", "error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    SqlCommand cmd2 = new SqlCommand("SELECT transport_order_id FROM orderbuffer WHERE id = " + ID + " ", connection2);
                    var exist = cmd2.ExecuteScalar();
                    connection2.Close();
                    HttpClient client2 = new HttpClient();
                    var sys = new Client(client2);
                    // sys.BaseUrl = "http://" + IP_host + "/api/v1/";
                    if (usew == true)
                        sys.BaseUrl = "http://" + WIP_host + "/api/v1/";
                    else
                        sys.BaseUrl = "http://" + IP_host + "/api/v1/";
                    var x = sys.Orders3Async(false, exist.ToString());

                }
                refresh_orderDG();
            }
            else
                MessageBox.Show("no row seleted");

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
                    SqlCommand cmd2 = new SqlCommand("DELETE FROM orderbuffer WHERE id = " + ID + " ", connection2);
                    var exist = cmd2.ExecuteNonQuery();
                    connection2.Close();


                }
                refresh_orderDG();
            }
            else
                MessageBox.Show("no row seleted");

        }

        private void IO_Click(object sender, RoutedEventArgs e)
        {

            if (IO.SelectedItem != null)
            {// insert_agv();
                string connectionString = Connect_string;

                DataRowView dataRowView = (DataRowView)IO.SelectedItem;
                string ID = (dataRowView.Row[1]).ToString();
                // MessageBox.Show(ID);
                HttpClient client2 = new HttpClient();
                var sys = new Client(client2);
                //sys.BaseUrl = "http://" + IP_utility + "/api/v1/";
                if (usew == true)
                    sys.BaseUrl = "http://" + WIP_utility + "/api/v1/";
                else
                    sys.BaseUrl = "http://" + IP_utility + "/api/v1/";

                string trimmed = String.Concat(ID.Where(c => !Char.IsWhiteSpace(c)));
                var to = new UtilityOrderDefinition();


                try
                {


                    to = IOsetTO(trimmed, "on");
                    var x2 = sys.OrdersUAsync(to, to.Utility_order_id).GetAwaiter().GetResult();

                }
                catch (ApiException ex)
                {
                    Console.WriteLine("ERREUR ordre ...{0}", to.Utility_order_id);
                    Console.WriteLine(ex.Message.ToString());

                    updatedb_error(to.Utility_order_id, DateTime.Now, "Error", "bad location information please check", 0);


                }
                catch (HttpRequestException)
                {
                    MessageBox.Show("fleet controller not running or connection to " + sys.BaseUrl.ToString() + " ", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
                MessageBox.Show("no row seleted");

        }

        private void IO_off_Click(object sender, RoutedEventArgs e)
        {

            if (IO.SelectedItem != null)
            {// insert_agv();
                string connectionString = Connect_string;

                DataRowView dataRowView = (DataRowView)IO.SelectedItem;
                string ID = (dataRowView.Row[1]).ToString();
                // MessageBox.Show(ID);
                HttpClient client2 = new HttpClient();
                var sys = new Client(client2);
                if (usew == true)
                    sys.BaseUrl = "http://" + WIP_utility + "/api/v1/";
                else
                    sys.BaseUrl = "http://" + IP_utility + "/api/v1/";

                string trimmed = String.Concat(ID.Where(c => !Char.IsWhiteSpace(c)));
                var to = new UtilityOrderDefinition();


                try
                {


                    to = IOsetTO(trimmed, "off");
                    var x2 = sys.OrdersUAsync(to, to.Utility_order_id).GetAwaiter().GetResult();

                }
                catch (ApiException ex)
                {
                    Console.WriteLine("ERREUR ordre ...{0}", to.Utility_order_id);
                    Console.WriteLine(ex.Message.ToString());

                    updatedb_error(to.Utility_order_id, DateTime.Now, "Error", "bad location information please check", 0);


                }
            }
            else
                MessageBox.Show("no row seleted");

        }

        private void DAck_Click(object sender, RoutedEventArgs e)
        {
            if (order_list.SelectedItem != null)
            {// insert_agv();
                string connectionString = Connect_string;

                DataRowView dataRowView = (DataRowView)order_list.SelectedItem;
                int ID = Convert.ToInt32(dataRowView.Row[14]);
                string trimmed = String.Concat((dataRowView.Row[1]).ToString().Where(c => !Char.IsWhiteSpace(c)));
                string status = dataRowView.Row[12].ToString();
                string nstatus;

                MessageBox.Show(ID + "  order  " + trimmed + " status " + status);

                //using (SqlConnection connection2 = new SqlConnection(connectionString))
                //{
                //    connection2.Open();
                //    SqlCommand cmd2 = new SqlCommand("DELETE FROM orderbuffer WHERE id = " + ID + " ", connection2);
                //    var exist = cmd2.ExecuteNonQuery();
                //    connection2.Close();
                HttpClient client2 = new HttpClient();
                var sys = new Client(client2);
                if (usew == true)
                    sys.BaseUrl = "http://" + WIP_host + "/api/v1/";
                else
                    sys.BaseUrl = "http://" + IP_host + "/api/v1/";
                // sys.BaseUrl = "http://" + MainWindow.IP_host + "/api/v1/";
                try
                {
                    sys.ContinueAsync(trimmed, ID);

                    switch (status)
                    {
                        case var s when status.Contains("AGV waiting ack to fetch"):
                            {
                                nstatus = "AGV ready to fetch";
                                break;
                            }
                        case var s when status.Contains("AGV waiting ack to deliver"):
                            {
                                nstatus = "AGV ready to deliver";
                                break;

                            }
                        case var s when status.Contains("AGV waiting fetched ack"):
                            {
                                nstatus = "Pallet fetched";
                                break;
                            }
                        case var s when status.Contains("AGV waiting delivered ack"):
                            {
                                nstatus = "Pallet delivered";
                                break;
                            }

                        default:
                            {
                                nstatus = "AGV realesed from error";
                                Console.WriteLine("default");
                                break;
                            }
                    }
                    MainWindow.updatedback(trimmed, nstatus, ID);
                }

                catch
                {
                    MessageBox.Show("error sending Ack");
                }

                //}
                refresh_orderDG();
            }
            else
                MessageBox.Show("no row seleted");

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
                SqlCommand cmd2 = new SqlCommand("TRUNCATE TABLE orderbuffer ", connection2);
                var exist = cmd2.ExecuteScalar();
                connection2.Close();

            }
            refresh_orderDG();
            log2.Items.Clear();
        }

        private void Csv_loazd_Click(object sender, RoutedEventArgs e)
        {
            csv_loazd.IsEnabled = false;
            getinfo();
            Thread a = new Thread(load_csv);
            a.IsBackground = true;
            a.Start();
            // load_csv();
            csv_loazd.IsEnabled = true;

        }

        private void Csv_loazd4_Click(object sender, RoutedEventArgs e)
        {
            if (constraint_fetch.IsChecked == true)
            { cfetch = true; }
            else
                cfetch = false;
            if (constraint_deliver.IsChecked == true)
            { cdeliver = true; }
            else
                cdeliver = false;


            csv_loazd4.IsEnabled = false;
            Thread a = new Thread(load_csv4);
            a.IsBackground = true;
            a.Start();
            csv_loazd4.IsEnabled = true;

        }

        // load CSV file for mutiple order 
        void load_csv()
        {
            try
            {
                var csvTable = new DataTable();
                HttpClient client2 = new HttpClient();
                var sys = new Client(client2);
                if (usew == true)
                    sys.BaseUrl = "http://" + WIP_host + "/api/v1/";
                else
                    sys.BaseUrl = "http://" + IP_host + "/api/v1/";
                // sys.BaseUrl = "http://" + IP_host + "/api/v1/";
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
                    using (var csvReader = new CsvReader(new StreamReader(System.IO.File.OpenRead(filename)), true, ';'))
                    {
                        csvTable.Load(csvReader);
                    }
                    string Column1 = csvTable.Columns[0].ToString();
                    string Row1 = csvTable.Rows[0][1].ToString();
                    string area = "";
                    string area_id = "";
                    foreach (DataRow dr in csvTable.Rows)
                    {
                        Console.WriteLine("{0}, {1}, {2}", dr[0].ToString(), dr[1].ToString(), dr[2].ToString());


                        try
                        {
                            if (dr[3].ToString() == "0")

                                to = NewTO(dr[0].ToString(), dr[1].ToString(), int.Parse(dr[2].ToString()));
                            else
                            {
                                if (area != dr[3].ToString())
                                {
                                    area = dr[3].ToString();
                                    area_id = area + DateTime.Now.Subtract(new DateTime(2024, 1, 1)).TotalMilliseconds.ToString();
                                }
                                to = createTO_const(dr[0].ToString(), dr[1].ToString(), int.Parse(dr[2].ToString()), area_id, int.Parse(dr[4].ToString()), dr[3].ToString(), dr[5].ToString());
                            }


                            var x2 = sys.Orders2Async(to, to.Transport_order_id).GetAwaiter().GetResult();
                            Thread.Sleep(1000);
                        }
                        catch (ApiException ex)
                        {
                            Console.WriteLine("ERREUR ordre ...{0}", to.Transport_order_id);
                            Console.WriteLine(ex.Message.ToString());

                            updatedb_error(to.Transport_order_id, DateTime.Now, "Error " + ex.StatusCode, "wrong parameter sent to host", 0);

                        }

                        // refresh_orderDG();
                        //   Console.WriteLine(Column1, Row1);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "load csv");

            }



        }

        void load_csv4()
        {
            try
            {
                var csvTable = new DataTable();
                HttpClient client2 = new HttpClient();
                var sys = new Client(client2);
                if (usew == true)
                    sys.BaseUrl = "http://" + WIP_host + "/api/v1/";
                else
                    sys.BaseUrl = "http://" + IP_host + "/api/v1/";
                // sys.BaseUrl = "http://" + IP_host + "/api/v1/";
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
                    using (var csvReader = new CsvReader(new StreamReader(System.IO.File.OpenRead(filename)), true, ';'))
                    {
                        csvTable.Load(csvReader);
                    }
                    string Column1 = csvTable.Columns[0].ToString();
                    string Row1 = csvTable.Rows[0][1].ToString();
                    string area = "";
                    string area_id = "";
                    foreach (DataRow dr in csvTable.Rows)
                    {
                        Console.WriteLine("{0}, {1}, {2}", dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString());


                        try
                        {

                            if (area != dr[5].ToString())
                            {
                                area = dr[5].ToString();
                                area_id = area + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString();
                            }
                            to = createTO4_const(dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), int.Parse(dr[4].ToString()), area_id, int.Parse(dr[6].ToString()), dr[5].ToString());



                            var x2 = sys.Orders2Async(to, to.Transport_order_id).GetAwaiter().GetResult();
                            Thread.Sleep(1000);

                        }
                        catch (ApiException ex)
                        {
                            Console.WriteLine("ERREUR ordre ...{0}", to.Transport_order_id);
                            Console.WriteLine(ex.Message.ToString());

                            updatedb_error(to.Transport_order_id, DateTime.Now, "Error " + ex.StatusCode, "wrong parameter sent to host", 0);

                        }

                        // refresh_orderDG();
                        Console.WriteLine(Column1, Row1);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "load csv");

            }



        }

        //void csvthread(string (filename))
        //{
        //    using (var csvReader = new CsvReader(new StreamReader(System.IO.File.OpenRead(filename)), true, ';'))
        //    {
        //        csvTable.Load(csvReader);
        //    }
        //    string Column1 = csvTable.Columns[0].ToString();
        //    string Row1 = csvTable.Rows[0][1].ToString();
        //    string area = "";
        //    string area_id = "";
        //    foreach (DataRow dr in csvTable.Rows)
        //    {
        //        Console.WriteLine("{0}, {1}, {2}", dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString());


        //        try
        //        {

        //            if (area != dr[5].ToString())
        //            {
        //                area = dr[5].ToString();
        //                area_id = area + DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalHours.ToString();
        //            }
        //            to = createTO4_const(dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), int.Parse(dr[4].ToString()), area_id, int.Parse(dr[6].ToString()), dr[5].ToString());



        //            var x2 = sys.Orders2Async(to, to.Transport_order_id).GetAwaiter().GetResult();

        //        }
        //        catch (ApiException ex)
        //        {
        //            Console.WriteLine("ERREUR ordre ...{0}", to.Transport_order_id);
        //            Console.WriteLine(ex.Message.ToString());

        //            updatedb_error(to.Transport_order_id, DateTime.Now, "Error " + ex.StatusCode, "wrong parameter sent to host", 0);

        //        }

        //    }
        //}

        private void ip(object sender, TextChangedEventArgs e)
        {
            // IP_host = Ip_host.Text;
        }

        private void ipvisu(object sender, TextChangedEventArgs e)
        {

        }

        private void Connect_Checked(object sender, RoutedEventArgs e)
        {
            if (connect.IsChecked == true)
            {
                comok = true;
                WIP_utility = Ip_utility_web.Text;
                WIP_host = Ip_host_web.Text;
                IP_utility = Ip_utility.Text;
                IP_host = Ip_host.Text;
                WIP_visu = Ip_visu_web.Text;
                IP_visu = Ip_visu.Text;
                connection();
            }
            else
            {
                comok = false;
                status.Content = "Server : " + "no connection" + "  |  system name : " + "no system | error ip ";
                status.Background = Brushes.OrangeRed;
                ev_id = 0;
            }
        }

        private void Ack_Checked(object sender, RoutedEventArgs e)
        {
            if (ack_auto.IsChecked == true)
            {
                autoack = true;

            }
            else
            {
                autoack = false;

            }
        }

        private void Pallet_Copy_TextChanged(object sender, TextChangedEventArgs e)
        {
            carwash = CWloc.Text;
        }

        private void Reset_log_Click(object sender, RoutedEventArgs e)
        {
            log2.Items.Clear();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            HttpClient client2 = new HttpClient();
            var sys = new Client(client2);
            // sys.BaseUrl = "http://" + IP_host + "/api/v1/";
            if (usew == true)
                sys.BaseUrl = "http://" + WIP_host + "/api/v1/";
            else
                sys.BaseUrl = "http://" + IP_host + "/api/v1/";

            var to = new TransportOrderDefinition();


            try
            {


                to = updateTO(Fetch_address.Text, Deliver_address.Text, stoid);
                var x2 = sys.Orders2Async(to, stoid).GetAwaiter().GetResult();

            }
            catch (ApiException ex)
            {
                Console.WriteLine("ERREUR ordre ...{0}", to.Transport_order_id);
                Console.WriteLine(ex.Message.ToString());

                updatedb_error(to.Transport_order_id, DateTime.Now, "Error", "bad location information please check", 0);


            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(ex.ToString());
            }

            update.IsEnabled = false;
        }

        private void Order_list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Order_list_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {

        }

        private void double_click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (order_list.SelectedItem != null)
                {// insert_agv();
                    string connectionString = Connect_string;

                    DataRowView dataRowView = (DataRowView)order_list.SelectedItem;
                    int ID = Convert.ToInt32(dataRowView.Row[0]);
                    stoid = dataRowView.Row[1].ToString();
                    string pick = dataRowView.Row[2].ToString();
                    string drop = dataRowView.Row[4].ToString();
                    string pickH = dataRowView.Row[3].ToString();
                    string dropH = dataRowView.Row[5].ToString();
                    string palette = dataRowView.Row[16].ToString();

                    Dispatcher.Invoke(new InvokeDelegate(() =>
                    {
                        Fetch_address.Text = pick;
                        Deliver_address.Text = drop;
                        Fetch_Height.Text = pickH;
                        Deliver_Height.Text = dropH;
                        pallet.Text = palette;
                        update.IsEnabled = true;
                        toid.Content = stoid.ToString();

                        //status.Background = (Brush)new BrushConverter().ConvertFromString("#ff9999");
                    }));

                }
            }
            catch (System.ArgumentNullException ex)

            {
                MessageBox.Show(ex.Message);
            }

        }



        private async void webhook_Checked(object sender, RoutedEventArgs e)
        {
            if (webhook.IsChecked == true)
            {
                // webhook_order.IsChecked = true;
                WIP_utility = Ip_utility_web.Text;
                WIP_host = Ip_host_web.Text;
                IP_utility = Ip_utility.Text;
                IP_host = Ip_host.Text;
                WIP_visu = Ip_visu_web.Text;
                IP_visu = Ip_visu.Text;
                // utistart();
                forward_adress = Ip_redirect_sent.Text;
                ip_host_webhooks = "http://+:" + Ip_host_webhook.Text;
                loca_ip_host_webbhooks = "http://localhost:" + Ip_host_webhook.Text;
                ip_visu_webhooks = "http://+:" + Ip_visu_webhook.Text;
                loca_ip_visu_webbhooks = "http://localhost:" + Ip_visu_webhook.Text;
                web = true;
                usew = true;
                WriteMessage("Server: listening" + ip_host_webhooks, Brushes.Green, log2);
                webh.Content = "Server : listening" + ip_host_webhooks;
                webh.Background = Brushes.LimeGreen;
                Thread c = new Thread(new ParameterizedThreadStart(serverAsync));
                //  c = new Thread(serverAsync);
                c.IsBackground = true;
                c.Start(ip_host_webhooks);
                if (enablevisu)
                {
                    Thread d = new Thread(new ParameterizedThreadStart(serverAsync));
                    d.IsBackground = true;
                    d.Start(ip_visu_webhooks);
                }

            }
            else
            {
                web = false;
                {
                    try
                    {

                        string url = loca_ip_host_webbhooks + "/visu";
                        // forward_adress = "http://localhost:8001";
                        HttpClient client = new HttpClient();
                        var requestData = new { type = "stop" };
                        string json = System.Text.Json.JsonSerializer.Serialize(requestData);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        var response = await client.PostAsync(url, content);
                        string responseContent = await response.Content.ReadAsStringAsync();
                        url = loca_ip_host_webbhooks + "/order";
                        content = new StringContent(json, Encoding.UTF8, "application/json");
                        response = await client.PostAsync(url, content);
                        responseContent = await response.Content.ReadAsStringAsync();
                        //MessageBox.Show(responseContent);*

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        // WriteMessage("fleet controller not running or connection to " + sys.BaseUrl.ToString() + " ", Brushes.MediumPurple, log2);

                    }
                }
                webhook.IsEnabled = true;

            }
        }





        void utistart()
        {
            HttpClient client2 = new HttpClient();
            var sys = new Client(client2);
            if (usew == true)
                sys.BaseUrl = "http://" + WIP_utility + "/api/v1/";
            else
                sys.BaseUrl = "http://" + IP_utility + "/api/v1/";


            var to = new UtilityOrderDefinition();


            try
            {


                to = NewUtilityTO();
                var x2 = sys.OrdersUAsync(to, to.Utility_order_id).GetAwaiter().GetResult();

            }
            catch (ApiException ex)
            {
                Console.WriteLine("ERREUR ordre ...{0}", to.Utility_order_id);
                Console.WriteLine(ex.Message.ToString());

                updatedb_error(to.Utility_order_id, DateTime.Now, "Error", "bad location information please check", 0);


            }
            catch (HttpRequestException)
            {
                //MessageBox.Show("fleet controller not running or connection to " + sys.BaseUrl.ToString() + " ", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                WriteMessage("fleet controller not running or connection to " + sys.BaseUrl.ToString() + " ", Brushes.MediumPurple, log2);
            }
        }

        private void iputility(object sender, TextChangedEventArgs e)
        {

        }

        private void weback_Checked(object sender, RoutedEventArgs e)
        {
            if (webhookACK.IsChecked == true)
            {
                autoackw = true;

            }
            else
            {
                autoackw = false;

            }
        }

        private void forward_Checked(object sender, RoutedEventArgs e)
        {
            if (forward1.IsChecked == true)
            {
                forward_enable = true;

            }
            else
            {
                forward_enable = false;

            }
        }


        private void weborder_Checked(object sender, RoutedEventArgs e)
        {
            if (webhook_order.IsChecked == true)
            {
                usew = true;

            }
            else
            {
                usew = false;

            }
        }

        private void Save_set_Click(object sender, RoutedEventArgs e)
        {
            {
                string connectionString = Connect_string;

                try
                {
                    using (SqlConnection connection2 = new SqlConnection(connectionString))
                    {
                        connection2.Open();

                        {
                            SqlCommand cmd = new SqlCommand(" UPDATE Settings SET  IP_host = @Ip_host, IP_visu = @Ip_visu, IP_utility = @Ip_utility ,WIP_host = @WIp_host, WIP_visu = @WIp_visu, WIP_utility = @WIp_utility,Carwash = @carwash  Where id = 1 ", connection2);

                            cmd.Parameters.AddWithValue("@Ip_host", Ip_host.Text);
                            cmd.Parameters.AddWithValue("@Ip_visu", Ip_visu.Text);
                            cmd.Parameters.AddWithValue("@Ip_utility", Ip_utility.Text);
                            cmd.Parameters.AddWithValue("@WIp_host", Ip_host_web.Text);
                            cmd.Parameters.AddWithValue("@WIp_visu", Ip_visu_web.Text);
                            cmd.Parameters.AddWithValue("@WIp_utility", Ip_utility_web.Text);
                            cmd.Parameters.AddWithValue("@carwash", CWloc.Text);
                            cmd.ExecuteNonQuery();


                        }
                        //status_db.Content = "Data updated to db";



                    }
                    MessageBox.Show("Settings saved");
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
        }

        private void Load_set_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection connection2 = new SqlConnection(Connect_string))
            {

                try
                {
                    connection2.Open();


                    DataSet ds2 = new DataSet();
                    DataRow dRow;
                    string str = "SELECT  * FROM Settings ";
                    SqlCommand cmd = new SqlCommand(str, connection2);
                    SqlDataAdapter sda2 = new SqlDataAdapter(cmd);
                    sda2.Fill(ds2);
                    dRow = ds2.Tables[0].Rows[0];
                    Ip_host.Text = dRow.ItemArray.GetValue(1).ToString();
                    Ip_visu.Text = dRow.ItemArray.GetValue(2).ToString();
                    Ip_utility.Text = dRow.ItemArray.GetValue(3).ToString();

                    Ip_host_web.Text = dRow.ItemArray.GetValue(4).ToString();
                    Ip_visu_web.Text = dRow.ItemArray.GetValue(5).ToString();
                    Ip_utility_web.Text = dRow.ItemArray.GetValue(6).ToString();
                    CWloc.Text = dRow.ItemArray.GetValue(7).ToString();
                    MessageBox.Show("Settings reloaded");
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Create Database");
                }
                finally
                {
                    connection2.Close();
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            Thread c = new Thread(get_bb);
            c.IsBackground = true;
            c.Start();
            BB.IsEnabled = false;
        }

        private void get_bb()
        {
            string bbpath = @"C:\Base8\Logs\BlacBoxes";
            try
            {
                var csvTable = new DataTable();
                string directory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string filename = directory + "\\bb.csv";

                using (var csvReader = new CsvReader(new StreamReader(System.IO.File.OpenRead(filename)), true, ';'))
                {
                    csvTable.Load(csvReader);
                }

                foreach (DataRow dr in csvTable.Rows)
                {
                    Console.WriteLine("{0}, {1}", dr[0].ToString(), dr[1].ToString());

                    try
                    {

                        if (dr[0].ToString() == "bb_path")
                        {
                            string bbpath2 = dr[1].ToString();

                            bbpath2 = bbpath2 + "/" + System.DateTime.Today.ToString("yyyy-MM-dd");
                            if (Directory.Exists(bbpath2))
                            {

                            }
                            else
                            {
                                Directory.CreateDirectory(bbpath2);
                            }
                            bbpath = bbpath2;
                            Dispatcher.Invoke(new InvokeDelegate(() =>
                            {
                                loc.Content = bbpath;

                            }));
                        }
                        else
                        {


                            HtmlWeb web = new HtmlWeb();
                            HtmlDocument site = web.Load("http://" + dr[1].ToString() + "/cgi-bin/blackbox/index.cgi");

                            WebClient dlclient = new WebClient();
                            HtmlAgilityPack.HtmlDocument hp = new HtmlAgilityPack.HtmlDocument();

                            HtmlNodeCollection links = site.DocumentNode.SelectNodes("//a[@href]");
                            // HtmlNodeCollection links = site.DocumentNode.SelectNodes("//a[contains(@href],'blackbox')]");
                            foreach (HtmlNode link in links)
                            {
                                Console.WriteLine(link.GetAttributeValue("href", "DefaultValue"));
                                string bb = link.GetAttributeValue("href", "DefaultValue");
                                //   bb.Replace("/blackbox-files/", link.InnerText);
                                if (link.GetAttributeValue("href", "DefaultValue").Contains("blackbox-files"))
                                {
                                    bb = bb.Replace("/blackbox-files/defaultsite", "/" + link.InnerText);
                                    string save = bbpath + bb;
                                    if (File.Exists(save))
                                    {
                                        Dispatcher.Invoke(new InvokeDelegate(() =>
                                        {

                                            ListViewItem li = new ListViewItem();
                                            li.Foreground = Brushes.Blue;
                                            li.Content = System.DateTime.Now.ToString("HH:mm:ss") + " -> " + link.GetAttributeValue("href", "DefaultValue").ToString() + " already downloaded  " + link.InnerText;
                                            this.bbview.Items.Add(li);

                                        }));

                                    }
                                    else
                                    {
                                        dlclient.DownloadFile("http://" + dr[1].ToString() + link.GetAttributeValue("href", "DefaultValue").ToString(), save);
                                        Dispatcher.Invoke(new InvokeDelegate(() =>
                                        {
                                            this.bbview.Items.Add(System.DateTime.Now.ToString("HH:mm:ss") + " -> " + link.GetAttributeValue("href", "DefaultValue").ToString() + "  " + link.InnerText);

                                        }));
                                    }

                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(new InvokeDelegate(() =>
                        {
                            ListViewItem li = new ListViewItem();
                            li.Foreground = Brushes.Red;
                            li.Content = System.DateTime.Now.ToString("HH:mm:ss") + " -> " + ex.Message.ToString() + " " + dr[1].ToString() + " AGV " + dr[0].ToString();
                            this.bbview.Items.Add(li);
                        }));
                    }

                }
            }





            catch (Exception ex)
            { MessageBox.Show(ex.ToString(), "GET blackBox"); }
            Dispatcher.Invoke(new InvokeDelegate(() =>
            {
                BB.IsEnabled = true;
            }));

        }

        private void changeBackground(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ListViewItem selectedItem = sender as ListViewItem;
            selectedItem.Background = Brushes.Red;
        }

        private void opendir_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(@"" + loc.Content);
            }
            catch (Exception ex)
            { MessageBox.Show(ex.ToString(), "GET blackBox"); }
        }

        private void changeBackground(object sender, MouseButtonEventArgs e)
        {

        }

        private void batch_Checked(object sender, RoutedEventArgs e)
        {
            if (batch_enable.IsChecked == true && batch_enable_drop.IsChecked == true)
            {
                useb = true;
                useb_drop = true;
                ctype = "3";
            }
            else if (batch_enable.IsChecked == true)
            {
                useb = true;
                useb_drop = false;
                ctype = "1";
            }


            else if (batch_enable_drop.IsChecked == true)
            {
                ctype = "2";
                useb_drop = true;
                useb = false;
            }
            else
            {
                ctype = "0";
                useb_drop = false;
                useb = false;

            }



        }

        private void C1_Click(object sender, RoutedEventArgs e)
        {
            HttpClient client2 = new HttpClient();
            var sys = new Client(client2);

            if (usew == true)
                sys.BaseUrl = "http://" + WIP_host + "/api/v1/";
            else
                sys.BaseUrl = "http://" + IP_host + "/api/v1/";

            var to = new TransportOrderDefinition();
            string content = (sender as Button).Content.ToString();
            MessageBox.Show("fleet controller not running or connection to " + content, "error", MessageBoxButton.OK, MessageBoxImage.Error);

            try
            {
                if (useb == true)
                {
                    to = createTO_const(Fetch_address.Text, Deliver_address.Text, 10, area.Text, Int16.Parse(sequence.Text), area.Text, ctype);
                    var x2 = sys.Orders2Async(to, to.Transport_order_id).GetAwaiter().GetResult();
                }
                else
                {
                    to = NewTO(content, "C2", 20);
                    var x2 = sys.Orders2Async(to, to.Transport_order_id).GetAwaiter().GetResult();
                }



            }
            catch (ApiException ex)
            {
                Console.WriteLine("ERREUR ordre ...{0}", to.Transport_order_id);
                Console.WriteLine(ex.Message.ToString());

                updatedb_error(to.Transport_order_id, DateTime.Now, "Error", "bad location information please check", 0);


            }
            catch (HttpRequestException)
            {
                MessageBox.Show("fleet controller not running or connection to " + sys.BaseUrl.ToString() + " ", "error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            refresh_orderDG();


        }

        private void IO_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void A1P1_Click(object sender, RoutedEventArgs e)
        {

        }

        public static string LoginToken()
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://localhost:8001");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                //  write your json content here
                string json = JsonConvert.SerializeObject(new
                {
                    userName = "test",
                    password = "test"
                }
                );


                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                return result;
            }

        }



        async void server2Async()
        {

            try
            {
                var listener = new HttpListener();

                listener.Prefixes.Add(sever2_adress);


                listener.Start();


                Console.WriteLine("Listening on port 8002...");


                //Thread thread = new Thread(() =>
                //{
                //    while (listener.IsListening)
                //    {
                while (true)
                {
                    try
                    {

                        HttpListenerContext ctx = listener.GetContext();
                        HttpListenerRequest request = ctx.Request;
                        // Console.WriteLine($"Received request for {request.Url}");
                        string ua = request.Headers.Get("User-Agent");
                        Console.WriteLine($"{request.HttpMethod} {request.Url}");

                        var body = request.InputStream;
                        var encoding = request.ContentEncoding;
                        var reader = new StreamReader(body, encoding);
                        string s = "null";
                        bool ok = false;
                        if (request.HasEntityBody)
                        {


                            if (request.ContentType != null)
                            {
                                Console.WriteLine("Client data content type {0}", request.ContentType);
                            }
                            Console.WriteLine("Client data content length {0}", request.ContentLength64);

                            // Console.WriteLine("Start of data:");
                            s = reader.ReadToEnd();
                            //Console.WriteLine(s);
                            //Console.WriteLine("End of data:");
                            // var jsonTextReader = new Newtonsoft.Json.JsonTextReader(reader);

                            //Event Event2 = System.Text.Json.JsonSerializer.Deserialize<Event>(s);
                            var dyna = JsonConvert.DeserializeObject<dynamic>(s);
                            string type = dyna.type;
                            if (type == "stop")
                            {
                                var response2 = ctx.Response;
                                response2.StatusCode = (int)HttpStatusCode.OK;
                                response2.ContentType = "text/plain";
                                response2.OutputStream.Write(new byte[] { }, 0, 0);
                                response2.OutputStream.Close();
                                break;
                            }


                            ok = await forward(s);
                            //   LocalDB.webhook_receive(dyna);
                            WriteMessage("Redirect: " + s, Brushes.Blue, bbview1);


                            reader.Close();
                            body.Close();

                        }
                        if (ok == false)
                        {

                            WriteMessage("Redirect: timeout ", Brushes.Blue, bbview1);
                        }
                        else
                        {
                            var response = ctx.Response;
                            response.StatusCode = (int)HttpStatusCode.OK;
                            response.ContentType = "text/plain";
                            response.OutputStream.Write(new byte[] { }, 0, 0);
                            response.OutputStream.Close();
                        }
                    }
                    catch (System.Net.HttpListenerException ex)
                    {
                        WriteMessage("Redirect: " + ex, Brushes.Blue, bbview1);
                    }

                }
                //});
                //thread.Start();
                //Console.ReadLine();
                //while (true)
                //{







                //    //HttpListenerResponse resp = ctx.Response;
                //    //resp.Headers.Set("Content-Type", "text/plain");

                //    //string data = s ?? "unknown";
                //    //byte[] buffer = Encoding.UTF8.GetBytes(data);
                //    //resp.ContentLength64 = buffer.Length;

                //    //Stream ros = resp.OutputStream;
                //    //ros.Write(buffer, 0, buffer.Length);
                //    if (web2 == false)
                //    {
                //        break;
                //    }
                //}
                listener.Stop();
                Console.WriteLine("Stop Listening on port 8002...");
                // web2 = true;
                WriteMessage("Redirect: Stop Listening on: " + sever2_adress, Brushes.Blue, bbview1);
                Dispatcher.Invoke(new InvokeDelegate(() =>
               {
                   BB1.Content = "Start listenening ";
                   BB1.Background = Brushes.LightGray;


               }));
                //thread.Join();
            }
            catch (HttpListenerException ex)
            {
                // MessageBox.Show(ex.Message);
                web2 = false;
                WriteMessage("Redirect: error Listening on: " + sever2_adress, Brushes.MediumVioletRed, bbview1);
                WriteMessage("Redirect:" + ex.Message, Brushes.MediumVioletRed, bbview1);
                Dispatcher.Invoke(new InvokeDelegate(() =>
                {
                    BB1.Content = "Start listenening ";
                    BB1.Background = Brushes.LightGray;
                    //ListViewItem li = new ListViewItem();
                    //li.Content = "Redirect: Error Listening on " + sever2_adress;
                    //this.bbview1.Items.Add(li);
                    //    this.bbview1.ScrollIntoView(this.bbview1.Items.Count);
                }));
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public async Task<bool> forward(string s)
        {
            try
            {

                string url = forward_adress;
                HttpClient client = new HttpClient();
                var requestData = new { type = "value" };
                string json = System.Text.Json.JsonSerializer.Serialize(s);
                var content = new StringContent(s, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
                string responseContent = await response.Content.ReadAsStringAsync();
                //MessageBox.Show(responseContent);
                return true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                WriteMessage("Redirect: error while forwarding to: " + forward_adress, Brushes.MediumVioletRed, bbview1);
                WriteMessage("Redirect:" + ex.Message, Brushes.MediumVioletRed, bbview1);
                return false;
            }

        }



        private async void test_click(object sender, RoutedEventArgs e)
        {
            try
            {
                test.IsEnabled = false;
                string url = Ip_host_web_TEST.Text;

                HttpClient client = new HttpClient();
                var requestData = new { type = test_text.Text };
                string json = System.Text.Json.JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
                string responseContent = await response.Content.ReadAsStringAsync();
                //MessageBox.Show(responseContent);*
                test.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                test.IsEnabled = true;

            }
        }



        private async void Button_Click_server(object sender, RoutedEventArgs e)
        {
            if (web2)
            {
                try
                {

                    string url = loca_serv2_adress;

                    HttpClient client = new HttpClient();
                    var requestData = new { type = "stop" };
                    string json = System.Text.Json.JsonSerializer.Serialize(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    //MessageBox.Show(responseContent);*
                    web2 = false;
                    //test.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    // MessageBox.Show(ex.Message);
                    WriteMessage("Redirect: error stop Listening on: " + sever2_adress, Brushes.Blue, bbview1);
                    WriteMessage("Redirect:" + ex.Message, Brushes.Blue, bbview1);
                }
            }
            else
            {
                sever2_adress = "http://+:" + Ip_redirect_web.Text;
                loca_serv2_adress = "http://localhost:" + Ip_redirect_web.Text;
                forward_adress = Ip_redirect_sent.Text;
                BB1.Content = " stop listening ";
                BB1.Background = Brushes.OrangeRed;
                WriteMessage("Redirect: start Listening on: " + sever2_adress, Brushes.Blue, bbview1);

                web2 = true;
                c = new Thread(server2Async);
                c.IsBackground = true;
                c.Start();
            }
        }

        private void Update_server_Click(object sender, RoutedEventArgs e)
        {
            sever2_adress = "http://+:" + Ip_redirect_web.Text;
            loca_serv2_adress = "http://+:" + Ip_redirect_web.Text;

        }

        private void Update_webhook_Click(object sender, RoutedEventArgs e)
        {
            ip_host_webhooks = "http://+:" + Ip_host_webhook.Text;
            loca_ip_host_webbhooks = "http://localhost:" + Ip_host_webhook.Text;
            ip_visu_webhooks = "http://+:" + Ip_visu_webhook.Text;
            loca_ip_visu_webbhooks = "http://localhost:" + Ip_visu_webhook.Text;
        }

        private async void Start_test_Click(object sender, RoutedEventArgs e)
        {
            if (test2)
            {
                try
                {

                    string url = Loca_test_ip;

                    HttpClient client = new HttpClient();
                    var requestData = new { type = "stop" };
                    string json = System.Text.Json.JsonSerializer.Serialize(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    //MessageBox.Show(responseContent);*
                    test2 = false;
                    // test.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                    WriteMessage("Test: " + ex.Message, Brushes.MediumPurple, bbview1);
                    // test.IsEnabled = true;
                    test2 = false;

                }
            }
            else
            {
                test_ip = "http://+:" + Ip_test_server.Text;
                Loca_test_ip = "http://localhost:" + Ip_test_server.Text;
                start_test.Content = "Test stop \n listening ";
                start_test.Background = Brushes.OrangeRed;
                WriteMessage("Test: start Listening on " + test_ip, Brushes.MediumPurple, bbview1);
                test2 = true;
                c = new Thread(test_server);
                c.IsBackground = true;
                c.Start();
            }
        }



        void test_server()
        {
            try
            {
                var listener = new HttpListener();

                listener.Prefixes.Add(test_ip);


                listener.Start();


                Console.WriteLine("Listening on " + test_ip);


                //Thread thread = new Thread(() =>
                //{
                //    while (listener.IsListening)
                //    {
                while (true)
                {
                    try
                    {

                        HttpListenerContext ctx = listener.GetContext();
                        HttpListenerRequest request = ctx.Request;
                        // Console.WriteLine($"Received request for {request.Url}");
                        string ua = request.Headers.Get("User-Agent");
                        Console.WriteLine($"{request.HttpMethod} {request.Url}");

                        var body = request.InputStream;
                        var encoding = request.ContentEncoding;
                        var reader = new StreamReader(body, encoding);
                        string s = "null";

                        if (request.HasEntityBody)
                        {


                            if (request.ContentType != null)
                            {
                                Console.WriteLine("Client data content type {0}", request.ContentType);
                            }
                            Console.WriteLine("Client data content length {0}", request.ContentLength64);

                            // Console.WriteLine("Start of data:");
                            s = reader.ReadToEnd();
                            //Console.WriteLine(s);
                            //Console.WriteLine("End of data:");
                            // var jsonTextReader = new Newtonsoft.Json.JsonTextReader(reader);

                            //Event Event2 = System.Text.Json.JsonSerializer.Deserialize<Event>(s);
                            var dyna = JsonConvert.DeserializeObject<dynamic>(s);
                            string type = dyna.type;
                            if (type == "stop")
                            {
                                var response2 = ctx.Response;
                                response2.StatusCode = (int)HttpStatusCode.OK;
                                response2.ContentType = "text/plain";
                                response2.OutputStream.Write(new byte[] { }, 0, 0);
                                response2.OutputStream.Close();
                                break;
                            }

                            //   LocalDB.webhook_receive(dyna);
                            WriteMessage("Test: " + s, Brushes.MediumPurple, bbview1);


                            reader.Close();
                            body.Close();

                        }

                        var response = ctx.Response;
                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.ContentType = "text/plain";
                        response.OutputStream.Write(new byte[] { }, 0, 0);
                        response.OutputStream.Close();
                    }
                    catch (System.Net.HttpListenerException ex)
                    {
                        WriteMessage("Test: error" + ex.Message, Brushes.MediumPurple, bbview1);
                    }

                }
                //});
                //thread.Start();
                //Console.ReadLine();
                //while (true)
                //{







                //    //HttpListenerResponse resp = ctx.Response;
                //    //resp.Headers.Set("Content-Type", "text/plain");

                //    //string data = s ?? "unknown";
                //    //byte[] buffer = Encoding.UTF8.GetBytes(data);
                //    //resp.ContentLength64 = buffer.Length;

                //    //Stream ros = resp.OutputStream;
                //    //ros.Write(buffer, 0, buffer.Length);
                //    if (web2 == false)
                //    {
                //        break;
                //    }
                //}
                listener.Stop();
                Console.WriteLine("Stop Listening on port 8002...");
                // web2 = true;
                WriteMessage("Test: Stop Listening on " + test_ip, Brushes.MediumPurple, bbview1);
                Dispatcher.Invoke(new InvokeDelegate(() =>
            {
                start_test.Content = "Start listenening ";
                start_test.Background = Brushes.LightBlue;

            }));

            }
            catch (HttpListenerException ex)
            {
                // MessageBox.Show(ex.Message);
                WriteMessage("Test: Error Listening on " + test_ip, Brushes.MediumPurple, bbview1);
                WriteMessage("Test: " + ex.Message, Brushes.MediumPurple, bbview1);

                Dispatcher.Invoke(new InvokeDelegate(() =>
                {
                    start_test.Content = "Start Test \n server ";
                    start_test.Background = Brushes.LightBlue;
                    test2 = false;
                    //webhook.IsEnabled = true;
                }));
            }

        }

        private void Update_test_server_ip_Click(object sender, RoutedEventArgs e)
        {
            test_ip = "http://+:" + Ip_test_server.Text + "/";
            Loca_test_ip = "http://localhost:" + Ip_test_server.Text + "/";
        }

        private void Update_redirect_Click(object sender, RoutedEventArgs e)
        {
            forward_adress = Ip_redirect_sent.Text;
        }


        private void WriteMessage(string message, Brush color, ListView lv)
        {

            Dispatcher.BeginInvoke(new Action(delegate
            {
                ListViewItem ls = new ListViewItem
                {
                    Foreground = color,
                    Content = message
                };
                lv.Items.Add(ls);
                lv.ScrollIntoView(lv.Items[lv.Items.Count - 1]);
            }));
        }

        private void Start4_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Visuenable_Checked(object sender, RoutedEventArgs e)
        {
            if (visuenable.IsChecked == true)
            {
                enablevisu = true;
            }
            else { enablevisu = false; }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            bbview1.Items.Clear();
        }


        private TransportOrderDefinition NewTO3(string fetch, string deliver, int delay)
        {

            TransportOrderStep stp1 = new TransportOrderStep
            {
                Operation_type = "Task",
                Addresses = new string[] { deliver }
            };
            TransportOrderStep stp2 = new TransportOrderStep
            {
                Operation_type = "Drop",
                Addresses = new string[] { fetch }
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
                DeliverAddress = deliver,
                AGV = AGVID.Text,
                LoadType = pallet.Text,
            };

            var to = new TransportOrderDefinition
            {
                Transport_order_id = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(),
                Transport_unit_type = pallet.Text,
                Start_time = null,
                End_time = DateTimeOffset.UtcNow.AddMinutes(delay),
                Custom_data = custom,
                Steps = new TransportOrderStep[] { stp0, stp1, stp2 },
                Partial_steps = false,
            };

            string CmdText;
            SqlParameter SqlPrmtr;
            List<SqlParameter> PrmtrList;



            CmdText = " INSERT INTO orderbuffer" +

                "([transport_order_id],[fetch_address],[fetch_height] ,[deliver_address],[deliver_height],[LoadDimensionX],[LoadDimensionY],[LoadWeight],[CreatedAt],[Sent],[pallet_type]) " +

                "VALUES (@transport_order_id,@fetch_address,@fetch_height, @deliver_address,@deliver_height,@LoadDimensionX,@LoadDimensionY,@LoadWeight,@CreatedAt,@Sent,@pallet_type)";

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

            SqlPrmtr = new SqlParameter("@pallet_type", SqlDbType.VarChar);
            SqlPrmtr.Value = pallet.Text;
            PrmtrList.Add(SqlPrmtr);

            InsertIntoSql(CmdText, PrmtrList, "new order added");


            return to;
        }

        private TransportOrderDefinition createTO3_const(string fetch, string deliver, string wait, int delay, string cid, int numid, string Area, string v)
        {
            string cid_drop = cid + "drop";
            int numid_drop = numid;
            palett = "Palett";

            if (v == "0" || v == "2")
            {
                cid = "";
                numid = 0;
            }


            TransportOrderStep stp0 = new TransportOrderStep
            {
                Operation_type = "Pick",
                Addresses = new string[] { fetch },
                Constraint_group_id = cid,
                Constraint_group_index = numid
            };

            if (v == "0" || v == "1")
            {
                cid_drop = "";
                numid_drop = 0;
            }

            TransportOrderStep stp1 = new TransportOrderStep
            {
                Operation_type = "Wait",
                Addresses = new string[] { wait },


            };

            TransportOrderStep stp2 = new TransportOrderStep
            {
                Operation_type = "Drop",
                Addresses = new string[] { deliver },
                Constraint_group_id = cid_drop,
                Constraint_group_index = numid_drop

            };


            custom_dataC custom = new custom_dataC
            {
                LoadWeight = "800",
                LoadDimensionX = "800",
                LoadDimensionY = "800",
                FetchHeight = "0",
                DeliverHeight = "0",
                FetchAddress = fetch,
                DeliverAddress = deliver,
                AGV = AGV,
                FetchConstraint = Area,
                FetchSequence = numid
            };


            var to = new TransportOrderDefinition
            {
                Transport_order_id = DateTime.Now.Subtract(new DateTime(2024, 1, 1)).TotalMilliseconds.ToString(),
                Transport_unit_type = palett,
                Start_time = null,
                End_time = DateTimeOffset.UtcNow.AddMinutes(delay),
                Custom_data = custom,
                Steps = new TransportOrderStep[] { stp0, stp1, stp2 },
                Partial_steps = false,
            };



            string connectionString = Connect_string;

            string CmdText;
            SqlParameter SqlPrmtr;
            List<SqlParameter> PrmtrList;



            CmdText = " INSERT INTO orderbuffer" +

                "([transport_order_id],[fetch_address],[fetch_height] ,[deliver_address],[deliver_height],[LoadDimensionX],[LoadDimensionY],[LoadWeight],[CreatedAt],[Sent],[pallet_type]) " +

                "VALUES (@transport_order_id,@fetch_address,@fetch_height, @deliver_address,@deliver_height,@LoadDimensionX,@LoadDimensionY,@LoadWeight,@CreatedAt,@Sent,@pallet_type)";

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

            SqlPrmtr = new SqlParameter("@pallet_type", SqlDbType.VarChar);
            SqlPrmtr.Value = palett;
            PrmtrList.Add(SqlPrmtr);

            InsertIntoSql(CmdText, PrmtrList, "new order added");



            return to;
        }


        private void Csv_loazd3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var csvTable = new DataTable();
                HttpClient client2 = new HttpClient();
                var sys = new Client(client2);
                if (usew == true)
                    sys.BaseUrl = "http://" + WIP_host + "/api/v1/";
                else
                    sys.BaseUrl = "http://" + IP_host + "/api/v1/";
                // sys.BaseUrl = "http://" + IP_host + "/api/v1/";
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
                    using (var csvReader = new CsvReader(new StreamReader(System.IO.File.OpenRead(filename)), true, ';'))
                    {
                        csvTable.Load(csvReader);
                    }
                    string Column1 = csvTable.Columns[0].ToString();
                    string Row1 = csvTable.Rows[0][1].ToString();
                    string area = "";
                    string area_id = "";
                    foreach (DataRow dr in csvTable.Rows)
                    {
                        Console.WriteLine("{0}, {1}, {2}", dr[0].ToString(), dr[1].ToString(), dr[2].ToString());


                        try
                        {
                            if (dr[3].ToString() == "0")

                                to = NewTO3(dr[0].ToString(), dr[1].ToString(), int.Parse(dr[2].ToString()));
                            else
                            {
                                if (area != dr[3].ToString())
                                {
                                    area = dr[3].ToString();
                                    area_id = area + DateTime.Now.Subtract(new DateTime(2024, 1, 1)).TotalMilliseconds.ToString();
                                }
                           //     to = createTO3_const(dr[0].ToString(), dr[1].ToString(), int.Parse(dr[2].ToString()), area_id, int.Parse(dr[4].ToString()), dr[3].ToString(), dr[5].ToString());
                            }


                            var x2 = sys.Orders2Async(to, to.Transport_order_id).GetAwaiter().GetResult();
                            Thread.Sleep(1000);
                        }
                        catch (ApiException ex)
                        {
                            Console.WriteLine("ERREUR ordre ...{0}", to.Transport_order_id);
                            Console.WriteLine(ex.Message.ToString());

                            updatedb_error(to.Transport_order_id, DateTime.Now, "Error " + ex.StatusCode, "wrong parameter sent to host", 0);

                        }

                        // refresh_orderDG();
                        //   Console.WriteLine(Column1, Row1);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "load csv");

            }
        }



        private void auto_incre(object sender, RoutedEventArgs e)
        {
            if (batch_autoincrment.IsChecked == true)
            {

                auto_increment = true;

            }
            else
            {


                auto_increment = false;

            }
        }

        private void usef(object sender, RoutedEventArgs e)
        {
            if (usefetch.IsChecked == true)
                use_fetch = true;
            else use_fetch = false;
        }

        private void getackClick(object sender, RoutedEventArgs e)
        {
            HttpClient client2 = new HttpClient();
            var sys = new Client(client2);
            sys.BaseUrl = "http://" + IP_host + "/api/v1/";
            try
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
                        item.Payload.AdditionalProperties.TryGetValue("error_name", out object error);
                        item.Payload.AdditionalProperties.TryGetValue("end_status", out object status_op);
                        Console.WriteLine(" validation adress: {0},transport_order_id : {1} , step {2} ", value, value2, value3);
                        string er;
                        if (error == null)
                            er = "";
                        else er = error.ToString();


                        Dispatcher.Invoke(new InvokeDelegate(() =>
                        {
                            this.log2.Items.Add(" - t_id : " + value2.ToString() + " waiting ack for adresse: " + value.ToString() + ", step: " + value3.ToString() + "  ,status: " + status_op.ToString() + "  ,error: " + er);

                        }));

                        //MainWindow.updatedb(To_id, DateTimeOffset.Parse(start_time), status, int.Parse(Ev_id));
                        // MainWindow.updatedb(value2.ToString(), value4.ToString(), "AGV waiting fetched ack", int.Parse(Ev_id));
                      
                            sys.ContinueAsync(value2.ToString(), item.Event_id);
                            Console.WriteLine(" ack operation envoyé ");
                            Dispatcher.Invoke(new InvokeDelegate(() =>
                            {
                                this.log2.Items.Add(" * t_id : " + value2.ToString() + " ack sent for adresse: " + value.ToString() + ", step: " + value3.ToString());
                            }));
                      


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
                            this.log2.Items.Add(" * t_id : " + value2.ToString() + " waiting preload ack : adress: " + value.ToString() + ", step: " + value3.ToString());
                        }));
                       
                            sys.ContinueAsync(value2.ToString(), item.Event_id);
                            Console.WriteLine(" ack arrivé envoyé ");
                            Dispatcher.Invoke(new InvokeDelegate(() =>
                            {
                                this.log2.Items.Add(" - t_id : " + value2.ToString() + " preload ack sent : adress: " + value.ToString() + " , step: " + value3.ToString());
                                //  this.log.Items[this.log.Items.Count - 1].Background = Brushes.LimeGreen
                            }));
                       

                    }
                    if (item.Type == "UnconnectedOrderCreatedEvent")
                    {
                        Console.WriteLine("carwash");



                    }
                    if (item.Type == "ParameterUpdateEvent")
                    {
                        Console.WriteLine("event update");
                        item.Payload.AdditionalProperties.TryGetValue("parameter_name", out object name);
                        item.Payload.AdditionalProperties.TryGetValue("transport_order_id", out object To_id);
                        item.Payload.AdditionalProperties.TryGetValue("parameter_value", out object value);
                        //   item.Payload.AdditionalProperties.TryGetValue("drive_start_time", out object value4);




                        ParameterRequestAnswer stp1 = new ParameterRequestAnswer
                        {
                            Event_id = item.Event_id,
                            Parameter_name = name.ToString(),
                            Parameter_value = value.ToString()
                        };

                        HttpClient client = new HttpClient();

                        var syspar = new ParameterClient(client);

                        syspar.BaseUrl = "http://" + IP_host + "/api/v1/";
                        syspar.ParAsync(To_id.ToString(), stp1);




                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "send ack");

            }

        }
    }
    public class T
    {
        public bool ok { get; set; }
    }
}
