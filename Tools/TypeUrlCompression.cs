
using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace RibCom.Tools
{
	public class TypeUrlCompression
	{
		private const string TypeUrlDefaultPrefix = "type.googleapis.com/";

		private readonly MessageSolver _solver;

		private Dictionary<System.Type, string> _uncompressedToCompressed = new Dictionary<System.Type, string>();
		private Dictionary<string, string> _compressedToUncompressed = new Dictionary<string, string>();

		public TypeUrlCompression(MessageSolver solver)
		{
			_solver = solver;
			UpdateCompressionTable();
		}


		public string GetCompressedTypeUrl(IMessage m)
		{
			if (_uncompressedToCompressed.TryGetValue(m.GetType(), out string compressed))
				return compressed;
			// The else should not happen (type not listed in the solver)
			else
				return "???";
		}

		public string GetUncompressedTypeUrl(string compressedTypeUrl)
		{
			if (_compressedToUncompressed.TryGetValue(compressedTypeUrl, out string uncompressed))
			{
				Console.WriteLine($"decompress {compressedTypeUrl} into {TypeUrlDefaultPrefix + uncompressed}");
				return TypeUrlDefaultPrefix + uncompressed;
			}
			// Should not happen
			else
			{
				Console.WriteLine($"$decompression gone wrong: {compressedTypeUrl} not recognized");
				return compressedTypeUrl;
			}
		}

		public void UpdateCompressionTable()
		{
			_uncompressedToCompressed.Clear();

			int id = 0;
			foreach (var kvp in _solver.GetTypeMessageDescriptorMapping())
			{
				string uncompressedName = kvp.Value.FullName;
				string compressedName = id.ToString();
				_uncompressedToCompressed[kvp.Key] = compressedName;
				_compressedToUncompressed[compressedName] = uncompressedName;

				id += 1;
			}
		}
	}
}