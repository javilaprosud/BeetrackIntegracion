using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Data.SqlClient;
using System.Data;

namespace BeetrackConsole
{
    class Program
    {
        public static string url;
        public static string enviarjson;

        static void Main(string[] args)
        {
           Guias();
           Routes();

   
        }

        public static void Guias()
        {
            url = "https://app.beetrack.com/api/external/v1/dispatch_guides"; 
            SqlConnection conn = new SqlConnection("Data Source=192.168.1.69;Initial Catalog=procesadorabd;uid=sa;Password=procesadora1");
            conn.Open();
            string consulta = "select sum(dlincantidad / UMPEquivalencia) as Cajas, a.HRLiRazonSoc as Cliente, a.DoctNumero as Documento , a.HRLiDirDesp as Direccion ";
            consulta = consulta + "from horulineas a inner join doclineas b ";
            consulta = consulta + "on a.doctnumero = b.doctnumero and a.TdocCodigo = b.TdocCodigo  and a.RelcRut = b.RelcRut and a.EmprCod = b.EmprCod ";
            consulta = consulta + "where a.HoRuNumero = (select BeetrHR from BeetrackLoad) ";
            consulta = consulta + "group by a.HRLiRazonSoc, a.DoctNumero, a.HRLiDirDesp ";
            consulta = consulta + "union all ";
            consulta = consulta + "select sum(b.OdliCantidad / UMPEquivalencia) as Cajas, a.HRLiRazonSoc as Cliente, a.DoctNumero as Documento , a.HRLiDirDesp as Direccion ";
            consulta = consulta + "from horulineas a inner join ODLineas b ";
            consulta = consulta + "on a.doctnumero = b.OdocNumero and a.TdocCodigo = b.TdocCodigo  and a.RelcRut = b.RelcRut and a.EmprCod = b.EmprCod ";
            consulta = consulta + "where a.HoRuNumero = (select BeetrHR from BeetrackLoad) ";
            consulta = consulta + "group by a.HRLiRazonSoc, a.DoctNumero, a.HRLiDirDesp ";

            SqlCommand command = new SqlCommand(consulta, conn);
            // int result = command.ExecuteNonQuery();
            SqlDataAdapter adapter = new SqlDataAdapter();
            adapter.SelectCommand = command;
            DataTable guiastabla = new DataTable();
            adapter.Fill(guiastabla);
            
            foreach(DataRow row in guiastabla.Rows)
                { 
                   
                    enviarjson = "{";
                    enviarjson = enviarjson + "\"id\": \""+ row["Documento"] + "\",";
                    enviarjson = enviarjson + "\"contact_name\": \""+ row["Cliente"] + "\",";
                    enviarjson = enviarjson + "\"contact_adress\":\""+row["Direccion"]+"\",";
                    enviarjson = enviarjson + "\"contact_phone\":\"\",";
                    enviarjson = enviarjson + "\"items\":[";
                    enviarjson = enviarjson + "{";
                    enviarjson = enviarjson + "\"name\": \"\",";
                    enviarjson = enviarjson + "\"quantity\": "+ row["Cajas"] + ",";
                    enviarjson = enviarjson + "\"price\":0,";
                    enviarjson = enviarjson + "\"code\": \"\"";
                    enviarjson = enviarjson + "}";
                    enviarjson = enviarjson + "],";
                    enviarjson = enviarjson + "\"name\": \"" + row["Direccion"] + "\"";
                    enviarjson = enviarjson + "}";
                    JsonBeetrack();
                }
        

        }

      public static void Routes()
        {
            url = "https://app.beetrack.com/api/external/v1/routes";
            SqlConnection conn = new SqlConnection("Data Source=192.168.1.69;Initial Catalog=procesadorabd;uid=sa;Password=procesadora1");
            conn.Open();
            string consulta = "select convert(varchar, a.HoRuFecha,105) as Fecha, a.vehiPatente as Patente, [BeetrMovil] as Movil, c.HRLiRazonSoc as Cliente, c.HRLiDirDesp as Direccion, c.DoctNumero as Documento from HojasRutas a inner join BeetrackLoad b  on  a.HoRuNumero = b.[BeetrHR] and a.EmprCod = b.EmprCod inner join HoRuLineas c on a.HoRuNumero = c.horunumero and a.EmprCod = c.emprcod";

            SqlCommand command = new SqlCommand(consulta, conn);
            // int result = command.ExecuteNonQuery();
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();
            enviarjson = "{";
            enviarjson = enviarjson + "\"truck_identifier\":\"" + reader["Movil"] + "\", ";
            enviarjson = enviarjson + "\"date\":\"" + reader["Fecha"] + "\", ";
            enviarjson = enviarjson + "\"driver_identifier\":\"" + reader["Patente"] + "\", ";
            enviarjson = enviarjson + "\"dispatches\":[ ";
            conn.Close();
            conn.Open();
            SqlDataAdapter adapter = new SqlDataAdapter();
            adapter.SelectCommand = command;
            DataTable guiastabla = new DataTable();
            adapter.Fill(guiastabla);
       
            foreach (DataRow row in guiastabla.Rows)
            {

                enviarjson = enviarjson + "{";
                enviarjson = enviarjson + "\"identifier\":"+ row["Documento"] + ", ";
                enviarjson = enviarjson + "\"contact_name\":\""+ row["Cliente"] + "\", ";
                enviarjson = enviarjson + "\"contact_address\":\"" + row["Direccion"] + "\", ";
                enviarjson = enviarjson + "\"estimated_at\":\"" + row["Fecha"] + "\" ";
                enviarjson = enviarjson + "} ";
                if (guiastabla.Rows.Count > 1)
                {
                    enviarjson = enviarjson + ",";
                }   

            }
            if (enviarjson.Substring(enviarjson.Length - 1, 1) == ",")
            {
                enviarjson = enviarjson.Substring(0, enviarjson.Length - 1);
                enviarjson = enviarjson + "], ";
                enviarjson = enviarjson + "\"name\":\"\" ";


                enviarjson = enviarjson + "}";
                JsonBeetrack();

            }
            else
            {
                enviarjson = enviarjson + "], ";
                enviarjson = enviarjson + "\"name\":\"\" ";


                enviarjson = enviarjson + "}";
                JsonBeetrack();
            }




        }

        public static void JsonBeetrack()
        {
            try
            {
 
                string clave = "f1d5d494febd65fbb4bdf6bdc6d010e5daff83a38fd3b75f27ef48db6be472d1";
                string webAddr = url;

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
                httpWebRequest.ContentType = "application/json; charset=utf-8";
                httpWebRequest.Headers.Add("X-AUTH-TOKEN", clave);
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = enviarjson;

                    streamWriter.Write(json);
                    streamWriter.Flush();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();
                    Console.WriteLine(responseText);


                    //Now you have your response.
                    //or false depending on information in the response     
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
                
            }
        }

    }
}
