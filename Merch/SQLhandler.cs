using MySql.Data.MySqlClient;
using System.Data;
using MercuryMeter;
using System;
//using ConsoleApplication1.Program;

class SQLhandler
{
    public MySqlConnection myConnec;
    //private MySqlCommand myCommand;
    public string database    ;
    public string DataSource  ;
    public string UserId      ;
    public string Password    ;


    public SQLhandler(string database, string DataSource, string UserId, string Password)
    {
        string Connect = "Database=" + database + ";Data Source=" + DataSource + ";User Id=" + UserId + ";Password=" + Password;
        this.database    = database    ;
        this.DataSource  = DataSource  ;
        this.UserId      =  UserId      ;
        this.Password =     Password;
        myConnec = new MySqlConnection(Connect);
    }
    //public SQLhandler(string Connect)
    //{
    //    myConnec = new MySqlConnection(Connect);
    //}

   // DataTable dtable = new DataTable();

    public int SendData(string command)
    {
       ConnectionState firstSTate = ConnectionState.Open;
        if (myConnec.State != ConnectionState.Open)
        {
            myConnec.Open(); 
            firstSTate = ConnectionState.Closed;
        }
        MySqlCommand myCommand = new MySqlCommand(command, myConnec);
        myCommand.ExecuteNonQuery();
        if (firstSTate == ConnectionState.Closed)
        {
            myConnec.Close();
        }
        return 1;
    }

    /// <summary>
    /// Запись помесячных срезов со счетчика
    /// </summary>
    /// <param name="buffer">`tarif1`, `tarif2`, `tarif3`, `tarif4`</param>
    /// <param name="device"></param>
    /// <param name="period">0 - содержимого тарифных аккумуляторов активной энергии
    ///                      01...12 номер месяца. Например 01 - на начало января</param>
    public void writeAccumulEnergy(int[] buffer,  Mercury206_Database device, byte period)
    {
        DateTime datetime = new DateTime();
        if (period == 0)
        {
            datetime = DateTime.Now;
        }
        else
        {
            datetime = (period > DateTime.Now.Month) ? new DateTime(DateTime.Now.AddYears(-1).Year, period, 1, 0, 0, 0) : new DateTime(DateTime.Now.Year, period, 1, 0, 0, 0);
        }
        string CommandText = "INSERT INTO meter206 ( `addr`, `tarif1`, `tarif2`, `tarif3`, `tarif4`, `oleDT` , `id`, `period`) VALUES (" + device.i_addr + ", " + buffer[0] + ", " + buffer[1] + ", " + buffer[2] + ", " + buffer[3] + ", " + datetime.ToOADate() + ", " + device.serial_number + ", " + period + " )";
        SendData(CommandText);
    }

    public void writeAccumulEnergy(Mercury230.accumulEnergy energystruct, int tarif, Mercury230.peroidQuery period, Mercury230_DatabaseSignals device, byte month)
    {
        DateTime datetime = new DateTime();
        if (month == 0)
        {
            datetime = DateTime.Now;
        }
        else
        {
            datetime = (month > DateTime.Now.Month) ? new DateTime(DateTime.Now.AddYears(-1).Year, month, 1, 0, 0, 0) : new DateTime(DateTime.Now.Year, month, 1, 0, 0, 0);
        }
        string CommandText = "INSERT INTO meter230 ( `addr`, `energy_active_in`, `energy_reactive_in`, `energy_reactive_out`, `oleDT` , `tariff`, `period` , `id`, `month`) VALUES (" + device.address.ToString() + ", " + energystruct.active_energy_in + ", " + energystruct.reactive_energy_in + ", " + energystruct.reactive_energy_out + ", " + datetime.ToOADate() + ", " + tarif.ToString() + ", '" + Convert.ToString((int)period) + "', " + device.serial_number.ToString() + " , " + month + " )";
       SendData(CommandText);

    }

    public void ConnectClose()
    {
        if (myConnec.State != ConnectionState.Closed)
        {
            myConnec.Close();
        }
    }

    public void ConnectOpen()
    {
        if (myConnec.State != ConnectionState.Open)
        {
            myConnec.Open();
        }
    }


    
    /// <summary>
    /// Получить таблицу записей за конкретные период
    /// </summary>
    /// <param name="device"></param>
    /// <param name="table"></param>
    /// <param name="period"></param>
    /// <returns></returns>
    public DataTable GiveSQLrecordingPeriod(Mercury206_Database device, string table, byte period)
    {
        ConnectionState firstSTate = ConnectionState.Open;
        DateTime dt = DateTime.Now;
        try
        {
            if (myConnec.State != ConnectionState.Open)
            {
                myConnec.Open();
                firstSTate = ConnectionState.Closed;
            }

            //DateTime dt_beginday = (period == 0) ? new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0) : new DateTime(dt.Year, 1, 1, 0, 0, 0);
            DateTime dt_beginday = new DateTime();
            DateTime dt_ending = new DateTime();
            if (period == 0)
            {
                dt_beginday = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                dt = DateTime.Now.AddDays(1);
                dt_ending = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
            }
            else
            {
                dt_beginday = (period > dt.Month) ? new DateTime(dt.AddYears(-1).Year, period, 1, 0, 0, 0) : new DateTime(dt.Year, period, 1, 0, 0, 0);

                dt_ending = (period > dt.Month) ? new DateTime(dt.Year, 1, 1, 0, 0, 0) : new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
            }

            string CommandText = "SELECT * FROM " + table + "  WHERE `addr` =" + device.i_addr + " AND `oleDT` >= " + dt_beginday.ToOADate() + " AND `oleDT` <= " + dt_ending.ToOADate() + " AND `period` = " + period;
            
            MySqlCommand myCommand = new MySqlCommand(CommandText, myConnec);
            //myConnec.Open(); //Устанавливаем соединение с базой данных.
            DataTable dtable = new DataTable();
            MySqlDataReader dr = myCommand.ExecuteReader();
            dtable.Load(dr);
            if (firstSTate == ConnectionState.Closed)
            {
                myConnec.Close();
            }
            return dtable;
        }
        catch(Exception e)
        {
            Console.WriteLine("Ошибка БД {0}", e.Message);
            return null;
        }
    }

    public DataTable GiveSQLrecordingPeriod(Mercury230_DatabaseSignals device, string table, Mercury230.peroidQuery period, byte month, byte tariff)
    {
        ConnectionState firstSTate = ConnectionState.Open;
        DateTime dt = DateTime.Now;
        try
        {
            if (myConnec.State != ConnectionState.Open)
            {
                myConnec.Open();
                firstSTate = ConnectionState.Closed;
            }

            DateTime dt_beginday = new DateTime();
            DateTime dt_ending = new DateTime();
              if (month == 0)
            {
                dt_beginday = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                dt = DateTime.Now.AddDays(1);
                dt_ending = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
            }
            else
            {
                dt_beginday = (month > dt.Month) ? new DateTime(dt.AddYears(-1).Year, month, 1, 0, 0, 0) : new DateTime(dt.Year, month, 1, 0, 0, 0);

                dt_ending = (month > dt.Month) ? new DateTime(dt.Year, 1, 1, 0, 0, 0) : new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
            }

            string CommandText = "SELECT * FROM " + table + "  WHERE `addr` =" + device.address.ToString() + " AND `oleDT` >= " + dt_beginday.ToOADate() + " AND `oleDT` <= " + dt_ending.ToOADate() + " AND `period` = " + (int)period + " AND `month` = " + month + " AND `tariff` = " + tariff ;
            MySqlCommand myCommand = new MySqlCommand(CommandText, myConnec);
            DataTable dtable = new DataTable();
            MySqlDataReader dr = myCommand.ExecuteReader();
            dtable.Load(dr);
            if (firstSTate == ConnectionState.Closed)
            {
                myConnec.Close();
            }
            return dtable;
        }
        catch(Exception e)
        {
            Console.WriteLine("Ошибка БД {0}", e.Message);
            return null;
        }


    }

    public DataTable ReadSqlTable(string CommandText)
    {
        ConnectionState firstSTate = ConnectionState.Open;
        if (myConnec.State != ConnectionState.Open)
        {
            myConnec.Open();
            firstSTate = ConnectionState.Closed;
        }
        //string CommandText = "SELECT * FROM " + table + "  WHERE `addr` =" + device.address.ToString() + " AND `oleDT` >= " + dt_start.ToOADate() + " AND `oleDT` <= " + dt_end.ToOADate() + " AND `period` = " + (int)period + " AND `month` = " + month + " AND `tariff` = " + tariff;
        MySqlCommand myCommand = new MySqlCommand(CommandText, myConnec);
        DataTable dtable = new DataTable();
        MySqlDataReader dr = myCommand.ExecuteReader();
        dtable.Load(dr);

        if (firstSTate == ConnectionState.Closed)
        {
            myConnec.Close();
        }
        return dtable;
    }
  
}