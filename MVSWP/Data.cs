using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Xml;

namespace MVSWP
{
    public static class Data
    {
        static readonly string connectionString = @"Server=(LocalDB)\MSSQLLocalDB;Database=Registers;Trusted_Connection=True;";


        public static void LoadInfoToTemp(MemoryStream ms)
        {
            string st = Encoding.UTF8.GetString(ms.ToArray());
            st = st.Remove(0, 1);
            st = "{MVS_PASSPORTS_tmp: " + st + "}";
            XmlDocument xml = JsonConvert.DeserializeXmlNode(st, "MVS");

            DataSet ds = new DataSet();
            XmlReader xr = new XmlNodeReader(xml);
            ds.ReadXml(xr);
            try
            {
                for (int i = 0; i < ds.Tables.Count; i++)
                {
                    using SqlBulkCopy bulkCopy = new SqlBulkCopy(connectionString)
                    {
                        DestinationTableName = "[Registers].[dbo].[" + ds.Tables[i].TableName + "]",
                        BulkCopyTimeout = 999
                    };

                    bulkCopy.WriteToServer(ds.Tables[0]);

                    bulkCopy.Close();
                }
                ms.Dispose();
                ms.Close();
                ds.Dispose();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void LoadInfoToOriginal()
        {
            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand command = new SqlCommand("[Registers].[dbo].[LoadToOfflineHistory_mvs]", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 999
                };

                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static int CheckProcedure()
        {
            int count = 0;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM [Registers].[dbo].[MVS_PASSPORTS_tmp]", connection);
                try
                {
                    connection.Open();
                    SqlDataReader sqlDataReader = command.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        count = sqlDataReader.GetInt32(0);
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }
            return count;
        }

        public static void Before()
        {
            int all = 0;
            int del = 0;
            int work = 0;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT [All]=COUNT(*),[del]=isnull(max(r.del),0),[work]=isnull(max(y.[work]),0) from [Registers].[dbo].[MVS_PASSPORTS]  t ", connection);
                command.CommandText += " left join (SELECT [del]=COUNT(*) from [Registers].[dbo].[MVS_PASSPORTS]  where isdelete=1 ) as r on 1=1 ";
                command.CommandText += " left join (SELECT [work]=COUNT(*) from [Registers].[dbo].[MVS_PASSPORTS]  where isdelete=0 ) as y on 1=1 ";

                try
                {
                    connection.Open();
                    SqlDataReader sqlDataReader = command.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        all = sqlDataReader.GetInt32(0);
                        del = sqlDataReader.GetInt32(1);
                        work = sqlDataReader.GetInt32(2);
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }
            Console.WriteLine($"Старт задачи: {DateTime.Now.ToString()} \n\r\n\r Всего: {all} Удаленных: {del} \n\r Рабочих: {work}");
        }
        public static void After()
        {
            int all = 0;
            int del = 0;
            int work = 0;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand comand = new SqlCommand("SELECT [All]=COUNT(*),[del]=isnull(max(r.del),0),[work]=isnull(max(y.[work]),0) from [Registers].[dbo].[MVS_PASSPORTS]  t ", connection);
                comand.CommandText += " left join (SELECT [del]=COUNT(*) from [Registers].[dbo].[MVS_PASSPORTS]  where isdelete=1 ) as r on 1=1 ";
                comand.CommandText += " left join (SELECT [work]=COUNT(*) from [Registers].[dbo].[MVS_PASSPORTS]  where isdelete=0 ) as y on 1=1 ";

                try
                {

                    connection.Open();
                    SqlDataReader sd = comand.ExecuteReader();
                    while (sd.Read())
                    {
                        all = sd.GetInt32(0);
                        del = sd.GetInt32(1);
                        work = sd.GetInt32(2);
                    }



                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
            Console.WriteLine($"Конец задачи: {DateTime.Now.ToString()} \n\r\n\r Всего: {all} Удаленных: {del} \n\r Рабочих: {work}");
        }

        public static void ClearTemp()
        {
            using SqlConnection connection = new SqlConnection(connectionString);
            SqlCommand command = new SqlCommand("TRUNCATE TABLE [Registers].[dbo].[MVS_PASSPORTS_tmp]", connection);
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }
    }
}
