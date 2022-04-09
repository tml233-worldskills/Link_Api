using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Runtime.Serialization.Json;
using System.IO;

public static class Utils {
	public static string ConnectionString => ConfigurationManager.ConnectionStrings["db"].ConnectionString;
	public static Tuple<SqlConnection, SqlCommand> Prepare(string sql, params object[] args) {
		var conn = new SqlConnection(ConnectionString);
		var cmd = new SqlCommand(sql, conn);
		for (int i = 0; i < args.Length; i += 1) {
			cmd.Parameters.AddWithValue("@" + i, args[i]);
		}
		return new Tuple<SqlConnection, SqlCommand>(conn, cmd);
	}
	public static int ExecuteUpdate(string sql, params object[] args) {
		var pair = Prepare(sql, args);
		var conn = pair.Item1;
		var cmd = pair.Item2;
		try {
			conn.Open();
			return cmd.ExecuteNonQuery();
		} finally {
			cmd.Dispose();
			conn.Close();
		}
	}
	public static object ExecuteScalar(string sql, params object[] args) {
		var pair = Prepare(sql, args);
		var conn = pair.Item1;
		var cmd = pair.Item2;
		try {
			conn.Open();
			return cmd.ExecuteScalar();
		} finally {
			cmd.Dispose();
			conn.Close();
		}
	}
	public static IEnumerable<SqlDataReader> ExecuteReader(string sql, params object[] args) {
		var pair = Prepare(sql, args);
		var conn = pair.Item1;
		var cmd = pair.Item2;
		SqlDataReader reader = null;
		try {
			conn.Open();
			reader = cmd.ExecuteReader();
			while (reader.Read()) {
				yield return reader;
			}
		} finally {
			reader?.Close();
			cmd.Dispose();
			conn.Close();
		}
	}
	public static DataTable ExecuteQuery(string sql, params object[] args) {
		var pair = Prepare(sql, args);
		var conn = pair.Item1;
		var cmd = pair.Item2;
		try {
			conn.Open();
			using (var adp = new SqlDataAdapter(cmd)) {
				var dt = new DataTable();
				adp.Fill(dt);
				return dt;
			}
		} finally {
			cmd.Dispose();
			conn.Close();
		}
	}
	public static string SerializeToJson<T>(T obj) where T : struct {
		var s = new DataContractJsonSerializer(typeof(T));
		using (var stream = new MemoryStream()) {
			s.WriteObject(stream, obj);
			return Encoding.UTF8.GetString(stream.ToArray());
		}
	}
	public static T DeserializeFromJson<T>(string json) where T : struct {
		var s = new DataContractJsonSerializer(typeof(T));
		using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json))) {
			return (T)s.ReadObject(stream);
		}
	}
}