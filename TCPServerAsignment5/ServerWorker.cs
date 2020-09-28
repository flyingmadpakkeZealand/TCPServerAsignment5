using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CsharpUnitTestAsignment1;
using Newtonsoft.Json;

namespace TCPServerAsignment5
{
    class ServerWorker
    {
        private static Dictionary<uint, Bicycle> _bicycleDictionary = new Dictionary<uint, Bicycle>();

        private static object _dictionaryLock = new object();

        public void Start()
        {
            _bicycleDictionary.Add(0, new Bicycle("Sample", 0d, 3, 0));

            TcpListener listener = new TcpListener(IPAddress.Loopback, 4646);
            listener.Start();

            while (true)
            {
                TcpClient socket = listener.AcceptTcpClient();

                Task.Run(() => DoClient(socket));
            }
        }

        private void DoClient(TcpClient client)
        {
            bool disconnect = false;
            NetworkStream stream = client.GetStream();

            StreamReader sr = new StreamReader(stream);
            StreamWriter sw = new StreamWriter(stream);

            sw.WriteLine("You have connected to the Bicycle server, you can type HentAlle, Hent and Gem to manipulate Bicycle data. STOP to disconnect");
            sw.Flush();

            while (!disconnect)
            {
                string protocol = sr.ReadLine();
                switch (protocol)
                {
                    case "HentAlle":AllProtocol(sw); break;
                    case "Hent":OneProtocol(sw, sr); break;
                    case "Gem": SaveProtocol(sw, sr); break;
                    case "STOP": disconnect = Disconnect(sw); break;
                    default: sw.WriteLine($"{protocol} is an unsupported protocol, please use HentAlle, Hent or Gem. STOP to disconnect"); break;
                }

                sw.Flush();
            }

            client.Close();
        }

        private void AllProtocol(StreamWriter sw)
        {
            sw.WriteLine("You have chosen HentAlle, this protocol has no instructions and will return all Bicycle data");
            StringBuilder builder = new StringBuilder();
            lock (_dictionaryLock)
            {
                foreach (Bicycle bicycle in _bicycleDictionary.Values)
                {
                    builder.Append(JsonConvert.SerializeObject(bicycle) + "\n");
                }
            }

            sw.WriteLine(builder.ToString());
        }

        private void OneProtocol(StreamWriter sw, StreamReader sr)
        {
            sw.WriteLine("You have chosen Hent, this protocol requires you to provide an Id, please type a whole number");
            sw.Flush();

            uint id = GetId(sw, sr);
            
            Bicycle output;
            lock (_dictionaryLock)
            {
                if (_bicycleDictionary.ContainsKey(id))
                {
                    output = _bicycleDictionary[id];
                }
                else
                {
                    sw.WriteLine($"There is no Bicycle with Id: {id}");
                    return;
                }
            }
            sw.WriteLine(JsonConvert.SerializeObject(output));
        }

        private void SaveProtocol(StreamWriter sw, StreamReader sr)
        {
            sw.WriteLine("You have chosen Gem, this protocol requires you to provide a new bicycle object in Json format, please note the bicycle must have a unique Id\n" +
                         "syntax: {\"Id\":<uint>,\"Color\":<string>,\"Gear\":<byte>,\"Price\":<double>}");

            while (true)
            {
                sw.Flush();
                try
                {
                    Bicycle input = JsonConvert.DeserializeObject<Bicycle>(sr.ReadLine());

                    if (input != null)
                    {
                        lock (_bicycleDictionary)
                        {
                            if (!_bicycleDictionary.ContainsKey(input.Id))
                            {
                                _bicycleDictionary.Add(input.Id, input);
                                sw.WriteLine($"Bicycle with Id: {input.Id} was added successfully");
                                return;
                            }
                        }
                        sw.WriteLine($"The Id: {input.Id} of this Bicycle already exist");
                    }
                    else
                    {
                        sw.WriteLine("Please provide a Json string");
                    }
                }
                catch (JsonException)
                {
                    sw.WriteLine("Invalid Json string. Please check your syntax and try again");
                }
            }
        }

        private uint GetId(StreamWriter sw, StreamReader sr)
        {
            while (true)
            {
                string input = sr.ReadLine();
                try
                {
                    return Convert.ToUInt32(input);
                }
                catch (FormatException)
                {
                    sw.WriteLine("Please only provide a whole number");
                    sw.Flush();
                }
            }
        }

        private bool Disconnect(StreamWriter sw)
        {
            sw.WriteLine("Disconnecting");
            return true;
        }
    }
}
