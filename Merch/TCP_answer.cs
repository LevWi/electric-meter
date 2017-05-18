using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MercuryMeter;
using System.Runtime.Serialization.Json;
using System.IO;

namespace ConsoleApplication1
{
     public class  Program
    {
        //принцип запроса
        #region Генерация ответов по TCP

        static string answerMakerMetod(string args)
        {
            Func<XmlDocument,string,string> giveNodeText = (xml, param) => {
                try{
                return xml.SelectNodes("/query/" + param)[0].InnerText;
                }
                catch{
                    return "";
                }
            } ;

            string str_badformat = "<Error>badformat</Error>";
            string str_null = "<Error>null</Error>";
            StringBuilder answerBuilder = new StringBuilder();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Mercury230_DatabaseSignals));
            XmlDocument xmlquery = new XmlDocument();
            try
            {
                xmlquery.LoadXml(args);
            }
            catch(XmlException)
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
                        if (meter_local.ToList().Count > 0)
                        {
                            foreach (Mercury230_DatabaseSignals el in meter_local)
                            {
                                //BinaryFormatter serializer = new BinaryFormatter();
                                using (MemoryStream strim = new MemoryStream())
                                {
                                    serializer.WriteObject(strim, Meter230_arr[0]);
                                    answerBuilder.Append(System.Text.Encoding.UTF8.GetString(strim.GetBuffer()));
                                }
                            }
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


        //static string answerMakerMetod(string args)
        //{
        //    //MainWindow.Win.OnExplicitShutdown
        //    StringBuilder answerBuilder = new StringBuilder(1000, 1000);
        //    //string[] param_array = { "cur", "volt", "freq", "power", "powfact", "dist_sin" };


        //    string[] arg = args.Split('*');
        //    try
        //    {
        //        int add_device = Convert.ToInt32(give_arg("add", arg[0]));
        //        string type_device = give_arg("type", arg[1]);

        //        #region для 234го
        //        if (type_device == "Mer234")
        //        {
        //            int curdevice = -1;
        //            foreach (Mercury230_DatabaseSignals device in Meter230_arr)
        //            {
        //                curdevice++;
        //                if (device.address == (byte)add_device)
        //                    break;

        //            }
        //            answerBuilder.AppendFormat("add={0}", (byte)add_device);
        //            for (int i = 2; i < arg.Length; i++)
        //            {
        //                try
        //                {


        //                    switch (arg[i])
        //                    {
        //                        // case "cur":
        //                        //     answerBuilder.AppendFormat("*cur=1:{0}-2:{1}-3:{2}", Meter230_arr[curdevice].current[0], Meter230_arr[curdevice].current[1], Meter230_arr[curdevice].current[2]);
        //                        //     break;
        //                        // case "volt":
        //                        //     answerBuilder.AppendFormat("*volt=1:{0}-2:{1}-3:{2}", Meter230_arr[curdevice].voltage[0], Meter230_arr[curdevice].voltage[1], Meter230_arr[curdevice].voltage[2]);
        //                        //     break;
        //                        // case "freq":
        //                        //     answerBuilder.AppendFormat("*freq=:{0}", Meter230_arr[curdevice].frequency); break;
        //                        // case "power":
        //                        //     answerBuilder.AppendFormat("*power=S:{0}-1:{1}-2:{2}-3:{3}", Meter230_arr[curdevice].powerbuf[0], Meter230_arr[curdevice].powerbuf[1], Meter230_arr[curdevice].powerbuf[2] /*, Meter230_arr[curdevice].powerbuf[3]*/); break;
        //                        // case "powfact":
        //                        //     answerBuilder.AppendFormat("*powfact=S:{0}-1:{1}-2:{2}-3:{3}", Meter230_arr[curdevice].power_factor[0], Meter230_arr[curdevice].power_factor[1], Meter230_arr[curdevice].power_factor[2] /*, Meter230_arr[curdevice].power_factor[3]*/); break;
        //                        case "dist_sin":
        //                            answerBuilder.AppendFormat("*dist_sin=1:{0}-2:{1}-3:{2}", Meter230_arr[curdevice].distortion_of_sinus[0], Meter230_arr[curdevice].distortion_of_sinus[1], Meter230_arr[curdevice].distortion_of_sinus[2]); break;
        //                        case "lastCon":
        //                            CultureInfo culture = CultureInfo.CreateSpecificCulture("de-DE");
        //                            //string str = Meter230_arr[curdevice].DataTime_last_contact.ToString("G", culture).Replace(':', '$');

        //                            answerBuilder.AppendFormat("*lastCon={0}", Meter230_arr[curdevice].DataTime_last_contact.ToString("G", culture).Replace(':', '$')); break;
        //                    }
        //                }
        //                catch (NullReferenceException)
        //                {
        //                    answerBuilder.AppendFormat("*" + arg[i] + ":NULL");
        //                }
        //            }

        //            return answerBuilder.ToString();
        //        }
        //        #endregion
        //        #region для 206го
        //        if (type_device == "Mer206")
        //        {
        //            int curdevice = -1;
        //            foreach (Mercury206_Database device in Meter206_arr)
        //            {
        //                curdevice++;
        //                if (device.i_addr == add_device)
        //                    break;
        //            }
        //            answerBuilder.AppendFormat("add:{0}", add_device);
        //            for (int i = 2; i < arg.Length; i++)
        //            {
        //                try
        //                {


        //                    switch (arg[i])
        //                    {
        //                        //case "cur":
        //                        //    answerBuilder.AppendFormat("*cur-1:{0}-2:{1}-3:{2}", Meter230_arr[curdevice].current[0], Meter230_arr[curdevice].current[1], Meter230_arr[curdevice].current[2]);
        //                        //    break;
        //                        //case "volt":
        //                        //    answerBuilder.AppendFormat("*volt-1:{0}-2:{1}-3:{2}", Meter230_arr[curdevice].voltage[0], Meter230_arr[curdevice].voltage[1], Meter230_arr[curdevice].voltage[2]);
        //                        //    break;
        //                        //case "freq":
        //                        //    answerBuilder.AppendFormat("*freq-:{0}", Meter230_arr[curdevice].frequency); break;
        //                        //case "power":
        //                        //    answerBuilder.AppendFormat("*power-S:{0}-1:{1}-2:{2}-3:{3}", Meter230_arr[curdevice].powerbuf[0], Meter230_arr[curdevice].powerbuf[1], Meter230_arr[curdevice].powerbuf[2], Meter230_arr[curdevice].powerbuf[3]); break;
        //                        //case "powfact":
        //                        //    answerBuilder.AppendFormat("*powfact-S:{0}-1:{1}-2:{2}-3:{3}", Meter230_arr[curdevice].power_factor[0], Meter230_arr[curdevice].power_factor[1], Meter230_arr[curdevice].power_factor[2], Meter230_arr[curdevice].power_factor[3]); break;
        //                        //case "dist_sin":
        //                        //    answerBuilder.AppendFormat("*dist_sin-1:{0}-2:{1}-3:{2}", Meter230_arr[curdevice].distortion_of_sinus[0], Meter230_arr[curdevice].distortion_of_sinus[1], Meter230_arr[curdevice].distortion_of_sinus[2]); break;
        //                        case "lastCon":
        //                            CultureInfo culture = CultureInfo.CreateSpecificCulture("de-DE");
        //                            //string str = Meter230_arr[curdevice].DataTime_last_contact.ToString("G", culture).Replace(':', '$');

        //                            answerBuilder.AppendFormat("*lastCon-:{0}", Meter206_arr[curdevice].DataTime_last_contact.ToString("G", culture).Replace(':', '$')); break;
        //                    }
        //                }
        //                catch (NullReferenceException)
        //                {
        //                    answerBuilder.AppendFormat("*" + arg[i] + ":NULL");
        //                }
        //            }
        //            return answerBuilder.ToString();
        //        }
        //        #endregion
        //    }
        //    catch (FormatException)
        //    {
        //        return "err";
        //    }

        //    return "err";
        //}

        //static string give_arg(string par, string arg)
        //{
        //    string ret = "";
        //    try
        //    {
        //        string[] memb = arg.Split(':');
        //        if (memb[0] == par)
        //        {
        //            return memb[1];
        //        }
        //        ret = "error";
        //    }
        //    catch
        //    {
        //        return "error";
        //    }
        //    return ret;
        //}
        #endregion
    }
}