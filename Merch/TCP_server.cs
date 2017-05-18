using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


class TCP_Server
{
        TcpListener server;
        public enum serverStatus : byte {START = 0, STOP = 1  }
        public serverStatus server_status;

        Func<string, string> answerMakerMetod;
        // Task task_Loop_ThreadOpen;

        public TCP_Server(Func<string, string> answer)
        {
            answerMakerMetod = answer;
        }

        void Loop_ThreadOpenner()
        {
            try
            {
                while (server_status == serverStatus.START)
                {
                    //int counter = 0;
                   // Console.Write("\nWaiting for a connection... ");

                    // При появлении клиента добавляем в очередь потоков его обработку.
                    ThreadPool.QueueUserWorkItem(ObrabotkaZaprosa, server.AcceptTcpClient());
                    // Выводим информацию о подключении.
                    //counter++;
                    //Console.WriteLine("Connection from TCP...");
                }
            }
            catch
            {
                return;
            }
        }
    
        public void Start(int port )
        {

        
            server = null;
            try
            {
                // Определим нужное максимальное количество потоков
                // Пусть будет по 4 на каждый процессор
                int MaxThreadsCount = Environment.ProcessorCount * 4;
                Console.WriteLine("Максимальное кол-во потоков: " + MaxThreadsCount.ToString());
                // Установим максимальное количество рабочих потоков
                ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);
                // Установим минимальное количество рабочих потоков
                ThreadPool.SetMinThreads(2, 2);


                // Устанавливаем порт для TcpListener = 9595.
                
                //IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                
                server = new TcpListener(IPAddress.Any, port);

                // Запускаем TcpListener и начинаем слушать клиентов.
                server.Start();
                server_status = serverStatus.START;
                // Принимаем клиентов в бесконечном цикле.
                Task task_Loop_ThreadOpen = new Task(Loop_ThreadOpenner);
                task_Loop_ThreadOpen.Start();
                
            }
            catch (SocketException e)
            {
                //В случае ошибки, выводим что это за ошибка.
                Console.WriteLine("SocketException: {0}", e);
                // Останавливаем TcpListener.
                Stop();

                
            }

        }

        public void Stop()
        {
            server_status = serverStatus.STOP;
       //     task_Loop_ThreadOpen.Dispose();
        //    task_Loop_ThreadOpen.Wait();
            server.Stop();
            
        }
        
        
        public void ObrabotkaZaprosa(object client_obj)
        {
            // Буфер для принимаемых данных.
            byte[] bytes = new byte[256];
            string data = null;
            
            //Можно раскомментировать Thread.Sleep(1000); 
            //Запустить несколько клиентов
            //и наглядно увидеть как они обрабатываются в очереди. 
            //Thread.Sleep(1000);

            TcpClient client = client_obj as TcpClient;
            client.ReceiveTimeout = 5000; 
            data = null;
            
            // Получаем информацию от клиента
            NetworkStream stream = client.GetStream();

            int i;

            // Принимаем данные от клиента в цикле пока не дойдём до конца.
            try
            {
                while ((server_status == serverStatus.START) && ((i = stream.Read(bytes, 0, bytes.Length)) != 0))
                {
                    // Преобразуем данные в ASCII string.
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                    //Формеруем ответ с помощью внешней функции 
                    data = answerMakerMetod(data);

                    // Преобразуем полученную строку в массив Байт.
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                    // Отправляем данные обратно клиенту (ответ).
                    stream.Write(msg, 0, msg.Length);

                }
            }
            catch (System.IO.IOException)
            {
                client.Close();
            }

            // Закрываем соединение.
            client.Close();
            
        }
}