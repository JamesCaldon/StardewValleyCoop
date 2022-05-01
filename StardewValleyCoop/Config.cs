using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace StardewValleyCoop
{
    public abstract class Config
    {
		public static T Load<T>(string filePath)
		{
			using TextReader reader = new StreamReader(filePath);
			var serializer = new XmlSerializer(typeof(T));
			return (T) (serializer.Deserialize(reader) ??
				throw new Exception("Failed to load Handler"));
		}

		public void Save(string filePath)
		{
			using FileStream fileStream = File.Create(filePath);
			using StreamWriter streamWriter = new(fileStream);
			var serializer = new XmlSerializer(this.GetType());
			serializer.Serialize(streamWriter, this);
		}
	}
}
