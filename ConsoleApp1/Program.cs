using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace ConsoleApp1
{
	class Program
	{

		static void Main(string[] args)
		{
			string ConnectionStrings = "server=192.168.99.100;port=3306;database=test;user=root;password=123456;SslMode=None;charset=utf8mb4;old guids=true;";

			var connection = GetConnection(ConnectionStrings);
			// 設定 Class Name
			var className = "RoadModel";
			// 設定要查找註解的 Table Name, 多筆用 "," 分開
			var tables = @"blog";

			// 這邊修改為你要執行的 SQL Command
			var sqlCommand = @"SELECT t.*
      FROM test.blog t";

			var dtAnnotation = GetAnnotation(connection, tables);

			var aa= DumpClassCSharp(connection, dtAnnotation, sqlCommand, className);

			Console.WriteLine(aa);
		}
		public static IDbConnection GetConnection(string connectionString)
		{
			var conn = new MySqlConnection(connectionString);

			return conn;
		}

		public static DataTable GetAnnotation(IDbConnection connection, string tables)
		{
			var sqlTable = "'" + string.Join("','", tables.Split(',')) + "'";
			var sql = string.Format(@"SELECT
							a.TABLE_NAME,
							b.COLUMN_NAME,
							b.DATA_TYPE,
							b.CHARACTER_MAXIMUM_LENGTH,
							b.COLUMN_DEFAULT,
							b.IS_NULLABLE,
							b.COLUMN_COMMENT AS FieldMemo
						FROM INFORMATION_SCHEMA.TABLES a
						LEFT JOIN INFORMATION_SCHEMA.COLUMNS b
							ON (a.TABLE_NAME = b.TABLE_NAME)
						WHERE TABLE_TYPE = 'BASE TABLE'
						AND a.TABLE_NAME IN ({0})", sqlTable);

			if (connection.State != ConnectionState.Open)
			{
				connection.Open();
			}

			var adapter = new MySqlDataAdapter();
			var cmd = connection.CreateCommand();
			cmd.CommandText = sql;
			cmd.CommandType = CommandType.Text;

			// Set the SqlDataAdapter's SelectCommand.
			adapter.SelectCommand = (MySqlCommand)cmd;

			// Fill the DataSet.
			var dataSet = new DataSet();
			adapter.Fill(dataSet);

			return dataSet.Tables[0];
		}

		public static string DumpClassCSharp(IDbConnection connection, DataTable dtAnnotation, string sql, string className = "Info")
		{
			if (connection.State != ConnectionState.Open)
			{
				connection.Open();
			}

			var cmd = connection.CreateCommand();
			cmd.CommandText = sql;
			var reader = cmd.ExecuteReader();

			var builder = new StringBuilder();
			do
			{
				if (reader.FieldCount <= 1) continue;

				builder.AppendFormat("public class {0}{1}", className, Environment.NewLine);
				builder.AppendLine("{");
				var schema = reader.GetSchemaTable();

				foreach (DataRow row in schema.Rows)
				{
					var type = (Type)row["DataType"];
					var name = TypeAliasesCSharp.ContainsKey(type) ? TypeAliasesCSharp[type] : type.Name;
					var isNullable = (bool)row["AllowDBNull"] && NullableTypes.Contains(type);
					var collumnName = (string)row["ColumnName"];

					var aa = (
							from dr in dtAnnotation.AsEnumerable()
							where dr.Field<string>("COLUMN_NAME") == collumnName
							select dr).FirstOrDefault();

					builder.AppendLine(string.Format("\t/// <summary>"));
					builder.AppendLine(string.Format("\t/// {0}", aa == null ? "對應不到註解" : aa.Field<string>("FieldMemo")));
					builder.AppendLine(string.Format("\t/// </summary>"));
					builder.AppendLine(string.Format("\tpublic {0}{1} {2} {{ get; set; }}", name, isNullable ? "?" : string.Empty, collumnName));
					builder.AppendLine();
				}

				builder.AppendLine("}");
				builder.AppendLine();
			} while (reader.NextResult());

			return builder.ToString();
		}

		private static readonly Dictionary<Type, string> TypeAliasesCSharp = new Dictionary<Type, string> {
		{ typeof(int), "int" },
		{ typeof(short), "short" },
		{ typeof(byte), "byte" },
		{ typeof(byte[]), "byte[]" },
		{ typeof(long), "long" },
		{ typeof(double), "double" },
		{ typeof(decimal), "decimal" },
		{ typeof(float), "float" },
		{ typeof(bool), "bool" },
		{ typeof(string), "string" }
	};

		private static readonly HashSet<Type> NullableTypes = new HashSet<Type> {
		typeof(int),
		typeof(short),
		typeof(long),
		typeof(double),
		typeof(decimal),
		typeof(float),
		typeof(bool),
		typeof(DateTime)
	     };
	}

}