using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using NLog;
using MercuryMeter;
using System.IO.Ports;
using System.Globalization;
using System.Threading;
using System.Data;
//using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization.Json;
//using System.Runtime.Serialization.Formatters.Binary;

namespace ConsoleApplication1
{
     class Program
    {
        struct ProgSettings 
        {
            public bool start ;
            public int TCPport ;
            public int timeoutSerial;
            public string tblName_meter230;
            public string tblName_meter206;

        }

        static ProgSettings progSettings = new ProgSettings();

        //static Mutex stop_quierMutex = new Mutex();
        static TCP_Server server ;
        static SQLhandler Sqlhandler_ ;
        static Task MeterQuerier;
        static System.Timers.Timer refreshTimer;

        public static Logger logger = LogManager.GetCurrentClassLogger();
        
        
        /// <summary>
        /// Массив счетчиков модели 230 - 234
        /// </summary>
        public static Mercury230_DatabaseSignals[] Meter230_arr;
        public static Mercury206_Database[] Meter206_arr;

        //static bool HandlerWorked = false;
        //protected static void ConsoleCancelHandler(object sender, ConsoleCancelEventArgs args)
        //{
        //    //if (!HandlerWorked)
        //    //{
        //    //    HandlerWorked = true;
        //        stop_quierMutex.WaitOne();
                
        //        Console.WriteLine("\nThe read operation has been interrupted.");

        //        Console.WriteLine("  Key pressed: {0}", args.SpecialKey);

        //        Console.WriteLine("  Cancel property: {0}", args.Cancel);

        //        // Set the Cancel property to true to prevent the process from terminating.
        //        Console.WriteLine("Setting the Cancel property to true...");
        //        args.Cancel = true;

        //        // Announce the new value of the Cancel property.
        //        Console.WriteLine("  Cancel property: {0}", args.Cancel);
        //        Console.WriteLine("The read operation will resume...\n");
        //     //   HandlerWorked = false;
        //        Console.ReadLine();
                
        //        stop_quierMutex.ReleaseMutex();

        //    //}
        //}

        //static void loopMeterSQLwriter()
        //{

        //}
        public static void loopMeterQuerier()
        {
            
            //Meter230_arr[0].InitRs();
            System.Threading.Thread.Sleep(2000);

               
                while (progSettings.start)
                 {
                  try
                  {


                #region Опрос 234-тых
                for (int i = 0; (i < Meter230_arr.Length) && progSettings.start; i++)
                {
                    //stop_quierMutex.WaitOne();
                    //  string buf = meterTable.Rows[i]["Тип"].ToString();
                    //  meterTable.Rows[i]["Тип"] = meterTable.Rows[i]["Тип"].ToString() + "*";
                    Mercury230.error_type error = Meter230_arr[i].RefreshData();
                    //console.WriteLine("{0}", Meter230_arr[i].DataTime_last_recordSQL);
                    // double time = Sqlhandler_.NextTimetoSQLwrite(Meter230_arr[i].address, Meter230_arr[i].serial_number);
                    // console.WriteLine("Следующая запись : {0}" , DateTime.FromOADate(time));
                    // server_stop();

                    if (error == Mercury230.error_type.none)
                    {
                        if ((DateTime.Now.Hour > 3) && (DateTime.Now.Hour < 23))
                        {
                            #region Коррекция времени
                            if (DateTime.Now > Meter230_arr[i].DateTime_nextTime_corecction)
                            {
                                //
                                Mercury230.elecMeterDateTime datetime = Meter230_arr[i].CallTime();
                                TimeSpan span = DateTime.Now - datetime.datetime;
                                if (span.Duration() > new TimeSpan(0, 0, 10))
                                {
                                    // Meter230_arr[i].level = 2;
                                    datetime = new Mercury230.elecMeterDateTime();
                                    datetime.datetime = DateTime.Now;
                                    Meter230_arr[i].SetTime(datetime);
                                    logger.Debug("Корректировка|адрес {0}|время {1}", Meter230_arr[i].address, Meter230_arr[i].CallTime().datetime);


                                }
                                else
                                {
                                    logger.Debug("Корректировка|адрес:{0}|Пропущена|{1}", Meter230_arr[i].address, span);
                                    // console.WriteLine(, span);
                                    Meter230_arr[i].DateTime_nextTime_corecction = DateTime.Now.AddMinutes(5);
                                }
                            }
                            #endregion

                            #region Снятие показаний

                            if (DateTime.Now > Meter230_arr[i].DataTime_nextPoint_recordSQL)
                            {
                                Sqlhandler_.ConnectOpen();
                                //Mercury230.accumulEnergy energy1 = Meter230_arr[i].CallAccumulEnergy(Mercury230.peroidQuery.thisMonth, 1, 2);
                                for (byte tarif = 1; tarif < 5; tarif++)
                                {
                                    if (Sqlhandler_.GiveSQLrecordingPeriod(Meter230_arr[i], "meter230", Mercury230.peroidQuery.lastDay, 0, tarif).Rows.Count == 0)
                                    {
                                        Mercury230.accumulEnergy energy = Meter230_arr[i].CallAccumulEnergy(Mercury230.peroidQuery.lastDay, tarif, 0);
                                        if (energy.error == 0)
                                        {
                                            Sqlhandler_.writeAccumulEnergy(energy, tarif, Mercury230.peroidQuery.lastDay, Meter230_arr[i], 0);
                                            logger.Debug("Запись счетчика {0}|За вчерашний день|Тариф {1}  ", Meter230_arr[i].address, tarif);
                                        }
                                        else
                                        {
                                            logger.Debug("Ошибка чтения показаний счетчика {0}|За вчерашний день|Тариф {1}  ", Meter230_arr[i].address, tarif);
                                        }
                                    }
                                    if (Sqlhandler_.GiveSQLrecordingPeriod(Meter230_arr[i], "meter230", Mercury230.peroidQuery.thisDay_beginning, 0, tarif).Rows.Count == 0)
                                    {
                                        Mercury230.accumulEnergy energy = Meter230_arr[i].CallAccumulEnergy(Mercury230.peroidQuery.thisDay_beginning, tarif, 0);
                                        if (energy.error == 0)
                                        {
                                            Sqlhandler_.writeAccumulEnergy(energy, tarif, Mercury230.peroidQuery.thisDay_beginning, Meter230_arr[i], 0);
                                            logger.Debug("Запись счетчика {0}|На начало дня|Тариф {1}  ", Meter230_arr[i].address, tarif);
                                        }
                                        else
                                        {
                                            logger.Debug("Ошибка чтения показаний счетчика {0}|На начало дня|Тариф {1}  ", Meter230_arr[i].address, tarif);
                                        }
                                    }
                                    for (byte month = 1; month <= 12; month++)
                                    {

                                        if ((Sqlhandler_.GiveSQLrecordingPeriod(Meter230_arr[i], "meter230", Mercury230.peroidQuery.thisMonth, month, tarif).Rows.Count == 0) && (month != DateTime.Now.Month))
                                        {

                                            Mercury230.accumulEnergy energy = Meter230_arr[i].CallAccumulEnergy(Mercury230.peroidQuery.thisMonth, tarif, month);
                                            if (energy.error == 0)
                                            {
                                                Sqlhandler_.writeAccumulEnergy(energy, tarif, Mercury230.peroidQuery.thisMonth, Meter230_arr[i], month);
                                                logger.Debug("Запись счетчика {0}|За месяц {2}|Тариф {1}  ", Meter230_arr[i].address, tarif, month);
                                            }
                                            else
                                            {
                                                logger.Debug("Ошибка чтения показаний счетчика {0}|За месяц {2}|Тариф {1}  ", Meter230_arr[i].address, tarif);
                                            }
                                        }
                                    }
                                    for (byte month = 1; month <= 12; month++)
                                    {

                                        if (Sqlhandler_.GiveSQLrecordingPeriod(Meter230_arr[i], "meter230", Mercury230.peroidQuery.thisMonth_beginning, month, tarif).Rows.Count == 0)
                                        {

                                            Mercury230.accumulEnergy energy = Meter230_arr[i].CallAccumulEnergy(Mercury230.peroidQuery.thisMonth_beginning, tarif, month);
                                            if (energy.error == 0)
                                            {
                                                Sqlhandler_.writeAccumulEnergy(energy, tarif, Mercury230.peroidQuery.thisMonth_beginning, Meter230_arr[i], month);
                                                logger.Debug("Запись счетчика {0}|На начало месяца {2}|Тариф {1}  ", Meter230_arr[i].address, tarif, month);
                                            }
                                            else
                                            {
                                                logger.Debug("Ошибка чтения показаний счетчика {0}|На начало месяца {2}|Тариф {1}  ", Meter230_arr[i].address, tarif);
                                            }
                                        }
                                    }
                                }
                                Meter230_arr[i].DataTime_nextPoint_recordSQL = DateTime.Now.AddSeconds(30);
                                Sqlhandler_.ConnectClose();
                            }

                        }
                            #endregion
                    }
                    //stop_quierMutex.ReleaseMutex();
                }
                #endregion

                #region Опрос 206-ых
                for (int i = 0; (i < Meter206_arr.Length) && progSettings.start; i++)
                {
                    //stop_quierMutex.WaitOne();
                    MeterDevice.error_type error;

                    int serialnum_ = Meter206_arr[i].GiveSerialNumber();

                    if (Meter206_arr[i].serial_number == serialnum_)
                    {
                        error = MeterDevice.error_type.none;

                    }
                    else
                    {
                        error = (MeterDevice.error_type)serialnum_;
                        if (!Enum.IsDefined(typeof(MeterDevice.error_type), serialnum_))
                        {
                            logger.Debug("!!! Адрес {0}| S/n {1}|Ожидался {2} !!!", Meter206_arr[i].i_addr, serialnum_, Meter206_arr[i].serial_number);
                            break;
                        }

                    }

                    if (error == MeterDevice.error_type.none)
                    {
                        if ((DateTime.Now.Hour > 3) && (DateTime.Now.Hour < 23))
                        {
                            #region Время
                            if (DateTime.Now > Meter206_arr[i].DateTime_nextTime_corecction)
                            {
                                //
                                Mercury206.elecMeterDateTime datetime206 = Meter206_arr[i].CallDateTime();
                                TimeSpan span = DateTime.Now - datetime206.dt;
                                if (span.Duration() > new TimeSpan(0, 0, 10))
                                {
                                    // Meter230_arr[i].level = 2;
                                    datetime206 = new Mercury206.elecMeterDateTime();
                                    datetime206.dt = DateTime.Now;
                                    Meter206_arr[i].SetTime(datetime206.dt);
                                    logger.Debug("Корректировка|адрес {0}|время {1}", Meter206_arr[i].i_addr, Meter206_arr[i].CallDateTime().dt);
                                }
                                else
                                {
                                    logger.Debug("Корректировка|адрес:{0}|Пропущена|{1}", Meter206_arr[i].i_addr, span);
                                    // console.WriteLine(, span);
                                    Meter206_arr[i].DateTime_nextTime_corecction = DateTime.Now.AddMinutes(5);
                                }
                            }
                            #endregion

                            if (Meter206_arr[i].DataTime_nextPoint_recordSQL < DateTime.Now)
                            {
                                for (byte per = 0; per <= 12; per++)
                                {
                                    Sqlhandler_.ConnectOpen();
                                    if (Sqlhandler_.GiveSQLrecordingPeriod(Meter206_arr[i], "meter206", per).Rows.Count == 0)
                                    {
                                        int[] energy = (per == 0) ? Meter206_arr[i].CallAccumulEnergy_activ() : Meter206_arr[i].CallAccumulEnergy_activ_month((byte)((int)per - 1));
                                        if (energy.Length == 4)
                                        {
                                            Sqlhandler_.writeAccumulEnergy(energy, Meter206_arr[i], per);
                                            logger.Debug("Запись счетчика произведена. Счетчик {0}, ПериодКод {1}", Meter206_arr[i].i_addr, per);
                                        }
                                    }
                                    Sqlhandler_.ConnectClose();
                                    Meter206_arr[i].DataTime_nextPoint_recordSQL = DateTime.Now.AddSeconds(30);
                                }
                            }
                        }
                        //if (DateTime.Now > Meter206_arr[i].DataTime_nextPoint_recordSQL)
                        //{
                        //    Sqlhandler_.ConnectOpen();
                        //        int[] energy = Meter206_arr[i].CallAccumulEnergy_activ();
                        //        if (energy.Length == 4)
                        //        {
                        //            Sqlhandler_.writeAccumulEnergy(energy, Meter206_arr[i], 0);
                        //            //Mercury230.accumulEnergy energy2 = Meter230_arr[i].CallAccumulEnergy(Mercury230.peroidQuery.afterReset, j, 1);
                        //            //Sqlhandler_.writeAccumulEnergy(energy2, j, Mercury230.peroidQuery.afterReset, Meter230_arr[i]);

                        //            logger.Debug("Запись счетчика произведена. Счетчик {0} Время {1}", Meter206_arr[i].i_addr, DateTime.Now);
                        //            // myConnection.Close();
                        //            DateTime dt = DateTime.Now;
                        //            // Делаем записи в 00:30 . Писать в 00:00 рисковано - счетчик может не перейти на новую дату
                        //            DateTime dt_beginday = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 30);
                        //            Meter206_arr[i].DataTime_nextPoint_recordSQL = dt_beginday.AddDays(1);
                        //        }
                        //    Sqlhandler_.ConnectClose();
                        //}
                    }
                    logger.Trace("Устройство {0} : {1}", Meter206_arr[i].i_addr, error);
                    //stop_quierMutex.ReleaseMutex();
                }
                #endregion

                 }
                 catch (Exception ex)
                 {
                logger.Error(ex);
                  }
                }



        }

        #region Генерация ответов по TCP

        static string answerMakerMetod(string args)
        {
            Func<XmlDocument, string, string> giveNodeText = (xml, param) =>
            {
                try
                {
                    return xml.SelectNodes("/query/" + param)[0].InnerText;
                }
                catch
                {
                    return "";
                }
            };

            string str_badformat = "<Error>badformat</Error>";
            string str_null = "<Error>null</Error>";
            StringBuilder answerBuilder = new StringBuilder();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Mercury230_DatabaseSignals));
            XmlDocument xmlquery = new XmlDocument();
            try
            {
                xmlquery.LoadXml(args);
            }
            catch (XmlException)
            {
                return str_badformat;
            }


            XmlNodeList nodeList = xmlquery.SelectNodes("/query/type");

            if (nodeList.Count > 0)
            {
                //nodeList = xmlquery.SelectNodes("/query/type");
                switch (nodeList[0].InnerText)
                {
                    case "readmeter":
                        string add_meter = giveNodeText(xmlquery, "add_meter");
                        string id_meter = giveNodeText(xmlquery, "id_meter");
                        IEnumerable<MercuryMeter.Mercury230_DatabaseSignals> meter_local =
                                    from meter in Meter230_arr
                                    where (meter.address.ToString() == add_meter) && (meter.serial_number.ToString() == id_meter)
                                    select meter;
                        if (meter_local.Count() > 0)
                        {
                            answerBuilder.Append("<answer>");
                            foreach (Mercury230_DatabaseSignals el in meter_local)
                            {
                                answerBuilder.Append("<meter><![CDATA[");
                                //BinaryFormatter serializer = new BinaryFormatter();
                                using (MemoryStream stream = new MemoryStream())
                                {
                                    
                                    serializer.WriteObject(stream, el);
                                    var streamreader = new StreamReader(stream);
                                    stream.Position = 0;
                                    answerBuilder.Append(streamreader.ReadToEnd());
                                }
                                answerBuilder.Append("]]></meter>");
                            }
                            answerBuilder.Append("</answer>");
                            return answerBuilder.ToString();  // тут нельзя оставлять

                        }
                        else
                        {
                            return str_null;
                        }
                        break;

                }
                return answerBuilder.ToString();
            }
            else
            {
                return str_badformat;
            }
        }

        #endregion

        static void ReloadDataGrid(object sender, MercuryMeter.ReloadDataArgs e)
        {

            //console.WriteLine("{0} : {1}", e.addr, e.error);
            logger.Trace("Устройство {0} : {1}", e.addr, e.error);
        }

        /// <summary>
        /// Чтение конфигурационного файла
        /// </summary>
        static int readXMLdocument()
        {
            SerialPort rs_port = new SerialPort();
            XmlDocument xmlDocument = new XmlDocument();
            try
            {
            xmlDocument.Load("Meter_conf.xml");

            //meterTable = new DataTable("Meters");
            //meterTable.Columns.Add("Тип", typeof(string));
            //meterTable.Columns.Add("Адрес", typeof(string));
            //meterTable.Columns.Add("Статус", typeof(string));
            // rs_port = new System.IO.Ports.SerialPort();
            XmlNodeList devices = xmlDocument.SelectNodes("/Meters/DataBaseSQL");
            //Sqlhandler_ = new SQLhandler(devices[0].Attributes["Database"].Value, devices[0].Attributes["DataSource"].Value, devices[0].Attributes["UserId"].Value, devices[0].Attributes["Password"].Value);
            if (devices.Count > 0)
            {
              Sqlhandler_ = new SQLhandler(devices[0].Attributes["Database"].Value, devices[0].Attributes["DataSource"].Value, devices[0].Attributes["UserId"].Value, devices[0].Attributes["Password"].Value);
              progSettings.tblName_meter206 = devices[0].Attributes["tbl_206"].Value;
              progSettings.tblName_meter230 = devices[0].Attributes["tbl_230"].Value;
            }

            devices = xmlDocument.SelectNodes("/Meters/autostart");

            if (devices.Count > 0)
            {
                Console.WriteLine("Чтение параметров автозапуска");
                string com = devices[0].Attributes["defaultCOMport"].Value;
                progSettings.TCPport = Convert.ToInt32(devices[0].Attributes["TCPport"].Value);
                progSettings.timeoutSerial = Convert.ToInt32(devices[0].Attributes["timeout"].Value);
                Int32 baudrate = Convert.ToInt32(devices[0].Attributes["baudRate"].Value);
                System.IO.Ports.Parity parity = (Parity)Enum.Parse(typeof(Parity), devices[0].Attributes["parity"].Value, true);
                System.IO.Ports.StopBits stopbits = (StopBits)Enum.Parse(typeof(StopBits), devices[0].Attributes["stopBits"].Value, true);
                int dataBits = Convert.ToInt32(devices[0].Attributes["dataBits"].Value);
                rs_port = new System.IO.Ports.SerialPort(com, baudrate, parity, dataBits, stopbits);
                Console.WriteLine("Работа по последовательному порту " + com + "...");

               // progSettings.start = devices[0].Attributes["serverStart"].Value == "yes";

                //this.WindowState = WindowState.Normal;
            }

            int i = xmlDocument.SelectNodes("//meter[@type = 'Merc234']").Count;
            Meter230_arr = new Mercury230_DatabaseSignals[i];
            i = xmlDocument.SelectNodes("//meter[@type = 'Merc206']").Count;
            Meter206_arr = new Mercury206_Database[i];
            i = -1;

            foreach (XmlNode device in xmlDocument.SelectNodes("//meter[@type = 'Merc234']"))
                {

                        i++;
                        //meterTable.Rows.Add("Меркурий 234", device.Attributes["addr"].Value, "");
                        byte addr = Convert.ToByte(device.Attributes["addr"].Value);
                        string[] str_pasw_buf = device.Attributes["password_lvl1"].Value.Split(',');
                        byte[][] byte_pass = new byte[2][];
                        byte_pass[0] = new byte[str_pasw_buf.Length];
                        for (int j = 0; j < str_pasw_buf.Length; j++)
                        {
                            byte_pass[0][j] = Convert.ToByte(str_pasw_buf[j]);
                        }
                        str_pasw_buf = device.Attributes["password_lvl2"].Value.Split(',');
                        byte_pass[1] = new byte[str_pasw_buf.Length];
                        for (int j = 0; j < str_pasw_buf.Length; j++)
                        {
                            byte_pass[1][j] = Convert.ToByte(str_pasw_buf[j]);
                        }
                        int serialnumber = Convert.ToInt32(device.Attributes["id"].Value);
                        Meter230_arr[i] = new Mercury230_DatabaseSignals(rs_port, addr, serialnumber, byte_pass, progSettings.timeoutSerial);
                        Meter230_arr[i].ReloadData += ReloadDataGrid;
                    //    Meter230_arr[i].DataTime_nextPoint_recordSQL = DateTime.FromOADate(Sqlhandler_.NextTimetoSQLwrite(Meter230_arr[i], "meter230"));
                    
                }
            i = -1;
            foreach (XmlNode device in xmlDocument.SelectNodes("//meter[@type = 'Merc206']"))
            {
                i++;
                uint addr = Convert.ToUInt32(device.Attributes["addr"].Value);
                int serialnumber = Convert.ToInt32(device.Attributes["id"].Value);
                Meter206_arr[i] = new Mercury206_Database(rs_port, addr, serialnumber, progSettings.timeoutSerial);
               // Meter206_arr[i].ReloadData += ReloadDataGrid;
                
              //  Meter206_arr[i].DataTime_nextPoint_recordSQL = DateTime.FromOADate(Sqlhandler_.NextTimetoSQLwrite(Meter206_arr[i], "meter206", 0));
            }



                // XmlDocument xmlDocument = new XmlDocument();
                // xmlDocument.Load("Meter_conf.xml");

                // dataGrid_meter.ItemsSource = "Meters";

                Console.WriteLine("Файл считан...");
                return 0;
            }
            catch (Exception e)
            {
                logger.Error("Ошибка загрузки настроек: {0}", e.Message);
                return -1;
                //MessageBoxResult ti = MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                // ti = MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBoxResult ti = MessageBox.Show("Проблема при старте сервера", "Ошибка сервера", MessageBoxButton.OK, MessageBoxImage.Error);


                //if (MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
                //{
                //    return;
                //}
            }
        }

        static bool b_work = true;
        static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            
            logger.Error("{0}: {1}", sender, args.ExceptionObject);

        }

        static void RefreshMetersLimits()
        {
            refreshTimer.Enabled = false;
            if (Sqlhandler_ == null)
                return;
            try
            {
                var sqlhandlerlocal_ = new SQLhandler(Sqlhandler_.database, Sqlhandler_.DataSource, Sqlhandler_.UserId, Sqlhandler_.Password);
                //sqlhandler2_.myConnec.

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Mercury230_DatabaseSignals));

                foreach (Mercury230_DatabaseSignals meter in Meter230_arr)
                {
                    string sqlselect = String.Format("select * from dumpmeters where addr={0} and id={1}", meter.address, meter.serial_number);
                    DataTable dt = sqlhandlerlocal_.ReadSqlTable(sqlselect);
                    if (dt.Rows.Count > 0)
                    {
                        string objSerStr = dt.Rows[0]["dump"].ToString();
                        try
                        {
                            Mercury230_DatabaseSignals meterLocal =
                                                (Mercury230_DatabaseSignals)serializer.ReadObject(new System.IO.MemoryStream(Encoding.ASCII.GetBytes(objSerStr)));
                            meter.Phases[0].current.CopyLimits(meterLocal.Phases[0].current);
                            meter.Phases[1].current.CopyLimits(meterLocal.Phases[1].current);
                            meter.Phases[2].current.CopyLimits(meterLocal.Phases[2].current);
                            meter.Phases[0].voltage.CopyLimits(meterLocal.Phases[0].voltage);
                            meter.Phases[1].voltage.CopyLimits(meterLocal.Phases[1].voltage);
                            meter.Phases[2].voltage.CopyLimits(meterLocal.Phases[2].voltage);
                            meter.Phases[0].power.CopyLimits(meterLocal.Phases[0].power);
                            meter.Phases[1].power.CopyLimits(meterLocal.Phases[1].power);
                            meter.Phases[2].power.CopyLimits(meterLocal.Phases[2].power);
                            meter.CommonActivePower.CopyLimits(meterLocal.CommonActivePower);
                            meter.CommonPower.CopyLimits(meterLocal.CommonPower);
                        }
                        catch(Exception exc)
                        {
                            Console.WriteLine("Ошибка чтения БД : {0}", exc.Message);
                        }
                    }
                }
                //else
                //{
                //    MessageBox.Show("Будут взяты данные по умолчанию\n\r", "Нет информации в БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                //}
            }
            catch (Exception exc)
            {
                logger.Error("Ошибка при чтении БД " + exc.Message);
                return;
            }
            logger.Debug("-----------Лимиты счетчиков обновлены--------------");
        }

        
        static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);
           // Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);
            
            //Console.WriteLine(args[0]);
            if (readXMLdocument() == 0)
            {

                if (args.Length > 0)
                {
                    foreach (string arg in args)
                    {
                        if (arg == "-req:start")
                        {
                            progSettings.start = true;

                        }

                    }
                }


                if ((progSettings.TCPport >= 0) && progSettings.start)
                {
                    try
                    {
                        //logger.Debug("Попытка соединия соединия БД MySQL");
                        Sqlhandler_.ConnectOpen();
                        Sqlhandler_.ConnectClose();

                        logger.Debug("Попытка соединия соединия БД MySQL - Успешно");

                        MeterQuerier = new Task(loopMeterQuerier);
                        MeterQuerier.Start();

                        refreshTimer = new System.Timers.Timer(30000);
                        refreshTimer.AutoReset = false;
                        refreshTimer.Elapsed += new System.Timers.ElapsedEventHandler(refreshTimer_Elapsed);

                        refreshTimer.Enabled = true;

                        logger.Debug("Старт опроса");
                        Console.Beep(1000, 500);
                        server = new TCP_Server(answerMakerMetod);
                        server.Start(progSettings.TCPport);
                        logger.Debug("Запуск TCP севера. Порт: {0}", progSettings.TCPport);


                    }
                    catch (Exception ex)
                    {
                        logger.Error("{0}: {1}", ex.Source, ex.Message);
                        Console.ReadLine();
                        return;
                    }

                }
            }
                while (b_work){
                            if (Console.ReadLine() == "exit")
                            {
                                break;
                            }
                }
        }

        static void refreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            RefreshMetersLimits();
            refreshTimer.Enabled = true;
        }
    }
}
