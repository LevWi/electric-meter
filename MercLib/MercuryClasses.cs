using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
//using System.Xml.Serialization;
using System.Runtime.Serialization;



//public DataSet db = new DataSet("DataBaseSignal");

// public Mercury_DatabaseSignals() {

//    db = new DataSet("DataBaseSignal");
//    DataTable SignalTable = new DataTable("Signals");
//    DataColumn device_addr = new DataColumn("Address", typeof(byte));

//    device_addr.AllowDBNull = true;
//    device_addr.Unique = true;
//    DataColumn type = new DataColumn("type", typeof(object));
//    DataColumn power = new DataColumn("power", typeof(int[]));
//    DataColumn voltage = new DataColumn("voltage", typeof(float[]));
//    DataColumn current = new DataColumn("current", typeof(float[]));
//    DataColumn power_factor = new DataColumn("power_factor", typeof(float));
//    DataColumn frequency = new DataColumn("frequency", typeof(float[]));
//    DataColumn distortion_of_sinus = new DataColumn("distortion_of_sinus", typeof(float));
//    //DataColumn last_contact         = new DataColumn("last_contact", typeof(DateTime));
//    SignalTable.Columns.Add(device_addr);
//    SignalTable.Columns.Add(type);
//    SignalTable.Columns.Add(power);
//    SignalTable.Columns.Add(voltage);
//    SignalTable.Columns.Add(current);
//    SignalTable.Columns.Add(power_factor);
//    SignalTable.Columns.Add(frequency);
//    SignalTable.Columns.Add(distortion_of_sinus);
//    db.Tables.Add(SignalTable);
//}

// public int addNewMeter(Mercury230 obj)
// {
//     DataRow[] row = db.Tables["Signals"].Select("Address = " + obj.address.ToString());
//     if (row.Length > 0)
//     {
//         return -1;
//     }
//     db.Tables["Signals"].Rows.Add(obj.address, obj);
//     return 1;
// }


//public void writevalue(Mercury230 device, string param , params float[] arg)
//{

//    DataRow[] row = db.Tables["Signals"].Select("Address = " + device.address.ToString());
//    row[0][param] = arg;
//}


namespace MercuryMeter
{
    [DataContract]
    [Serializable]
    public class MeterDevice
    {
        public int timeout { get;  set; }
        [IgnoreDataMember]
        public System.IO.Ports.SerialPort rs_port { get; private set; }
        [IgnoreDataMember]
        public DateTime DataTime_last_contact { get; private set; }
        
        public enum error_type : int { none = 0, 
                                       AnswError = -5,  // вернул один или несколько ошибочных ответов
                                       CRCErr = -4, 
                                       NoAnsw = -2,    // ничего не ответил на запрос после коннекта связи
                                       WrongId = -3,   // серийный номер не соответствует 
                                       NoConnect = -1   // отсутствие ответа
                                      };
        public struct RXmes
        {
            public error_type err;
            public byte[] buff;
            public byte[] trueCRC;

            public void testCRC()
            {
                err = error_type.CRCErr;
                if (buff.Length < 4)
                {
                    err = error_type.CRCErr;
                    return;
                }
                byte[] newarr = buff;
                Array.Resize(ref newarr, newarr.Length - 2);
                byte[] trueCRC = Modbus.Utility.ModbusUtility.CalculateCrc(newarr);
                if ((trueCRC[1] == buff.Last()) && (trueCRC[0] == buff[(buff.Length - 2)]))
                {
                   err = error_type.none;
                }
            }
            public void ReadArr(byte[] b)
            {
                buff = b;
                testCRC();
            }
        }


        //public MeterDevice() { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="timeout">Время ожидания ответа от устройства</param>
        public MeterDevice(SerialPort port, int i_timeout)
        {
            rs_port = port;
            timeout = i_timeout;
        }



        public static int BCD_to_byte(byte bt)
        {
            int result = 0;
            result += (10 * (bt >> 4));
            result += (bt & 0xf);
            return result;
        }

        public static int byte_to_BCD(byte bt)
        {
            return ((bt / 10) << 4) + (bt % 10);
        }

        public RXmes SendCmd(byte[] data)
        {
            RXmes RXmes_ = new RXmes();
            byte[] crc = Modbus.Utility.ModbusUtility.CalculateCrc(data);
            Array.Resize(ref data, data.Length + 2);
            data[data.Length - 2] = crc[0];
            data[data.Length - 1] = crc[1];

            rs_port.Write(data, 0, data.Length);
            //  Отправляем запрос, ждем 100 мс и смотрим что пришло в ответ

            System.Threading.Thread.Sleep(timeout);

            if (rs_port.BytesToRead > 0)
            {
                byte[] answer = new byte[(int)rs_port.BytesToRead];
                //  Читаем буфер для анализа ответа на команду управления
               // если задержка по времени не верна rs_port.BytesToRead может измениться во время этой функции... Нужно предусмотреть
                rs_port.Read(answer, 0, rs_port.BytesToRead);
                RXmes_.ReadArr(answer);
                if (RXmes_.err == error_type.none)
                {
                DataTime_last_contact = DateTime.Now;
                }
                return RXmes_;
            }
            RXmes_.err = error_type.NoConnect;
            return RXmes_;
            //  Если не пришло вообще никакого ответа,
            //   возвращаем отрицательное значение
        }

        public int InitRs()
        {
            try
            {
                if (rs_port.IsOpen == true)
                    rs_port.Close();

                rs_port.Open();
            }
            catch
            {
                return -1;
            }
            return 0;
        }
    }
     public class Mercury206 : MeterDevice
    {
        public byte[] address { get; private set; }
        public Mercury206(SerialPort port, uint addr, int i_timeout = 50)
            : base(port, i_timeout)
        {
            byte[] add = new byte[4];
            for(int i = 0 ; i < 4 ; i++)
            {
                add[3-i] = (byte)((addr >> (8 * i)) & 0xff);
            }
           // address[0], address[1], address[2], address[3],

            address = add;
        }

        /// <summary>
        /// Чтение серийного номера
        /// </summary>
        /// <returns>возвращает массив 4 байта / </returns>
        public int GiveSerialNumber()
        {
            byte[] mes = { address[0], address[1], address[2], address[3], 0x2F };
            MeterDevice.RXmes RXmes = SendCmd(mes);
            if (RXmes.err == error_type.none)
            {
                byte[] bytebuf = new byte[4];
                Array.Copy(RXmes.buff, 5, bytebuf, 0, 4);

                return ((bytebuf[0] << 24) + (bytebuf[1] << 16) + (bytebuf[2] << 8) + bytebuf[3]);
            }
            return (int)RXmes.err;
        }


        /// <summary>
        /// Чтение содержимого тарифных аккумуляторов активной энергии
        /// </summary>
        /// <returns>Считанный массив. В случае ошибки - массив с одним элементом (кодом ошибки error_type) =  { (int)Rxmes_.err };</returns>
        public int[] CallAccumulEnergy_activ()
        {
            byte[] mes = { address[0], address[1], address[2], address[3], 0x27 };
            RXmes Rxmes_ = SendCmd(mes);
            if ((Rxmes_.err == error_type.none) || (Rxmes_.buff.Length > 7))
            {
                int[] intBuf = new int[(Rxmes_.buff.Length - 5 - 2)/4];

                return energy_byteArr_to_IntArr(Rxmes_.buff);
            }
            int[] z = { (int)Rxmes_.err };
            return z;
        }

         /// <summary>
         /// Чтение месячных срезов 
         /// </summary>
        /// <param name="month">Младшая тетрада - месяц  0h…Bh (0 -январь … Bh - декабрь); 0Fh – текущий месяц</param>
        /// <returns>Считанный массив. В случае ошибки - массив с одним элементом (кодом ошибки error_type) =  { (int)Rxmes_.err };</returns>
        public int[] CallAccumulEnergy_activ_month(byte month)
        {
            byte[] mes = { address[0], address[1], address[2], address[3], 0x32, month };
            RXmes Rxmes_ = SendCmd(mes);
            if ((Rxmes_.err == error_type.none) || (Rxmes_.buff.Length > 7))
            {
                int[] intBuf = new int[(Rxmes_.buff.Length - 5 - 2) / 4];

                return energy_byteArr_to_IntArr(Rxmes_.buff);
            }
            int[] z = { (int)Rxmes_.err };
            return z;
        }       

        private int[] energy_byteArr_to_IntArr(byte[] buff)
        {
            int[] intBuf = new int[(buff.Length - 5 - 2) / 4];
            for (int i = 0; i < intBuf.Length; i++)
            {
                byte[] newbyte = { buff[4 * i + 5], buff[4 * i + 6], buff[4 * i + 7], buff[4 * i + 8] };
                for (int j = 0; j < newbyte.Length; j++)
                {
                    newbyte[j] = (byte)BCD_to_byte(newbyte[j]);
                }
                intBuf[i] = ((newbyte[0] << 24) + (newbyte[1] << 16) + (newbyte[2] << 8) + newbyte[3]) * 10;
                
            }

            return intBuf;
        }

        public struct elecMeterDateTime
        {
            public const byte holiday = 7;
            public DateTime dt;
            public error_type error;
            public byte day;

        }
        /// <summary>
        /// Чтение внутренних часов и календаря счетчика
        /// </summary>
        /// <returns></returns>
        public elecMeterDateTime CallDateTime()
        {
            byte[] mes = { address[0], address[1], address[2], address[3], 0x21 };
            RXmes Rxmes_ = SendCmd(mes);
            elecMeterDateTime date_struct = new elecMeterDateTime();
            
            if (Rxmes_.err == error_type.none)
            {
                for (int i = 0; i < Rxmes_.buff.Length; i++)
                {
                    Rxmes_.buff[i] = (byte)BCD_to_byte(Rxmes_.buff[i]);
                }
                // 5   6  7 8   9 10  11
                //dow-hh-mm-ss-dd-mon-yy
                date_struct.dt = new DateTime(Rxmes_.buff[11]+2000, Rxmes_.buff[10], Rxmes_.buff[9], Rxmes_.buff[6], Rxmes_.buff[7], Rxmes_.buff[8]);
                date_struct.day = Rxmes_.buff[5];
                
            }

            date_struct.error = Rxmes_.err;
            return date_struct;
        }

        /// <summary>
        /// Установка внутренних часов и календаря счетчика
        /// </summary>
        /// <param name="dt">Устанавливаемое время</param>
        /// <returns></returns>
        public error_type SetTime(DateTime dt)
        {
            // 5   6  7 8   9 10  11
            //dow-hh-mm-ss-dd-mon-yy
            byte dow = (byte)byte_to_BCD((byte)dt.DayOfWeek);
            byte hh = (byte)byte_to_BCD((byte)dt.Hour);
            byte mm = (byte)byte_to_BCD((byte)dt.Minute);
            byte ss = (byte)byte_to_BCD((byte)dt.Second);
            byte dd = (byte)byte_to_BCD((byte)dt.Day);
            byte mon = (byte)byte_to_BCD((byte)dt.Month);
            byte yy = (byte)byte_to_BCD((byte)(dt.Year - dt.Year / 100 * 100));
            byte[] mes = { address[0], address[1], address[2], address[3], 0x02, dow, hh, mm, ss, dd, mon, yy};
            RXmes Rxmes_ = SendCmd(mes);
            return Rxmes_.err;
        }

    }

     [DataContract][Serializable]
    public class Mercury230 : MeterDevice
    {
        
        //0 - норма
        //1- ошибка контрольной связи
        //2 - отказ устройства
        //-1  - нет связи
        //public enum error_type : int { none = 0, CRCErr = 1, NoAnsw = 2, WrongId = 3, NoConnect = -1 };
        public byte address { get; private set; }
        public string ReadLocationMeter()
        {
            byte[] mes = { address, 0x08, 0x0B };
            RXmes Rxmes_ = SendCmd(mes);

            if ((testAnswer(Rxmes_) == 0) && (Rxmes_.buff.Length > 5))
            {
                byte[] arr = { Rxmes_.buff[1], Rxmes_.buff[2], Rxmes_.buff[3], Rxmes_.buff[4] };
                if ((arr[0] + arr[1] + arr[2] + arr[3]) == 0)
                {
                    return "none";
                }
                return System.Text.Encoding.ASCII.GetString(arr); ;
            }
            return "err";
        }
        
        public int explainAnswer(RXmes RXmes_)
        {
            if ((RXmes_.err != error_type.CRCErr) && (RXmes_.err != error_type.NoConnect))
            {
                if ((RXmes_.buff[0] == address))
                {
                    return RXmes_.buff[1];
                }
            }
            return -1;
        }

        public struct accumulEnergy
        {
            public long active_energy_in;
            public long active_energy_out;
            public long reactive_energy_in;
            public long reactive_energy_out;
            public int error;
            //public long replaceBytes(byte[] arr)
            //{
            //    if (arr.Length == 4)
            //    {
            //        return (arr[2] << 24) + (arr[3] << 16) + (arr[0] << 8) + arr[1]; 
            //    }
            //    return -1;
            //}
        }
        public struct elecMeterDateTime
        {
            public enum seasons : byte { summer = 0, winter = 1 } ;
            public DateTime datetime;
            public seasons season;
            public int numDay;


            public int numDayofWeek()
            {
                return numDayofWeek(datetime);
            }
            public int numDayofWeek(DateTime dat)
        {
            
            switch (dat.DayOfWeek)
            {
                case DayOfWeek.Monday: return 0+1;
                case DayOfWeek.Tuesday: return 1+1;
                case DayOfWeek.Wednesday: return 2+1;
                case DayOfWeek.Thursday: return 3+1;
                case DayOfWeek.Friday: return 4+1;
                case DayOfWeek.Saturday: return 5+1;
                case DayOfWeek.Sunday: return 6+1;
            }
            return -1;
        }
            //public int numDayofWeekRus(DateTime dat)
            //{

            //    switch (dat.DayOfWeek)
            //    {
            //        case DayOfWeek.Monday:      return 0;
            //        case DayOfWeek.Tuesday:     return 1;
            //        case DayOfWeek.Wednesday:   return 2;
            //        case DayOfWeek.Thursday:    return 3;
            //        case DayOfWeek.Friday:      return 4;
            //        case DayOfWeek.Saturday:    return 5;
            //        case DayOfWeek.Sunday:      return 6;
            //    }
            //    return -1;
            //}

            public void dateFromAnswer(byte[] btarr)
            {
                int i = 0;
                byte[] massive = btarr;
                foreach (byte bt in btarr)
                {
                    massive[i] = (byte)BCD_to_byte(bt);
                    i++;
                }

                DateTime DT = new DateTime(2000 + massive[7], massive[6], massive[5], massive[3], massive[2], massive[1]);
                datetime = DT;
                season = (seasons)massive[8];
                numDay = massive[4];
            }
        }
        //public struct RXmes
        //{
        //    public error_type err;
        //    public byte[] buff;
        //    public byte[] trueCRC;

        //    public void testCRC()
        //    {
        //        err = error_type.CRCErr;
        //        byte[] newarr = buff;
        //        Array.Resize(ref newarr, newarr.Length - 2);
        //        byte[] trueCRC = Modbus.Utility.ModbusUtility.CalculateCrc(newarr);
        //        if ((trueCRC[1] == buff.Last()) && (trueCRC[0] == buff[(buff.Length - 2)]))
        //        {
        //            err = error_type.none;
        //        }
        //    }

        //    public void ReadArr(byte[] b)
        //    {
        //        buff = b;
        //        testCRC();
        //    }
        //}
        public enum peroidQuery : byte
        {
            afterReset = 0x0,
            thisYear = 1,
            lastYear = 2,
            thisMonth = 3, thisDay = 4, lastDay = 5,
            thisYear_beginning = 9,
            lastYear_beginning = 0x0A,
            thisMonth_beginning = 0x0B,
            thisDay_beginning = 0x0C,
            lastDay_beginning = 0x0D
        }

        error_type testAnswer(RXmes RXmes_)
        {
            if (RXmes_.err != error_type.NoConnect)
            {
                if ((RXmes_.buff[0] == address))
                {
                    return error_type.none;
                }
            }
            return RXmes_.err;
        }

        //public Mercury230() { }
        public Mercury230(SerialPort port, byte addr, int i_timeout = 50) : base(port , i_timeout) 
        {
            address = addr;
        }

        public error_type SetLocationMeter(string str)
        {
            //str = str;
            byte[] bytes = new byte[str.Length +3];
            bytes[0] = address;
            bytes[1] = 0x03;
            bytes[2] = 0x22;
            int i = 2;
            byte[] charr = Encoding.ASCII.GetBytes(str);
            foreach (char ch in charr)
            {
                i++;
                bytes[i] = charr[i-3];
            }
            RXmes Rxmes_ = SendCmd(bytes);
            return testAnswer(Rxmes_);
        }

  
        public error_type testCon()
        {
            byte[] mes = { address, 0 };
            error_type err = SendCmd(mes).err;
            return err;
        }
        public elecMeterDateTime CallTime()
        {
            elecMeterDateTime elecMeterDateTime_ = new elecMeterDateTime();
            byte[] mes = { address, 0x04, 0x00 };
            RXmes RXmes = SendCmd(mes);
            if (RXmes.err == error_type.none)
                        elecMeterDateTime_.dateFromAnswer(RXmes.buff);
            return elecMeterDateTime_;
        }

        public int SetTime(elecMeterDateTime datetime)
        {
            byte[] mes = new byte[11];
            mes[0] = address;
            mes[1] = 3 ;
            mes[2] = 0x0C;

            mes[3] = (byte)byte_to_BCD(Convert.ToByte(datetime.datetime.Second)) ;
            mes[4] = (byte)byte_to_BCD(Convert.ToByte(datetime.datetime.Minute)) ;
            mes[5] = (byte)byte_to_BCD(Convert.ToByte(datetime.datetime.Hour)) ;
                                                                                 
            mes[6] = (byte)datetime.numDayofWeek();                                                             
                                                                                 
            mes[7] = (byte)byte_to_BCD(Convert.ToByte(datetime.datetime.Day)) ;
            mes[8] = (byte)byte_to_BCD(Convert.ToByte(datetime.datetime.Month)) ;

            int year = datetime.datetime.Year;
            year = year -  (year / 100 * 100);

            mes[9] = (byte)byte_to_BCD(Convert.ToByte(year));

            mes[10] = (byte)datetime.season;

            return explainAnswer(SendCmd(mes));
        }

        public int CorrectionTime(TimeSpan time)
        {
            byte[] mes = new byte[6];
            mes[0] = address;
            mes[1] = 3;
            mes[2] = 0x0D;

            mes[3] = (byte)byte_to_BCD(Convert.ToByte(time.Seconds));
            mes[4] = (byte)byte_to_BCD(Convert.ToByte(time.Minutes));
            mes[5] = (byte)byte_to_BCD(Convert.ToByte(time.Hours));

            return explainAnswer(SendCmd(mes));
        }


        public enum power : byte { P=0 , Q=1, S=2}
        public enum BWIR_param : byte
        {
            power = 0 ,
            voltage = 1,
            current =2 ,
            power_factor = 3 ,
            frequency = 4 ,
            distortion_of_sinus = 5
        }


        public enum AdditionalParameters_query : byte {h14=0x14,h16=0x16,h11=0x11}


        /// <summary>
        /// Запрос дополнительных параметров мощности, тока, коэффициента мощности
        /// </summary>
        /// <param name="param">тип перечисления BWIR_param</param>
        /// <param name="Num_Of_Phase">Номер фазы 1, 2, 3. Для Мощности и коэффициента мощности 0 - по сумме фаз</param>
        /// <param name="query">Номер функции для запроса. 14, 16 , 11 . По умолчанию 16
        ///                     14 - чтение зафиксрованных данных</param>
        /// <param name="powerType">The param Для чтения мощности - P, Q или S</param>
        /// <returns>Возвращает параметры Сумма, Фаза1, Фаза2, Фаза3. Или Значение для одной фазы</returns>
        /// 
        ///
        public int[] CallAdditionalParameters( BWIR_param param, byte Num_Of_Phase, AdditionalParameters_query query = AdditionalParameters_query.h16, power powerType = power.P )
        {
            
            byte[] mes = new byte[4];
            mes[0] = address;
            mes[1] = 0x08;
            mes[2] = (byte)query;
            if (param == BWIR_param.frequency)
                Num_Of_Phase = 0 ;
            mes[3] += (byte)(((byte)param << 4) + ((byte)powerType << 2) + Num_Of_Phase);
            RXmes Rxmes_ = SendCmd(mes);
            if (Rxmes_.buff != null)
            {
                if ( ( (Rxmes_.buff.Length - 3) % 4) > 0 ) 
                {
                    int[] j = new int[(Rxmes_.buff.Length - 3) / 3];
                    for (int i = 0; i < j.Length; i++)
                    {
                        byte[] newbyte = { Rxmes_.buff[3 * i + 1], Rxmes_.buff[3 * i + 2], Rxmes_.buff[3 * i + 3]};

                        j[i] = (newbyte[0] << 16) + (newbyte[2] << 8) + newbyte[1];
                    }
                    return j;
                }
                else if ((Rxmes_.buff.Length - 3) == 12) // ответ включает Сумма + 1ф + 2ф + 3ф  = 12байт 
                {
                    int[] j = new int[4];
                    for (int i = 0; i < j.Length; i++)
                    {
                        byte[] newbyte = { Rxmes_.buff[3 * i + 1], Rxmes_.buff[3 * i + 2], Rxmes_.buff[3 * i + 3] };

                        // 6 бит 1 байта данных - направление реактивной мощности этот бит обнуляем
                        // 7 бит - направление активной мощности . 
                        j[i] = ((newbyte[0] & 128) == 1 ? -1 : 1 ) * ((newbyte[0] & 0x3F) << 16) + (newbyte[2] << 8) + newbyte[1];
                        
                    }
                    return j;
                }
                else
                {
                    int[] j = new int[(Rxmes_.buff.Length - 3) / 4];
                    for (int i = 0; i < j.Length; i++)
                    {
                        byte[] newbyte = { Rxmes_.buff[4 * i + 1], Rxmes_.buff[4 * i + 2], Rxmes_.buff[4 * i + 3], Rxmes_.buff[4 * i + 4] };

                        j[i] = (newbyte[1] << 24) + (newbyte[0] << 16) + (newbyte[3] << 8) + newbyte[2];
                    }
                    return j;
                }
                
            }
            return null;
        }



        public accumulEnergy CallAccumulEnergy(peroidQuery period, byte tariff, byte month = 1) // запрос накопленной энергии
        {
            // tariff = 0 - работа по всем тарифам ;
            byte[] mes = new byte[4];
            mes[0] = address; mes[1] = 5;
            mes[2] = (byte)(((byte)period << 4) | month); mes[3] = tariff;
            RXmes Rxmes_ = SendCmd(mes);
            accumulEnergy EnergyStruct = new accumulEnergy();

            if (Rxmes_.err == error_type.none)
            {
                if (Rxmes_.buff.Length > 16)
                {
                    long[] longBuf = new long[4];
                    for (int i = 0; i < 4; i++)
                    {
                        byte[] newbyte = { Rxmes_.buff[4 * i + 1], Rxmes_.buff[4 * i + 2], Rxmes_.buff[4 * i + 3], Rxmes_.buff[4 * i + 4] };

                        longBuf[i] = (newbyte[1] << 24) + (newbyte[0] << 16) + (newbyte[3] << 8) + newbyte[2];
                    }
                    EnergyStruct.active_energy_in = longBuf[0];
                    EnergyStruct.active_energy_out = longBuf[1];
                    EnergyStruct.reactive_energy_in = longBuf[2];
                    EnergyStruct.reactive_energy_out = longBuf[3];
                    EnergyStruct.error = 0;
                    return EnergyStruct;
                }
            }
            EnergyStruct.error = -1;
            return EnergyStruct;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public int OpenChannel(byte level, byte[] password)
        {

            byte[] mes = new byte[password.Length + 3];
            mes[0] = address;
            mes[1] = 1; // байт запроса
            mes[2] = level;
            Array.Copy(password, 0, mes, 3, password.Length);
            RXmes RXmes = SendCmd(mes);
            return explainAnswer(RXmes);
        }

        /// <summary>
        /// Получение серийного номера и даты выпуска
        /// </summary>
        /// <returns>Возвращает массив байт. Первые 3 байта - дата выпуска. Далее серийный номер</returns>
        public byte[] GiveSerialNumber()
        {
            byte[] mes = {address, 0x08 , 0};
            RXmes RXmes = SendCmd(mes);
            if (RXmes.err == error_type.none) {
                byte[] bytebuf = new byte[7];
                Array.Copy(RXmes.buff, 1, bytebuf, 0, 7);
                return bytebuf;
            }
            return null;
        }


        }


    public class ReloadDataArgs : EventArgs
    {
        public int addr { get; set; }
        public  Mercury230.error_type error { get; set; }
    }

    [Serializable][DataContract]
    public class Mercury230_DatabaseSignals : Mercury230
    {
        public event EventHandler<ReloadDataArgs> ReloadData;

        protected virtual void OnThresholdReached(ReloadDataArgs e)
        {
            EventHandler<ReloadDataArgs> handler = ReloadData;
            
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public TimeSpan lostconnect_timeout = new TimeSpan(0, 0, 10);
        public int level = 2;
        public byte[][] password ;
        [DataMember]
        public int serial_number { private set; get; }
        [DataMember]
        public error_type error_meter = 0;
        [DataMember]
        public DateTime DateTime_lastTime_connection;
        public DateTime DateTime_nextTime_corecction;
        public DateTime DataTime_nextPoint_recordSQL;
        [DataMember]
        public MetersParameter CommonPower;
        [DataMember]
        public MetersParameter CommonActivePower;

        //public DateTime DataTime_last_contact;

        //public int[] powerbuf;
        //public float[] voltage;
        //public float[] current;
        //public float[] power_factor; // 4 элемента - По сумме фаз + 1ф+2ф+3ф
        [DataMember]
        public float frequency;
        public float[] distortion_of_sinus;
        [DataMember]
        public Phase[] Phases; // У счетчика 3 фазы

        //public Mercury230_DatabaseSignals(){}
        public Mercury230_DatabaseSignals(SerialPort port, byte addr, int serial, byte[][] pas, int timeout = 50)
            : base(port, addr, timeout)
        {
            serial_number = serial;
            DataTime_nextPoint_recordSQL = new DateTime(2000, 1, 1); ;
            this.password = pas;
            Phases = new Phase[3];
            Phases[0] = new Phase();
            Phases[1] = new Phase();
            Phases[2] = new Phase();
            CommonPower = new MetersParameter(-0.01F, 5000, 5); 
            DateTime_lastTime_connection = new DateTime(2000, 1, 1);
            DateTime_nextTime_corecction = new DateTime(2000, 1, 1);
            DataTime_nextPoint_recordSQL = new DateTime(2000, 1, 1);
            CommonActivePower = new MetersParameter(-0.01F, 5000, 50);
        }
        public error_type RefreshData()
        {
            InitRs();
            //try
            //{
            //    if (rs_port.IsOpen == false)
            //    if (rs_port.IsOpen == true)
            //        rs_port.Close();

            //    rs_port.Open();
            //}
            //catch
            //{
            //    return -1;
            //}
            //return 0;
            DateTime_lastTime_connection = DateTime.Now;
            ReloadDataArgs args = new ReloadDataArgs();
            args.addr = address;

            if (testCon() == 0)
            {
                //Console.WriteLine("Тест связи успешен");

                
                if (OpenChannel(Convert.ToByte(level), password[level-1]) == 0)
                {

                    //Console.WriteLine("Доступ разрешен");
                    byte[] btbuf = GiveSerialNumber();
                    //Console.WriteLine("Серийный номер");
                    if (btbuf == null)
                    {
                        args.error = error_type.NoConnect;
                        OnThresholdReached(args);
                        error_meter = error_type.NoConnect;
                        return error_type.NoConnect;
                    }
                    args.error = error_type.none;
                    error_meter = error_type.none;
                    try
                    {
                        int buf = 0;

                        for (int z = 0; z < 4; z++)
                        {
                            buf += (int)((double)btbuf[3 - z] * System.Math.Pow(10, 2 * z));
                        }
                        if (buf != serial_number)
                        {

                            args.error = error_type.WrongId;
                            error_meter = error_type.WrongId;
                            OnThresholdReached(args);
                            return error_type.WrongId;
                        }

                        int[] int1 = CallAdditionalParameters(Mercury230.BWIR_param.voltage, 1, Mercury230.AdditionalParameters_query.h16);
                        Console.Write("Вольтаж:");
                        //voltage = new float[3];
                        //int i = -1;


                        for(int i = 0 ; i <= 2 ; i++)
                        {
                            Phases[i].voltage.Value = Convert.ToSingle(int1[i]) / 100;
                            Console.Write(" {0}-{1} ", i, Phases[i].voltage.Value);
                        }
                        Console.WriteLine();

                        int1 = CallAdditionalParameters(Mercury230.BWIR_param.current, 1, Mercury230.AdditionalParameters_query.h16);
                        Console.Write("Амперы:");
                        //current = new float[3];
                        //i = -1;

                        for(int i = 0 ; i <= 2 ; i++)
                        {
                            Phases[i].current.Value = Convert.ToSingle(int1[i]) / 1000; //правка от 07.09.2016  показания тока делим не на 100 а на 1000
                            Console.Write(" {0}-{1} ", i, Phases[i].current.Value);
                        }
                        Console.WriteLine();

                        int1 = CallAdditionalParameters(Mercury230.BWIR_param.frequency, 0);
                        frequency = Convert.ToSingle(int1[0]) / 100;
                        Console.WriteLine("Частота:{0}" , frequency);


                        int1 = CallAdditionalParameters(Mercury230.BWIR_param.power_factor, 0, Mercury230.AdditionalParameters_query.h16);
                        //power_factor = new float[int1.Length];
                        //Console.WriteLine("Масссив размером: {0}", int1.Count());
                        Console.Write("Коэфф мощности:");
                        for (int i = 1; i <= 3; i++)
                        {
                            // Ответ происходит по виду Сумма - 1ф -2ф -3ф . Потому сдвигаем на один индекс
                            // чтобы получить - 1ф -2ф -3ф
                            Phases[i-1].power_factor.Value = Convert.ToSingle(int1[i]) / 1000;
                            Console.Write(" {0}-{1} ", i, Phases[i-1].power_factor.Value);
                        }

                        CommonPower.Value = 0;
                        for (int i = 0; i < 3; i++)
                        {
                            CommonPower.Value += Phases[i].power.Value;
                        }

                        CommonActivePower.Value = 0;
                        for (int i = 0; i < 3; i++)
                        {
                            CommonActivePower.Value += Phases[i].power.Value * Phases[i].power_factor.Value;
                        }

                        Console.WriteLine();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Ошибка при ответе:{0}", e.Message);
                        args.error = error_type.AnswError;
                        error_meter = error_type.AnswError;
                        return error_meter;
                    }

                    //DataTime_last_contact = DateTime.Now;
                    
                    OnThresholdReached(args);
                    error_meter = error_type.none; 
                    return error_type.none;

                }
               // Console.WriteLine("{0} : отказ", address);
                args.error = error_type.NoAnsw;
                error_meter = error_type.NoAnsw; 
                OnThresholdReached(args);
                return error_type.NoAnsw;
            }
           // Console.WriteLine("Ошибка");
            args.error = error_type.NoConnect;
            error_meter = error_type.NoConnect; 
            OnThresholdReached(args);
            return error_type.NoConnect;

        }

    }
    public class Mercury206_Database : Mercury206
    {
        public DateTime DateTime_nextTime_corecction { set; get; }
        public DateTime DataTime_nextPoint_recordSQL { set; get; }
        public int serial_number { private set; get; }
        
        public int i_addr { 
            get{ 
             return (address[0] << 24) + (address[1] << 16) + (address[2] << 8) + address[3]; }
             } 
        
        public Mercury206_Database(SerialPort port, uint addr, int serial, int timeout = 50)
            : base(port, addr, timeout)
        {
            serial_number = serial;
            DataTime_nextPoint_recordSQL = new DateTime();
            
        }
    }
}
